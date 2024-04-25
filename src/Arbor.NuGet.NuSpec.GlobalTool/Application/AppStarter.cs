using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Zio;
using Zio.FileSystems;

namespace Arbor.NuGet.NuSpec.GlobalTool.Application;

public static class AppStarter
{
    public static async Task<int> CreateAndStartAsync(string[]? args)
    {
        int exitCode;

        using (var app = Start(args))
        {
            exitCode = await app.ExecuteAsync();
        }

        if (Debugger.IsAttached)
        {
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }

        return exitCode;
    }

    private static App Start(string[]? args, IFileSystem? fileSystem = null)
    {
        var logger = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(value: 1));

        var app = new App(args ?? [], logger, fileSystem ?? new PhysicalFileSystem(), cancellationTokenSource);

        return app;
    }
}