using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Threading;

namespace RestQueue.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestsController : ControllerBase
    {
        [HttpGet("hello")]
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
