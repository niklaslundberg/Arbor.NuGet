using System;
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
    internal static class NuSpecCommandDefinition
    {
        private static readonly Option SourceDir =
            new Option(
                "--source-directory",
                Strings.SourceDirectoryDescription,
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                });

        private static readonly Option OutputFile =
            new Option(
                "--output-file",
                Strings.OutputFileDescription,
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                });

        private static readonly Option PackageId =
            new Option(
                "--package-id",
                Strings.PackageIdDescription,
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                });

        private static readonly Option PackageVersion =
            new Option(
                "--package-version",
                Strings.PackageVersionDescription,
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                });

        private static readonly Option VersionFile =
            new Option(
                "--version-file",
                Strings.VersionFile,
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                });

        private static readonly Option MsBuildVersionFile =
            new Option(
                "--msbuild-version-file",
                Strings.VersionFile,
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                });

        private static readonly Option PackageDirectory =
            new Option(
                "--package-directory",
                "NuGet package directory output path",
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                });

        private static SemanticVersion? GetVersion(string? packageVersion,
            string? versionFile,
            string? msBuildVersionFile,
            IFileSystem fileSystem,
            ILogger logger)
        {
            SemanticVersion version;

            if (!string.IsNullOrWhiteSpace(packageVersion))
            {
                if (!SemanticVersion.TryParse(packageVersion, out SemanticVersion parsedVersion))
                {
                    logger.Error("Invalid semver '{PackageVersion}'", packageVersion);
                    return default;
                }

                version = parsedVersion;
            }
            else if (!string.IsNullOrWhiteSpace(versionFile))
            {
                var versionFromFile =
                    JsonFileVersionHelper.GetVersionFromJsonFile(versionFile!.NormalizePath(), fileSystem, logger);

                if (versionFromFile is null)
                {
                    logger.Error("Could not get version from file '{VersionFile}'", versionFile);
                    return default;
                }

                version = versionFromFile;
            }
            else
            {
                var versionFromFile =
                    JsonFileVersionHelper.GetVersionFromMsBuildFile(msBuildVersionFile!.NormalizePath(), fileSystem,
                        logger);

                if (versionFromFile is null)
                {
                    logger.Error("Could not get version from MSBuild file '{VersionFile}'", versionFile);
                    return default;
                }

                version = versionFromFile;
            }

            return version;
        }

        public static Command Tool(ILogger logger, IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            var tool = new Command("nuspec", Strings.NuSpecDescription);

            Command NuSpec()
            {
                return new Command(
                    "create",
                    Strings.CreateDescription,
                    new[] {SourceDir, OutputFile, PackageId, PackageVersion, VersionFile, MsBuildVersionFile, PackageDirectory},
                    handler: CommandHandler.Create<string, string, string, string, string, string, string>(Bind));
            }

            async Task<int> Bind(string? sourceDirectory,
                string? outputFile,
                string? packageId,
                string? packageVersion,
                string? versionFile,
                string? msBuildVersionFile,
                string? packageDirectory)
            {
                if (string.IsNullOrWhiteSpace(sourceDirectory))
                {
                    logger.Error("Missing expected --{Arg} argument", SourceDir.Aliases.FirstOrDefault());
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(outputFile))
                {
                    logger.Error("Missing expected --{Arg} argument", OutputFile.Aliases.FirstOrDefault());
                    return 2;
                }

                if (string.IsNullOrWhiteSpace(packageId))
                {
                    logger.Error("Missing expected --{Arg} argument", PackageId.Aliases.FirstOrDefault());
                    return 3;
                }

                if (string.IsNullOrWhiteSpace(packageVersion) &&
                    string.IsNullOrWhiteSpace(versionFile) &&
                    string.IsNullOrWhiteSpace(msBuildVersionFile))
                {
                    logger.Error("Missing expected --{Arg} argument", PackageVersion.Aliases.FirstOrDefault());
                    return 4;
                }

                var version = GetVersion(packageVersion, versionFile, msBuildVersionFile, fileSystem, logger);

                if (version is null)
                {
                    throw new InvalidOperationException("Could not get version");
                }

                var packageDefinition = new PackageDefinition(
                    new PackageId(packageId),
                    version);

                int exitCode = await NuSpecCreator.CreateSpecificationAsync(
                    packageDefinition,
                    logger,
                    new DirectoryEntry(fileSystem, sourceDirectory.NormalizePath()),
                    outputFile.NormalizePath(),
                    cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

                if (exitCode != 0)
                {
                    return exitCode;
                }

                if (!string.IsNullOrWhiteSpace(packageDirectory))
                {
                    var outputFileEntry = fileSystem.GetFileEntry(outputFile.NormalizePath());
                    string packageFileName = $"{packageId}.{version.ToNormalizedString()}.nupkg";
                    var packageFilePath = packageDirectory.NormalizePath() / packageFileName;

                    int packageExitCode = await NuGetPacker.PackNuSpec(outputFileEntry, packageFilePath, logger,
                        cancellationToken);

                    if (packageExitCode != 0)
                    {
                        return packageExitCode;
                    }
                }

                return exitCode;
            }

            tool.AddCommand(NuSpec());

            return tool;
        }
    }
}