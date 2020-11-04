using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.FS;
using NuGet.Packaging;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.CommandLine
{
    internal static class PackageMetadataCommandDefinition
    {
        private static readonly Option PackageFile =
            new Option(
                "--package-file",
                Strings.PackageFile,
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 0, maximumNumberOfArguments: 1)
                });

        public static Command Tool(ILogger logger, IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            var tool = new Command("package-metadata", Strings.PackageMetadata);

            Command Version()
            {
                return new Command(
                    "version",
                    Strings.MetadataVersionDescription,
                    new[] {PackageFile},
                    handler: CommandHandler.Create<string>(packageFile =>
                        BindAndValidate(packageFile, logger, fileSystem, cancellationToken)));
            }

            tool.AddCommand(Version());

            return tool;
        }

        private static async Task BindAndValidate(string packageFile,
            ILogger logger,
            IFileSystem fileSystem,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await using var packageStream =
                fileSystem.OpenFile(packageFile.NormalizePath(), FileMode.Open, FileAccess.Read);

            using var reader = new PackageArchiveReader(packageStream);

            string? versionAsString = reader.NuspecReader.GetVersion().ToNormalizedString();

            logger.Information("{Message}", versionAsString);
        }
    }
}