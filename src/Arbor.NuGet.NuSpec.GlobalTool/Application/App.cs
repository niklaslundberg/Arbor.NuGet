using System;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using Arbor.NuGet.NuSpec.GlobalTool.CommandLine;
using Arbor.NuGet.NuSpec.GlobalTool.Extensions;
using Arbor.NuGet.NuSpec.GlobalTool.Logging;
using Arbor.Processing;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.Application;

public sealed class App(string[] args, ILogger logger, IFileSystem fileSystem, CancellationTokenSource cancellationTokenSource, bool leaveFileSystemOpen = false)
    : IDisposable
{
    public void Dispose()
    {
        if (!leaveFileSystemOpen)
        {
            fileSystem.Dispose();
        }

        cancellationTokenSource.Dispose();
    }

    public async Task<ExitCode> ExecuteAsync()
    {
        try
        {
            var parser = CreateParser();

            int exitCode;

            using (var serilogAdapter = new SerilogAdapter(logger))
            {
                exitCode = await parser.InvokeAsync(args, serilogAdapter);
            }

            return new(exitCode);
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
            logger.Error(ex, "Could not create nuspec");
            return ExitCode.Failure;
        }
    }

    private Parser CreateParser()
    {
        var parser = new CommandLineBuilder()
            .AddCommand(NuSpecCommandDefinition.Tool(logger, fileSystem, cancellationTokenSource.Token))
            .AddCommand(NuSpecCommandDefinition.CreatePackage(logger, fileSystem, cancellationTokenSource.Token))
            .AddCommand(PackCommandDefinition.Tool(logger, fileSystem, cancellationTokenSource.Token))
            .AddCommand(PackageMetadataCommandDefinition.Tool(logger, fileSystem, cancellationTokenSource.Token))
            .UseVersionOption()
            .UseHelp().UseParseDirective().UseDebugDirective().UseSuggestDirective().RegisterWithDotnetSuggest()
            .UseParseErrorReporting().UseExceptionHandler().Build();

        return parser;
    }
}