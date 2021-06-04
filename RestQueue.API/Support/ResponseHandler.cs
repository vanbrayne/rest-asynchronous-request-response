using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RestQueue.API.Model;

namespace RestQueue.API.Support
{
    public class ResponseHandler : IResponseHandler
    {
        private static readonly ConcurrentDictionary<Guid, ResponseData> ResponsesByRequestId = new ConcurrentDictionary<Guid, ResponseData>();
        private string _urlFormat = $"Programmers error, {nameof(RegisterUrlFormat)} has not been called.";

        // 202 Accepted, with a link to poll for the final response.
        public async Task AcceptedResponse(HttpResponse response, Guid requestId)
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

        /// <inheritdoc />
        public void AddResponse(Guid requestId, ResponseData responseData)
        {
            ResponsesByRequestId.TryAdd(requestId, responseData);
        }

        private ResponseData AcceptedResponse(Guid requestId)
        {
            return new ResponseData
            {
                StatusCode = HttpStatusCode.Accepted,
                BodyAsString = JsonConvert.SerializeObject(RedirectObject(requestId))
            };
        }

        public object RedirectObject(Guid requestId)=>  new {ResponseUrl = ResponseUrl(requestId)};

        public string ResponseUrl(Guid requestId)
        {
            var url =  string.Format(_urlFormat, requestId.ToString());
            return url;
        }
    }
}