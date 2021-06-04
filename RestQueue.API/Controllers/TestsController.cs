using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using RestQueue.API.Support;

namespace RestQueue.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestsController : ControllerBase
    {
        private static ControllerBase? _requestsController;
        public TestsController(IResponseHandler responseHandler)
        {
            // This is a hack for now to register the Url of the request controller
            _requestsController ??= new RequestsController(responseHandler);
        }

        [HttpGet("")]
        public string Hello()
        {
            return "Hello world!";
        }

        [HttpGet("slow")]
        public string Slow()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Thread.Sleep(TimeSpan.FromSeconds(10));
            stopwatch.Stop();
            return $"The execution took {stopwatch.Elapsed.TotalSeconds} seconds";
        }
    }
}
