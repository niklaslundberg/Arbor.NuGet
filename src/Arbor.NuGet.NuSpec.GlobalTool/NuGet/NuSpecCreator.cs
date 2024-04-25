using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.FS;
using Arbor.NuGet.NuSpec.GlobalTool.Checksum;
using Arbor.Processing;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet;

internal sealed class NuSpecCreator
{
    private const string NuSpecFileExtension = ".nuspec";
    private const string NugetOrgUrl = "https://nuget.org";

    private readonly ILogger _logger;

    private NuSpecCreator(ILogger logger) => _logger = logger;

    public static async Task<int> CreateSpecificationAsync(
        PackageDefinition packageDefinition,
        ILogger logger,
        DirectoryEntry sourceDirectory,
        UPath outputFile,
        CancellationToken cancellationToken)
    {
        var nuSpecCreator = new NuSpecCreator(logger);

        var nuGetPackageConfiguration =
            new NuGetPackageConfiguration(packageDefinition, sourceDirectory, outputFile);

        var exitCode = await nuSpecCreator.CreateNuGetPackageAsync(nuGetPackageConfiguration, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        return exitCode.Code;
    }

    private async Task<ExitCode> CreateNuGetPackageAsync(
        NuGetPackageConfiguration packageConfiguration,
        CancellationToken cancellationToken)
    {
        var packageDirectory = packageConfiguration.SourceDirectory;

        if (!packageDirectory.Exists)
        {
            _logger.Error("The source directory '{Directory}' does not exist", packageDirectory.FullName);
            return ExitCode.Failure;
        }

        string? extensionWithDot = packageConfiguration.OutputFile.GetExtensionWithDot();

        if (string.IsNullOrWhiteSpace(extensionWithDot))
        {
            _logger.Error("The output file is missing extension");
            return ExitCode.Failure;
        }

        if (!extensionWithDot.Equals(NuSpecFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Error("The output file must have the extension {Extension}", NuSpecFileExtension);
            return ExitCode.Failure;
        }

        string packageId = packageConfiguration.PackageDefinition.PackageId.Id;
        string? normalizedVersion = packageConfiguration.PackageDefinition.SemanticVersion.ToNormalizedString();
        string description = packageId;
        string summary = packageId;
        const string language = "en-US";
        const string projectUrl = NugetOrgUrl;
        const string iconUrl = NugetOrgUrl;
        const string requireLicenseAcceptance = "false";
        const string licenseUrl = NugetOrgUrl;
        const string copyright = "Undefined";
        string tags = string.Empty;

        var fileList = packageDirectory.EnumerateFiles("*", SearchOption.AllDirectories);

        string files = string.Join(
            Environment.NewLine,
            fileList.Select(file => NuSpecHelper.IncludedFile(file, packageDirectory)));

        var targetDirectory =
            new FileEntry(packageConfiguration.SourceDirectory.FileSystem, packageConfiguration.OutputFile)
                .Directory;

        targetDirectory.EnsureExists();

        var contentFilesInfo = await ChecksumHelper.CreateFileListForDirectory(packageDirectory, targetDirectory)
            .ConfigureAwait(continueOnCapturedContext: false);

        string contentFileListFile =
            $@"<file src=""{contentFilesInfo.ContentFilesFile}"" target=""{contentFilesInfo.ContentFilesFile}"" />";

        string checksumFile =
            $@"<file src=""{contentFilesInfo.ChecksumFile}"" target=""{contentFilesInfo.ChecksumFile}"" />";

        string nuspecContent = $@"<?xml version=""1.0""?>
<package>
    <metadata>
        <id>{packageId}</id>
        <version>{normalizedVersion}</version>
        <title>{packageId}</title>
        <authors>Authors</authors>
        <owners>Owners</owners>
        <description>
            {description}
        </description>
        <releaseNotes>
        </releaseNotes>
        <summary>
            {summary}
        </summary>
        <language>{language}</language>
        <projectUrl>{projectUrl}</projectUrl>
        <iconUrl>{iconUrl}</iconUrl>
        <requireLicenseAcceptance>{requireLicenseAcceptance}</requireLicenseAcceptance>
        <licenseUrl>{licenseUrl}</licenseUrl>
        <copyright>{copyright}</copyright>
        <dependencies>

        </dependencies>
        <references></references>
        <tags>{tags}</tags>
    </metadata>
    <files>
        {files}
        {contentFileListFile}
        {checksumFile}
    </files>
</package>";

        _logger.Information("{NuSpec}", nuspecContent);

        await using var tempDir = TempDirectory.Create(packageConfiguration.SourceDirectory.FileSystem,
            $"Arbor.NuGet_{Guid.NewGuid()}");

        var nuspecTempFile = UPath.Combine(tempDir.Directory.FullName, $"{packageId}.nuspec");

        tempDir.Directory.FileSystem.DeleteFile(nuspecTempFile);

        await tempDir.Directory.FileSystem
            .WriteAllTextAsync(nuspecTempFile, nuspecContent, Encoding.UTF8, cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        var tempFile = tempDir.Directory.FileSystem.GetFileEntry(nuspecTempFile);
        tempFile.CopyTo(packageConfiguration.OutputFile, overwrite: true);

        return ExitCode.Success;
    }
}