namespace Arbor.NuGet.NuSpec.GlobalTool.CommandLine;

internal class CommandOptions(
    string? sourceDirectory,
    string? outputFile,
    string? packageId,
    string? packageVersion,
    string? versionFile,
    string? msBuildVersionFile,
    string? packageDirectory,
    string? preReleaseVersion = null)
{
    public string? SourceDirectory { get; } = sourceDirectory;
    public string? OutputFile { get; } = outputFile;
    public string? PackageId { get; } = packageId;
    public string? PackageVersion { get; } = packageVersion;
    public string? VersionFile { get; } = versionFile;
    public string? MsBuildVersionFile { get; } = msBuildVersionFile;
    public string? PackageDirectory { get; } = packageDirectory;
    public string? PreReleaseVersion { get; } = preReleaseVersion;
}