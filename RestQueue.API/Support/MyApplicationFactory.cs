using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RestQueue.API.Support
{
    public class MyApplicationFactory<T> : WebApplicationFactory<T> where T : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                //builder.ConfigureKestrel(serverOptions =>
                //{
                //});
            });

            base.ConfigureWebHost(builder);
        }
    }
}