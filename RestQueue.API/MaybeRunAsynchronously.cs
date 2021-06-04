using System;
using System.Linq;
using System.Net.Http;
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
        private readonly IResponseHandler _responseHandler;
        private readonly HttpClient _httpClient;
        private readonly BackgroundQueueWorker<RequestData> _requestWorker;

        public MaybeRunAsynchronously(RequestDelegate next, IResponseHandler responseHandler)
        {
            _next = next;
            _responseHandler = responseHandler; // Store responses for eventual delivery
            _httpClient = GetHttpClientForInternalCalls();
            // Register a worker to handle requests on a background thread for later execution
            _requestWorker = new BackgroundQueueWorker<RequestData>(ExecuteRequestAndMakeResponseAvailable);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (PreferAsync(context.Request))
            {
                // The caller prefers asynchronous execution
                var id = await EnqueueRequestForLaterExecutionAsync(context.Request);
                await ReturnAcceptedWithLocationOfFinalResponse(context, id);
            }
            else
            {
                // Synchronous execution
                await _next(context);
            }
        }

        private static HttpClient GetHttpClientForInternalCalls()
        {
#if true
            // This works
            return HttpClientFactory.Create();
#else
            // ... but I would like to do something like this
            var webApplicationFactory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Startup>();
            return webApplicationFactory.CreateDefaultClient();
#endif
        }

        // This is called by the background worker, to execute the next request from the queue
        private async Task ExecuteRequestAndMakeResponseAvailable(RequestData requestData)
        {
            ResponseData responseData;
            try
            {
                var requestMessage = requestData.ToHttpRequestMessage();
                requestMessage.Headers.Add(IsRunningAsynchronouslyHeader, "TRUE"); 
                var response = await _httpClient.SendAsync(requestMessage);
                responseData = await new ResponseData().FromAsync(response);
            }
            catch (Exception e)
            {
                responseData = new ResponseData().From(e);
            }

            // Serialize the response and make it available to the caller
            _responseHandler.AddResponse(requestData.Id, responseData);
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
            _requestWorker.Enqueue(requestData);
            return requestData.Id;
        }

        private async Task ReturnAcceptedWithLocationOfFinalResponse(HttpContext context, Guid id)
        {
            await _responseHandler.AcceptedResponse(context.Response, id);
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