﻿using System;
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

namespace Arbor.NuGet.NuSpec.GlobalTool.CommandLine;

internal static class NuSpecCommandDefinition
{
    private static readonly Option SourceDirectory =
        new Option<string?>(
            "--source-directory",
            Strings.SourceDirectoryDescription);

    private static readonly Option OutputFile =
        new Option<string?>(
            "--output-file",
            Strings.OutputFileDescription);

    private static readonly Option PackageId =
        new Option<string?>(
            "--package-id",
            Strings.PackageIdDescription);

    private static readonly Option PackageVersion =
        new Option<string?>(
            "--package-version",
            Strings.PackageVersionDescription);

    private static readonly Option VersionFile =
        new Option<string?>(
            "--version-file",
            Strings.VersionFile);

    private static readonly Option MsBuildVersionFile =
        new Option<string?>(
            "--msbuild-version-file",
            Strings.MsBuildVersionFile);

    private static readonly Option PackageDirectory =
        new Option<string?>(
            "--package-directory",
            Strings.PackageOutputDirectory);

    private static readonly Option PreReleaseVersion =
        new Option<string?>(
            "--pre-release-version",
            Strings.PackageOutputDirectory);

    private static SemanticVersion GetVersion(CommandOptions options,
        IFileSystem fileSystem,
        ILogger logger)
    {
        SemanticVersion? version = null;

        if (!string.IsNullOrWhiteSpace(options.PackageVersion))
        {
            if (SemanticVersion.TryParse(options.PackageVersion, out SemanticVersion? parsedVersion))
            {
                version = parsedVersion;
            }
            else
            {
                logger.Error("Invalid semver '{PackageVersion}'", options.PackageVersion);
            }
        }
        else if (!string.IsNullOrWhiteSpace(options.VersionFile))
        {
            var versionFromFile =
                JsonFileVersionHelper.GetVersionFromJsonFile(options.VersionFile!.ParseAsPath(), fileSystem, logger);

            if (versionFromFile is { })
            {
                version = versionFromFile;
            }
            else
            {
                logger.Error("Could not get version from file '{VersionFile}'", options.VersionFile);
            }
        }
        else
        {
            var versionFromFile =
                JsonFileVersionHelper.GetVersionFromMsBuildFile(options.MsBuildVersionFile!.ParseAsPath(), fileSystem,
                    logger);

            if (versionFromFile is { })
            {
                version = versionFromFile;
            }
            else
            {
                logger.Error("Could not get version from MSBuild file '{VersionFile}'", options.MsBuildVersionFile);
            }
        }

        if (version is null)
        {
            throw new InvalidOperationException("Could not get version");
        }

        if (!string.IsNullOrWhiteSpace(options.PreReleaseVersion) && !version.IsPrerelease)
        {
            string preReleaseVersionAttempt = $"{version.ToNormalizedString()}-{options.PreReleaseVersion.TrimStart('-')}";

            if (SemanticVersion.TryParse(preReleaseVersionAttempt, out var preReleaseVersion))
            {
                version = preReleaseVersion;
            }
            else
            {
                throw new InvalidOperationException($"Could not get version with pre-release '{preReleaseVersionAttempt}'");
            }
        }
        else if (!string.IsNullOrWhiteSpace(options.PreReleaseVersion) && version.IsPrerelease)
        {
            logger.Warning("The version number already is already a preview version, skipping pre-release argument '{VersionFile}'", options.MsBuildVersionFile);
        }

        return version;
    }

    public static Command Tool(ILogger logger, IFileSystem fileSystem, CancellationToken cancellationToken)
    {
        var tool = new Command("nuspec", Strings.NuSpecDescription);

        Command NuSpec()
        {
            var command = new Command(
                "create",
                Strings.CreateDescription);

            command.AddOption(SourceDirectory);
            command.AddOption(OutputFile);
            command.AddOption(PackageId);
            command.AddOption(PackageVersion);
            command.AddOption(VersionFile);
            command.AddOption(MsBuildVersionFile);
            command.AddOption(PackageDirectory);
            command.AddOption(PreReleaseVersion);

            command.Handler = CommandHandler.Create<CommandOptions>(options =>
                BindAndValidate(options, logger, fileSystem, cancellationToken));

            return command;
        }

        tool.AddCommand(NuSpec());

        return tool;
    }

    public static Command CreatePackage(ILogger logger, IFileSystem fileSystem, CancellationToken cancellationToken)
    {
        var tool = new Command("package", Strings.NuSpecDescription);

        Command NuSpec()
        {
            var command = new Command(
                "create",
                Strings.CreateDescription);

            command.AddOption(SourceDirectory);
            command.AddOption(PackageId);
            command.AddOption(PackageVersion);
            command.AddOption(VersionFile);
            command.AddOption(MsBuildVersionFile);
            command.AddOption(PackageDirectory);
            command.AddOption(PreReleaseVersion);

            command.Handler = CommandHandler.Create<CommandOptions>(options =>
                BindAndValidate(options, logger, fileSystem, cancellationToken));

            return command;
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
        var version = GetVersion(options, fileSystem, logger);

        var packageDefinition = new PackageDefinition(
            new PackageId(options.PackageId!),
            version);

        await using var tempDirectory = TempDirectory.Create(fileSystem, "arbognuget_nuspec");

        UPath nuspecPath = options.OutputFile?.ParseAsPath() ??
                           tempDirectory.Directory.Path / $"{options.PackageId}_{Guid.NewGuid()}.nuspec";

        int exitCode = await NuSpecCreator.CreateSpecificationAsync(
            packageDefinition,
            logger,
            new DirectoryEntry(fileSystem, options.SourceDirectory!.ParseAsPath()),
            nuspecPath,
            cancellationToken);

        if (exitCode != 0)
        {
            return exitCode;
        }

        if (!string.IsNullOrWhiteSpace(options.PackageDirectory))
        {
            var outputFileEntry = fileSystem.GetFileEntry(nuspecPath);
            string packageFileName = $"{options.PackageId}.{version.ToNormalizedString()}.nupkg";
            var packageFilePath = options.PackageDirectory.ParseAsPath() / packageFileName;

            int packageExitCode = await NuGetPacker.PackNuSpec(
                outputFileEntry, packageFilePath, logger,
                cancellationToken);

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