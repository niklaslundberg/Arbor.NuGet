using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Arbor.NuGet.NuSpec.GlobalTool.Application
{
    public static class AppStarter
    {
        public static async Task<int> CreateAndStartAsync(string[] args)
        {
            int exitCode;

            using (var app = Start(args))
            {
                exitCode = await app.ExecuteAsync().ConfigureAwait(continueOnCapturedContext: false);
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }

            return exitCode;
        }

        private static App Start(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(value: 1));

            var app = new App(args ?? Array.Empty<string>(), logger, cancellationTokenSource);

            return app;
        }
    }
}