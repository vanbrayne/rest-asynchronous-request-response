using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RestQueue.API.Model;

namespace RestQueue.API.Support
{
    public class RequestExecutor : IRequestExecutor
    {
        private static readonly ConcurrentDictionary<Guid, ResponseData> ResponsesByRequestId = new ConcurrentDictionary<Guid, ResponseData>();
        private string _urlFormat = $"Programmers error, {nameof(RegisterUrlFormat)} has not been called.";

        private readonly HttpClient _httpClient;

        public RequestExecutor()
        {
#if true
            // This works
            _httpClient = HttpClientFactory.Create();
#else
            // ... but I would like to do something like this
            var webApplicationFactory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Startup>();
            HttpClient = webApplicationFactory.CreateDefaultClient();
#endif 
        }

        /// <summary>
        /// This method is called by the background worker to execute a request and to make the response available to the client.
        /// </summary>
        /// <param name="requestData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ExecuteRequestAndMakeResponseAvailable(RequestData requestData, CancellationToken cancellationToken)
        {
            ResponseData responseData;
            var requestMessage = requestData.ToHttpRequestMessage();
            try
            {
                var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
                responseData = await new ResponseData().FromAsync(response);
            }
            catch (Exception e)
            {
                responseData = new ResponseData().From(e);
            }

            // Serialize the response and make it available to the caller
            ResponsesByRequestId.TryAdd(requestData.Id, responseData);
        }

        // 202 Accepted, with a link to poll for the final response.
        public async Task SetResponseToAcceptedWithLocationOfFinalResponse(HttpResponse response, Guid requestId)
        {
            var responseData = AcceptedResponse(requestId);
            response.StatusCode = (int)responseData.StatusCode;
            response.ContentType = "application/json";
            await response.WriteAsync(responseData.BodyAsString);
        }

        /// <inheritdoc />
        public void RegisterUrlFormat(string urlFormat)
        {
            _urlFormat = urlFormat;
        }

        public ResponseData GetResponse(Guid requestId)
        {
            return ResponsesByRequestId.TryGetValue(requestId, out var responseData) ? responseData : AcceptedResponse(requestId);
        }

        private ResponseData AcceptedResponse(Guid requestId)
        {
            return new ResponseData
            {
                StatusCode = HttpStatusCode.Accepted,
                BodyAsString = JsonConvert.SerializeObject(RedirectObject(requestId))
            };
        }

        public object RedirectObject(Guid requestId) => new { ResponseUrl = ResponseUrl(requestId) };

        public string ResponseUrl(Guid requestId)
        {
            var url = string.Format(_urlFormat, requestId.ToString());
            return url;
        }
    }
}