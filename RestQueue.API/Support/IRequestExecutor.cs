using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RestQueue.API.Model;

namespace RestQueue.API.Support
{
    public interface IRequestExecutor
    {
        Task ExecuteRequestAndMakeResponseAvailable(RequestData requestData, CancellationToken cancellationToken);
        ResponseData GetResponse(Guid requestId);
        Task SetResponseToAcceptedWithLocationOfFinalResponse(HttpResponse response, Guid requestId);
        void RegisterUrlFormat(string urlFormat);
        string ResponseUrl(Guid requestId);
    }
}