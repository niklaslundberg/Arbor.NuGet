using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbor.FS;
using Arbor.KVConfiguration.JsonConfiguration;
using Arbor.NuGet.NuSpec.GlobalTool.Extensions;
using Arbor.NuGet.NuSpec.GlobalTool.NuGet;
using NuGet.Versioning;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.CommandLine
{
    public static class CommandDefinitions
    {
        public static Command Tool(ILogger logger, IFileSystem fileSystem, CancellationToken cancellationToken)
        {
            var tool = new Command("nuspec", Strings.NuSpecDescription);

            tool.AddCommand(NuSpec());

            return tool;

            async Task<SemanticVersion?> GetVersionFromFile(UPath versionFile)
            {
                try
                {
                    string? path = fileSystem.ConvertPathToInternal(versionFile);
                    var jsonFileReader = new JsonFileReader(path);

                    var configurationItems = jsonFileReader.ReadConfiguration();

                    const string major = nameof(major);
                    const string minor = nameof(minor);
                    const string patch = nameof(patch);

                    var items = new Dictionary<string, int> {[major] = 0, [minor] = 0, [patch] = 0};

                    foreach (string key in items.Keys.ToArray())
                    {
                        string? value = configurationItems.SingleOrDefault(pair =>
                            string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))?.Value;

                        if (!int.TryParse(value, out int intValue) ||
                            intValue < 0)
                        {
                            throw new FormatException("Could not parse {Key} as a positive integer");
                        }

                        items[key] = intValue;
                    }

                    return new SemanticVersion(items[major], items[minor], items[patch]);
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    logger.Error(ex, "Could not get version from file {VersionFile}", versionFile);
                    return null;
                }
            }

            async Task<int> Bind(string? sourceDirectory,
                string? outputFile,
                string? packageId,
                string? packageVersion,
                string? versionFile)
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

                if (string.IsNullOrWhiteSpace(packageVersion) &&
                    string.IsNullOrWhiteSpace(versionFile))
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
                else
                {
                    var versionFromFile = await GetVersionFromFile(versionFile!.NormalizePath());

                    if (versionFromFile is null)
                    {
                        logger.Error("Could not get version from file '{VersionFile}'", versionFile);
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
                    cancellationToken);
            }

            Command NuSpec()
            {
                return new Command(
                    "create",
                    Strings.CreateDescription,
                    new[] {SourceDir(), OutputFile(), PackageId(), PackageVersion(), VersionFile()},
                    handler: CommandHandler.Create<string, string, string, string, string>(Bind));
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
        }
    }
}