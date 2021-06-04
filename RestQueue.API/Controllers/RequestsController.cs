using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using RestQueue.API.Support;

namespace RestQueue.API.Controllers
{
    [ApiController]
    [Route("")]
    public class RequestsController : ControllerBase
    {
        private readonly IResponseHandler _responseHandler;

        public RequestsController(IResponseHandler responseHandler)
        {
            _responseHandler = responseHandler;
        }

        [HttpGet("")]
        public string Initialize()
        {
            var newGuid = Guid.NewGuid();
            var responseUrl = Url.Link("GetResponse",
                new Dictionary<string, object> { { "requestId", newGuid } });
            _responseHandler.RegisterUrlFormat(responseUrl.Replace(newGuid.ToString(), @"{0}"));
            // Hack: By having this as the landing page, we make sure that we will do the register
            // step in the constructor
            return "PoC for asynchronous REST calls." +
                   " First try GET http://localhost:22021/tests/hello and GET http://localhost:22021/tests/slow." +
                   "  Now call them again, but set the HTTP header \"Prefer: respond-async\". This makes them" + 
                   " run in the background. You should immediately get a URL back where you can access the final response." +
                   " when it is available.";
        }

        [HttpGet("requests/{requestId}", Name = "GetResponse")]
        public ActionResult GetResponse(Guid requestId)
        {
            var response = _responseHandler.GetResponse(requestId);
            if (response.StatusCode == HttpStatusCode.Accepted) return new AcceptedResult(_responseHandler.ResponseUrl(requestId), response);
            return Ok(response);
        }
    }
}
