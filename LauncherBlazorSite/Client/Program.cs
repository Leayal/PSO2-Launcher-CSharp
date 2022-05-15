using LauncherBlazorSite;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace LauncherBlazorSite
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            // builder.Services.AddScoped(sp => new HttpClient { BaseAddress = builder.HostEnvironment.IsDevelopment() ? new Uri(builder.HostEnvironment.BaseAddress) : new Uri(new Uri(builder.HostEnvironment.BaseAddress), "PSO2-Launcher-CSharp") });

            await builder.Build().RunAsync();
        }
    }
}