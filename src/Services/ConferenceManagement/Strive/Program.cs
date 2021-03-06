using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;

namespace Strive
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                CreateWebHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception e)
            {
                var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
                logger.Fatal(e, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().UseSerilog(
                (hostingContext, loggerConfiguration) => loggerConfiguration.ReadFrom
                    .Configuration(hostingContext.Configuration).Enrich.FromLogContext());
        }
    }
}
