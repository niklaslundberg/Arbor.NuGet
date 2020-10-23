using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.FS;
using Arbor.NuGet.NuSpec.GlobalTool.NuGet;
using NuGet.Versioning;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.CommandLine
{
    internal static class CommandDefinitions
    {
        public static Command Tool(ILogger logger, IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            var tool = new Command("nuspec", Strings.NuSpecDescription);

            tool.AddCommand(NuSpec());

            return tool;

            async Task<int> Bind(string? sourceDirectory,
                string? outputFile,
                string? packageId,
                string? packageVersion,
                string? versionFile,
                string? msBuildVersionFile)
            {
                if (string.IsNullOrWhiteSpace(sourceDirectory))
                {
                    logger.Error("Missing expected --{Arg} argument", SourceDir().Aliases.FirstOrDefault());
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(outputFile))
                {
                    logger.Error("Missing expected --{Arg} argument", OutputFile().Aliases.FirstOrDefault());
                    return 2;
                }

                if (string.IsNullOrWhiteSpace(packageId))
                {
                    logger.Error("Missing expected --{Arg} argument", PackageId().Aliases.FirstOrDefault());
                    return 3;
                }

                if (string.IsNullOrWhiteSpace(packageVersion)
                    && string.IsNullOrWhiteSpace(versionFile)
                    && string.IsNullOrWhiteSpace(msBuildVersionFile))
                {
                    logger.Error("Missing expected --{Arg} argument", PackageVersion().Aliases.FirstOrDefault());
                    return 4;
                }

                SemanticVersion version;

                if (!string.IsNullOrWhiteSpace(packageVersion))
                {
                    if (!SemanticVersion.TryParse(packageVersion, out SemanticVersion parsedVersion))
                    {
                        logger.Error("Invalid semver '{PackageVersion}'", packageVersion);
                        return 5;
                    }

                    version = parsedVersion;
                }
                else if (!string.IsNullOrWhiteSpace(versionFile))
                {
                    var versionFromFile = JsonFileVersionHelper.GetVersionFromJsonFile(versionFile!.NormalizePath(), fileSystem, logger);

                    if (versionFromFile is null)
                    {
                        logger.Error("Could not get version from file '{VersionFile}'", versionFile);
                        return 6;
                    }

                    version = versionFromFile;
                }
                else
                {
                    var versionFromFile = JsonFileVersionHelper.GetVersionFromMsBuildFile(msBuildVersionFile!.NormalizePath(), fileSystem, logger);

                    if (versionFromFile is null)
                    {
                        logger.Error("Could not get version from MSBuild file '{VersionFile}'", versionFile);
                        return 6;
                    }

                    version = versionFromFile;
                }

                var packageDefinition = new PackageDefinition(
                    new PackageId(packageId),
                    version);

                return await NuSpecCreator.CreateSpecificationAsync(
                    packageDefinition,
                    logger,
                    new DirectoryEntry(fileSystem, sourceDirectory.NormalizePath()),
                    outputFile.NormalizePath(),
                    cancellationToken).ConfigureAwait(false);
            }

            Command NuSpec()
            {
                return new Command(
                    "create",
                    Strings.CreateDescription,
                    new[] {SourceDir(), OutputFile(), PackageId(), PackageVersion(), VersionFile(), MsBuildVersionFile()},
                    handler: CommandHandler.Create<string, string, string, string, string, string>(Bind));
            }

            Option SourceDir()
            {
                return new Option(
                    "--source-directory",
                    Strings.SourceDirectoryDescription,
                    new Argument<string>
                    {
                        Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                    });
            }

            Option OutputFile()
            {
                return new Option(
                    "--output-file",
                    Strings.OutputFileDescription,
                    new Argument<string>
                    {
                        Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                    });
            }

            Option PackageId()
            {
                return new Option(
                    "--package-id",
                    Strings.PackageIdDescription,
                    new Argument<string>
                    {
                        Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                    });
            }

            Option PackageVersion()
            {
                return new Option(
                    "--package-version",
                    Strings.PackageVersionDescription,
                    new Argument<string>
                    {
                        Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                    });
            }

            Option VersionFile()
            {
                return new Option(
                    "--version-file",
                    Strings.VersionFile,
                    new Argument<string>
                    {
                        Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                    });
            }

            Option MsBuildVersionFile()
            {
                return new Option(
                    "--msbuild-version-file",
                    Strings.VersionFile,
                    new Argument<string>
                    {
                        Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                    });
            }
        }
    }
}