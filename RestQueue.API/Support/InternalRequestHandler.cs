using System.Net.Http;

namespace RestQueue.API.Support
{
    class InternalRequestHandler : IInternalRequestHandler
    {
        /// <inheritdoc />
        public HttpClient HttpClient { get; }

        public InternalRequestHandler()
        {
#if true
            // This works
            HttpClient = HttpClientFactory.Create();
#else
            // ... but I would like to do something like this
            var webApplicationFactory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Startup>();
            HttpClient = webApplicationFactory.CreateDefaultClient();
#endif 
        }
    }
}