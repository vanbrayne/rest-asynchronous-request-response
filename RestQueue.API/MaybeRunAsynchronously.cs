using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using RestQueue.API.Model;
using RestQueue.API.Support;

namespace RestQueue.API
{
    /// <summary>
    /// Logs requests and responses in the pipe
    /// </summary>
    public class MaybeRunAsynchronously
    {
        private const string IsRunningAsynchronouslyHeader = "X-Is-Running-Asynchronously";
        private readonly RequestDelegate _next;
        private readonly IRequestExecutor _requestExecutor;
        private readonly IBackgroundTaskQueue _taskQueue;

        public MaybeRunAsynchronously(RequestDelegate next, IRequestExecutor requestExecutor, IBackgroundTaskQueue taskQueue)
        {
            _next = next;
            _requestExecutor = requestExecutor; // Execute request and store the response for later retrieval
            _taskQueue = taskQueue;
        }

        // ReSharper disable once UnusedMember.Global
        public async Task InvokeAsync(HttpContext context)
        {
            if (PreferAsync(context.Request))
            {
                // The caller prefers asynchronous execution
                var id = await EnqueueRequestForLaterExecutionAsync(context.Request);
                await _requestExecutor.SetResponseToAcceptedWithLocationOfFinalResponse(context.Response, id);
            }
            else
            {
                // Synchronous execution
                await _next(context);
            }
        }

        // Does the client prefer asynchronous execution of this request?
        private bool PreferAsync(HttpRequest request)
        {
            return request.Headers.TryGetValue("Prefer", out var preferHeader)
                   && preferHeader.Contains("respond-async")
                   && !request.Headers.TryGetValue(IsRunningAsynchronouslyHeader, out _);
        }

        // Serialize the request and enqueue it for eventual execution on a background thread
        private async Task<Guid> EnqueueRequestForLaterExecutionAsync(HttpRequest request)
        {
            var requestData = await new RequestData().FromAsync(request);
            requestData.Headers.Add(IsRunningAsynchronouslyHeader, "TRUE");
            Func<CancellationToken, ValueTask> workItem = token =>
                _requestExecutor.ExecuteRequestAndMakeResponseAvailable(requestData, token);
            await _taskQueue.EnqueueAsync(workItem);
            return requestData.Id;
        }
    }

    public static class MaybeRunAsynchronouslyExtension
    {
        public static IApplicationBuilder UseMaybeRunAsynchronously(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MaybeRunAsynchronously>();
        }
    }
}