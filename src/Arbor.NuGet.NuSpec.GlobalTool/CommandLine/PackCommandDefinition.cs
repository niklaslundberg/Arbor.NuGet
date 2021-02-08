using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.FS;
using Arbor.NuGet.NuSpec.GlobalTool.NuGet;
using NuGet.Packaging;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.CommandLine
{
    internal static class PackCommandDefinition
    {
        private static readonly Option NuSpecFile =
            new Option<string?>(
                "--nuspec-file",
                "NuSpec file");

        private static readonly Option PackageDirectory =
            new Option<string?>(
                "--package-directory",
                Strings.PackageOutputDirectory);


        public static Command Tool(ILogger logger, IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            var tool = new Command("pack", Strings.PackNuSpecPackCommand);

            Command NuSpec()
            {
                var command = new Command(
                    "nuspec",
                    Strings.PackNuSpec);
                command.AddOption(NuSpecFile);
                command.AddOption(PackageDirectory);

                command.Handler = CommandHandler.Create<string, string>(Bind);
                return command;
            }

            Task<int> Bind(string? nuSpecFile,
                string? packageDirectory)
            {
                if (string.IsNullOrWhiteSpace(nuSpecFile))
                {
                    logger.Error("Missing expected --{Arg} argument", NuSpecFile.Aliases.FirstOrDefault());
                    return Task.FromResult(result: 1);
                }

                if (string.IsNullOrWhiteSpace(packageDirectory))
                {
                    logger.Error("Missing expected --{Arg} argument", PackageDirectory.Aliases.FirstOrDefault());
                    return Task.FromResult(result: 2);
                }

                return BindInternal(nuSpecFile, packageDirectory);
            }

            async Task<int> BindInternal(string nuSpecFile,
                string packageDirectory)
            {
                var nuSpecFileEntry = fileSystem.GetFileEntry(nuSpecFile.ParseAsPath());

                string normalizedVersion;
                string packageId;

                await using (var nuSpecStream = nuSpecFileEntry.Open(FileMode.Open, FileAccess.Read))
                {
                    var manifest = Manifest.ReadFrom(nuSpecStream, validateSchema: true);

                    normalizedVersion = manifest.Metadata.Version.ToNormalizedString();
                    packageId = manifest.Metadata.Id;
                }

                string packageFileName = $"{packageId}.{normalizedVersion}.nupkg";
                var packageFilePath = packageDirectory.ParseAsPath() / packageFileName;

                return await NuGetPacker.PackNuSpec(
                    nuSpecFileEntry,
                    packageFilePath,
                    logger,
                    cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            }

            tool.AddCommand(NuSpec());

            return tool;
        }
    }
}