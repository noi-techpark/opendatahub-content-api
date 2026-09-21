// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OdhApiImporter
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            LoadDotEnvFiles();

            var host = CreateHostBuilder(args).Build();

            //var monitorLoop = host.Services.GetRequiredService<MonitorLoop>();
            //monitorLoop.StartMonitorLoop();

            await host.RunAsync();
        }

        // Local development only: loads the solution-wide ../.env first, then this
        // project's own .env on top (overriding any shared key). Gated the same way
        // Host.CreateDefaultBuilder gates AddUserSecrets (env.IsDevelopment()), so a
        // Production run never has locally-checked-out dev secrets silently override
        // real environment variables, even if the .env files happen to be present.
        // The environment name isn't available yet this early, so it's read directly
        // from the same variable ASPNETCORE_ENVIRONMENT/DOTNET_ENVIRONMENT the host
        // itself uses to determine it.
        private static void LoadDotEnvFiles()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
                return;

            var rootEnv = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
            if (File.Exists(rootEnv))
                DotNetEnv.Env.Load(rootEnv);

            var projectEnv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(projectEnv))
                DotNetEnv.Env.Load(projectEnv);
        }

        private static void ConfigureServices(
            HostBuilderContext context,
            IServiceCollection services
        )
        {
            //services.AddHostedService<Worker>();
            //services.AddSingleton<MonitorLoop>();
            //services.AddHostedService<QueuedHostedService>();
            //services.AddSingleton<IBackgroundTaskQueue>(_ =>
            //{
            //    if (!int.TryParse(context.Configuration["QueueCapacity"], out var queueCapacity))
            //    {
            //        queueCapacity = 100;
            //    }
            //    return new DefaultBackgroundTaskQueue(queueCapacity);
            //});
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureServices(ConfigureServices)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
