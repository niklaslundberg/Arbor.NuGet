using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.FS;
using Arbor.NuGet.NuSpec.GlobalTool.NuGet;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.CommandLine
{
    internal static class PackCommandDefinition
    {
        private static readonly Option NuSpecFile =
            new Option(
                "--nuspec-file",
                "NuSpec file",
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                });

        private static readonly Option OutputFile =
            new Option(
                "--output-file",
                "NuGet package output file",
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                });


        public static Command Tool(ILogger logger, IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            var tool = new Command("pack", "NuGet pack nuspec");

            Command NuSpec()
            {
                return new Command(
                    "nuspec",
                    Strings.CreateDescription,
                    new[] {NuSpecFile, OutputFile},
                    handler: CommandHandler.Create<string, string>(Bind));
            }

            async Task<int> Bind(string? nuSpecFile,
                string? outputFile)
            {
                if (string.IsNullOrWhiteSpace(nuSpecFile))
                {
                    logger.Error("Missing expected --{Arg} argument", NuSpecFile.Aliases.FirstOrDefault());
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(outputFile))
                {
                    logger.Error("Missing expected --{Arg} argument", OutputFile.Aliases.FirstOrDefault());
                    return 2;
                }

                return await NuGetPacker.PackNuSpec(
                    fileSystem.GetFileEntry(nuSpecFile.NormalizePath()),
                    outputFile.NormalizePath(),
                    logger,
                    cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }

            tool.AddCommand(NuSpec());

            return tool;
        }
    }
}