using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using Arbor.NuGet.NuSpec.GlobalTool.CommandLine;
using Arbor.NuGet.NuSpec.GlobalTool.Extensions;
using Arbor.NuGet.NuSpec.GlobalTool.Logging;
using Arbor.Processing;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.Application
{
    public sealed class App : IDisposable
    {
        private readonly string[] _args;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly bool _leaveFileSystemOpen;

        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public App(string[] args, ILogger logger, IFileSystem fileSystem, CancellationTokenSource cancellationTokenSource, bool leaveFileSystemOpen = false)
        {
            _args = args;
            _logger = logger;
            _fileSystem = fileSystem;
            _cancellationTokenSource = cancellationTokenSource;
            _leaveFileSystemOpen = leaveFileSystemOpen;
        }

        public void Dispose()
        {
            if (!_leaveFileSystemOpen)
            {
                _fileSystem.Dispose();
            }

            _cancellationTokenSource.Dispose();
        }

        public async Task<ExitCode> ExecuteAsync()
        {
            try
            {
                var parser = CreateParser();

                int exitCode;

                using (var serilogAdapter = new SerilogAdapter(_logger))
                {
                    exitCode = await parser.InvokeAsync(_args, serilogAdapter)
                        .ConfigureAwait(continueOnCapturedContext: false);
                }

                return new ExitCode(exitCode);
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                _logger.Error(ex, "Could not create nuspec");
                return ExitCode.Failure;
            }
        }

        private Parser CreateParser()
        {
            var parser = new CommandLineBuilder()
                .AddCommand(NuSpecCommandDefinition.Tool(_logger, _fileSystem, _cancellationTokenSource.Token))
                .AddCommand(NuSpecCommandDefinition.CreatePackage(_logger, _fileSystem, _cancellationTokenSource.Token))
                .AddCommand(PackCommandDefinition.Tool(_logger, _fileSystem, _cancellationTokenSource.Token))
                .AddCommand(PackageMetadataCommandDefinition.Tool(_logger, _fileSystem, _cancellationTokenSource.Token))
                .UseVersionOption()
                .UseHelp().UseParseDirective().UseDebugDirective().UseSuggestDirective().RegisterWithDotnetSuggest()
                .UseParseErrorReporting().UseExceptionHandler().Build();

            return parser;
        }
    }
}