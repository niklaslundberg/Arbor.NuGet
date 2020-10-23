using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.FS;
using Arbor.NuGet.NuSpec.GlobalTool.Checksum;
using Arbor.NuGet.NuSpec.GlobalTool.Extensions;
using Arbor.Processing;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet
{
    internal class NuSpecCreator
    {
        private const string NuSpecFileExtension = ".nuspec";

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

            if (string.IsNullOrWhiteSpace(packageConfiguration.OutputFile.GetExtensionWithDot()))
            {
                _logger.Error("The output file is missing extension");
                return ExitCode.Failure;
            }

            if (!packageConfiguration.OutputFile.GetExtensionWithDot()
                .Equals(NuSpecFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Error("The output file must have the extension {Extension}", NuSpecFileExtension);
                return ExitCode.Failure;
            }

            string packageId = packageConfiguration.PackageDefinition.PackageId.Id;
            string? normalizedVersion = packageConfiguration.PackageDefinition.SemanticVersion.ToNormalizedString();
            string description = packageId;
            string summary = packageId;
            const string Language = "en-US";
            const string ProjectUrl = "http://nuget.org";
            const string IconUrl = "http://nuget.org";
            const string RequireLicenseAcceptance = "false";
            const string LicenseUrl = "http://nuget.org";
            string copyright = "Undefined";
            string tags = string.Empty;

            var fileList = packageDirectory.EnumerateFiles("*", SearchOption.AllDirectories);

            string files = string.Join(
                Environment.NewLine,
                fileList.Select(file => NuSpecHelper.IncludedFile(file, packageDirectory)));

            var targetDirectory = new FileEntry(packageConfiguration.SourceDirectory.FileSystem, packageConfiguration.OutputFile).Directory!;

            targetDirectory.EnsureExists();

            var contentFilesInfo = await ChecksumHelper.CreateFileListForDirectory(packageDirectory, targetDirectory).ConfigureAwait(false);

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
        <language>{Language}</language>
        <projectUrl>{ProjectUrl}</projectUrl>
        <iconUrl>{IconUrl}</iconUrl>
        <requireLicenseAcceptance>{RequireLicenseAcceptance}</requireLicenseAcceptance>
        <licenseUrl>{LicenseUrl}</licenseUrl>
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

            var tempDir = new DirectoryEntry( packageConfiguration.SourceDirectory.FileSystem, UPath.Combine(Path.GetTempPath().NormalizePath(), $"Arbor.NuGet_{DateTime.Now.Ticks}"))
                .EnsureExists();

            var nuspecTempFile = UPath.Combine(tempDir.FullName, $"{packageId}.nuspec");

            await tempDir.FileSystem.WriteAllTextAsync(nuspecTempFile, nuspecContent, Encoding.UTF8, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            var tempFile = tempDir.FileSystem.GetFileEntry(nuspecTempFile);
            tempFile.CopyTo(packageConfiguration.OutputFile, true);

            tempFile.Delete();

            tempDir.DeleteIfExists();

            return ExitCode.Success;
        }
    }
}