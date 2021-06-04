using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace RestQueue.API.Model
{
    public class RequestData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Method { get; set; } = "Get";
        public string EncodedUrl { get; set; } = "";
        public HeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public string? BodyAsString { get; set; }
        public HttpMethod HttpMethod => new HttpMethod(Method);

        public async Task<RequestData> FromAsync(HttpRequest request)
        {
            Method = request.Method;
            EncodedUrl = request.GetEncodedUrl();
            Headers = new HeaderDictionary();
            foreach (var requestHeader in request.Headers)
            {
                Headers.Add(requestHeader);
            }
            using var reader = new StreamReader(request.Body);
            BodyAsString = await reader.ReadToEndAsync();
            return this;
        }

        public HttpRequestMessage ToHttpRequestMessage()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod, EncodedUrl);
            foreach (var (key, value) in Headers)
            {
                requestMessage.Headers.Add(key, value.ToArray());
            }

            return requestMessage;
        }
    }
}