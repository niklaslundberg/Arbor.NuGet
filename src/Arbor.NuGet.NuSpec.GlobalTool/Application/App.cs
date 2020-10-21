using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Arbor.NuGet.NuSpec.GlobalTool.CommandLine;
using Arbor.NuGet.NuSpec.GlobalTool.Logging;
using Arbor.Processing;
using Serilog;

namespace Arbor.NuGet.NuSpec.GlobalTool.Application
{
    public sealed class App : IDisposable
    {
        private readonly string[] _args;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly ILogger _logger;

        public App(string[] args, ILogger logger, CancellationTokenSource cancellationTokenSource)
        {
            _args = args;
            _logger = logger;
            _cancellationTokenSource = cancellationTokenSource;
        }

        public void Dispose() => _cancellationTokenSource.Dispose();

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
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not create nuspec");
                return ExitCode.Failure;
            }
        }

        private Parser CreateParser()
        {
            var parser = new CommandLineBuilder()
                .AddCommand(CommandDefinitions.Tool(_logger, _cancellationTokenSource.Token)).UseVersionOption()
                .UseHelp().UseParseDirective().UseDebugDirective().UseSuggestDirective().RegisterWithDotnetSuggest()
                .UseParseErrorReporting().UseExceptionHandler().Build();

            return parser;
        }
    }
}