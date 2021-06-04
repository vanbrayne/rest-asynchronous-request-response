using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RestQueue.API.Model;

namespace RestQueue.API.Support
{
    public interface IResponseHandler
    {
        void AddResponse(Guid requestId, ResponseData responseData);
        ResponseData GetResponse(Guid requestId);
        Task AcceptedResponse(HttpResponse response, Guid requestId);
        void RegisterUrlFormat(string urlFormat);
        string ResponseUrl(Guid requestId);
    }
}