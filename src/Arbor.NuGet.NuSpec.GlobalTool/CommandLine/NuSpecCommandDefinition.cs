using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.FS;
using Arbor.NuGet.NuSpec.GlobalTool.NuGet;
using Arbor.NuGet.NuSpec.GlobalTool.Versioning;
using NuGet.Versioning;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.CommandLine
{
    internal static class NuSpecCommandDefinition
    {
        private static readonly Option SourceDirectory =
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
                Strings.MsBuildVersionFile,
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                });

        private static readonly Option PackageDirectory =
            new Option(
                "--package-directory",
                Strings.PackageOutputDirectory,
                new Argument<string>
                {
                    Arity = new ArgumentArity(minimumNumberOfArguments: 1, maximumNumberOfArguments: 1)
                });

        private static SemanticVersion GetVersion(string? packageVersion,
            string? versionFile,
            string? msBuildVersionFile,
            IFileSystem fileSystem,
            ILogger logger)
        {
            SemanticVersion? version = null;

            if (!string.IsNullOrWhiteSpace(packageVersion))
            {
                if (SemanticVersion.TryParse(packageVersion, out SemanticVersion parsedVersion))
                {
                    version = parsedVersion;
                }
                else
                {
                    logger.Error("Invalid semver '{PackageVersion}'", packageVersion);
                }
            }
            else if (!string.IsNullOrWhiteSpace(versionFile))
            {
                var versionFromFile =
                    JsonFileVersionHelper.GetVersionFromJsonFile(versionFile!.NormalizePath(), fileSystem, logger);

                if (versionFromFile is {})
                {
                    version = versionFromFile;
                }
                else
                {
                    logger.Error("Could not get version from file '{VersionFile}'", versionFile);
                }
            }
            else
            {
                var versionFromFile =
                    JsonFileVersionHelper.GetVersionFromMsBuildFile(msBuildVersionFile!.NormalizePath(), fileSystem,
                        logger);

                if (versionFromFile is {})
                {
                    version = versionFromFile;
                }
                else
                {
                    logger.Error("Could not get version from MSBuild file '{VersionFile}'", versionFile);
                }
            }

            if (version is null)
            {
                throw new InvalidOperationException("Could not get version");
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
                    new[]
                    {
                        SourceDirectory,
                        OutputFile,
                        PackageId,
                        PackageVersion,
                        VersionFile,
                        MsBuildVersionFile,
                        PackageDirectory
                    },
                    handler: CommandHandler.Create<string, string, string, string, string, string, string>(
                        (sourceDirectory,
                                outputFile,
                                packageId,
                                packageVersion,
                                versionFile,
                                msBuildVersionFile,
                                packageDirectory) =>
                            BindAndValidate(new CommandOptions(sourceDirectory, outputFile, packageId, packageVersion, versionFile,
                                msBuildVersionFile, packageDirectory), logger, fileSystem, cancellationToken)));
            }


            tool.AddCommand(NuSpec());

            return tool;
        }

        private static async Task<int> Bind(
            CommandOptions options,
            ILogger logger,
            IFileSystem fileSystem,
            CancellationToken cancellationToken)
        {
            var version = GetVersion(options.PackageVersion, options.VersionFile, options.MsBuildVersionFile, fileSystem, logger);

            var packageDefinition = new PackageDefinition(
                new PackageId(options.PackageId!),
                version);

            int exitCode = await NuSpecCreator.CreateSpecificationAsync(
                packageDefinition,
                logger,
                new DirectoryEntry(fileSystem, options.SourceDirectory!.NormalizePath()),
                options.OutputFile!.NormalizePath(),
                cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            if (exitCode != 0)
            {
                return exitCode;
            }

            if (!string.IsNullOrWhiteSpace(options.PackageDirectory))
            {
                var outputFileEntry = fileSystem.GetFileEntry(options.OutputFile!.NormalizePath());
                string packageFileName = $"{options.PackageId}.{version.ToNormalizedString()}.nupkg";
                var packageFilePath = options.PackageDirectory.NormalizePath() / packageFileName;

                int packageExitCode = await NuGetPacker.PackNuSpec(
                    outputFileEntry, packageFilePath, logger,
                    cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

                if (packageExitCode != 0)
                {
                    return packageExitCode;
                }
            }

            return exitCode;
        }

        private static Task<int> BindAndValidate(
            CommandOptions options,
            ILogger logger,
            IFileSystem fileSystem,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(options.SourceDirectory))
            {
                logger.Error("Missing expected --{Arg} argument", SourceDirectory.Aliases.FirstOrDefault());
                return Task.FromResult(result: 1);
            }

            if (string.IsNullOrWhiteSpace(options.OutputFile))
            {
                logger.Error("Missing expected --{Arg} argument", OutputFile.Aliases.FirstOrDefault());
                return Task.FromResult(result: 2);
            }

            if (string.IsNullOrWhiteSpace(options.PackageId))
            {
                logger.Error("Missing expected --{Arg} argument", PackageId.Aliases.FirstOrDefault());
                return Task.FromResult(result: 3);
            }

            if (string.IsNullOrWhiteSpace(options.PackageVersion) &&
                string.IsNullOrWhiteSpace(options.VersionFile) &&
                string.IsNullOrWhiteSpace(options.MsBuildVersionFile))
            {
                logger.Error("Missing expected --{Arg} argument", PackageVersion.Aliases.FirstOrDefault());
                return Task.FromResult(result: 4);
            }

            return Bind(options, logger, fileSystem, cancellationToken);
        }
    }

    internal class CommandOptions
    {
        public string? SourceDirectory { get; }
        public string? OutputFile { get; }
        public string? PackageId { get; }
        public string? PackageVersion { get; }
        public string? VersionFile { get; }
        public string? MsBuildVersionFile { get; }
        public string? PackageDirectory { get; }

        public CommandOptions(string? sourceDirectory, string? outputFile, string? packageId, string? packageVersion, string? versionFile, string? msBuildVersionFile, string? packageDirectory)
        {
            SourceDirectory = sourceDirectory;
            OutputFile = outputFile;
            PackageId = packageId;
            PackageVersion = packageVersion;
            VersionFile = versionFile;
            MsBuildVersionFile = msBuildVersionFile;
            PackageDirectory = packageDirectory;
        }
    }
}