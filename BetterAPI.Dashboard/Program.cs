using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Dashboard
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            var serverList = builder.Configuration.GetSection("ServerList").Get<string[]>();

            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(serverList[0], UriKind.Absolute)
            });

            var section = builder.Configuration.GetSection("Logging");
            builder.Logging.AddConfiguration(section);
            await builder.Build().RunAsync();
        }
    }
}
