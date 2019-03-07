using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Arbor.NuGet.NuSpec.GlobalTool.NuGet;

using NuGet.Versioning;

using Serilog;

namespace Arbor.NuGet.NuSpec.GlobalTool.CommandLine
{
    public static class CommandDefinitions
    {
        public static Command Tool(ILogger logger, CancellationToken cancellationToken)
        {
            var tool = new Command("nuspec", Strings.NuSpecDescription);

            tool.AddCommand(NuSpec());

            return tool;

            Task<int> Bind(string sourceDirectory, string outputFile, string packageId, string packageVersion)
            {
                if (string.IsNullOrWhiteSpace(sourceDirectory))
                {
                    logger.Error($"Missing expected --{SourceDir().Aliases.FirstOrDefault()} argument");
                    return Task.FromResult(1);
                }

                if (string.IsNullOrWhiteSpace(outputFile))
                {
                    logger.Error($"Missing expected --{OutputFile().Aliases.FirstOrDefault()} argument");
                    return Task.FromResult(2);
                }

                if (string.IsNullOrWhiteSpace(packageId))
                {
                    logger.Error($"Missing expected --{PackageId().Aliases.FirstOrDefault()} argument");
                    return Task.FromResult(3);
                }

                if (string.IsNullOrWhiteSpace(packageVersion))
                {
                    logger.Error($"Missing expected --{PackageVersion().Aliases.FirstOrDefault()} argument");
                    return Task.FromResult(4);
                }

                if (!SemanticVersion.TryParse(packageVersion, out SemanticVersion version))
                {
                    logger.Error("Invalid semver '{PackageVersion}'", packageVersion);
                    return Task.FromResult(5);
                }

                var packageDefinition = new PackageDefinition(
                    new PackageId(packageId),
                    version);

                return NuSpecCreator.CreateSpecificationAsync(
                    packageDefinition,
                    logger,
                    new DirectoryInfo(sourceDirectory),
                    outputFile,
                    cancellationToken);
            }

            Command NuSpec()
            {
                return new Command(
                    "create",
                    Strings.CreateDescription,
                    new[] { SourceDir(), OutputFile(), PackageId(), PackageVersion() },
                    handler: CommandHandler.Create<string, string, string, string>(Bind));
            }

            Option SourceDir()
            {
                return new Option(
                    "--source-directory",
                    Strings.SourceDirectoryDescription,
                    new Argument<string> { Arity = new ArgumentArity(1, 1) });
            }

            Option OutputFile()
            {
                return new Option(
                    "--output-file",
                    Strings.OutputFileDescription,
                    new Argument<string> { Arity = new ArgumentArity(1, 1) });
            }

            Option PackageId()
            {
                return new Option(
                    "--package-id",
                    Strings.PackageIdDescription,
                    new Argument<string> { Arity = new ArgumentArity(1, 1) });
            }

            Option PackageVersion()
            {
                return new Option(
                    "--package-version",
                    Strings.PackageVersionDescription,
                    new Argument<string> { Arity = new ArgumentArity(1, 1) });
            }
        }
    }
}