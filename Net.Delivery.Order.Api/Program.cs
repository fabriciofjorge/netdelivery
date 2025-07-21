using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Net.Delivery.Order.Api;

/// <summary>
/// Program class
/// </summary>
public static class Program
{
    /// <summary>
    /// Main method to run the application
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
