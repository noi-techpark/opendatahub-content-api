// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace OdhApiCore
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            LoadDotEnvFiles();

            var host = CreateHostBuilder(args).Build();

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

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
        }
    }
}
