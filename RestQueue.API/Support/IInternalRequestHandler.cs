using System.Net.Http;

namespace RestQueue.API.Support
{
    public interface IInternalRequestHandler
    {
        /// <summary>
        /// An HttpClient for internal calls
        /// </summary>
        HttpClient HttpClient { get; }
    }
}