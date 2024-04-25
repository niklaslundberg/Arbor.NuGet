namespace Arbor.NuGet.NuSpec.GlobalTool.CommandLine;

internal class CommandOptions
{
    public CommandOptions(string? sourceDirectory,
        string? outputFile,
        string? packageId,
        string? packageVersion,
        string? versionFile,
        string? msBuildVersionFile,
        string? packageDirectory,
        string? preReleaseVersion = null)
    {
        SourceDirectory = sourceDirectory;
        OutputFile = outputFile;
        PackageId = packageId;
        PackageVersion = packageVersion;
        VersionFile = versionFile;
        MsBuildVersionFile = msBuildVersionFile;
        PackageDirectory = packageDirectory;
        PreReleaseVersion = preReleaseVersion;
    }

    public string? SourceDirectory { get; }
    public string? OutputFile { get; }
    public string? PackageId { get; }
    public string? PackageVersion { get; }
    public string? VersionFile { get; }
    public string? MsBuildVersionFile { get; }
    public string? PackageDirectory { get; }
    public string? PreReleaseVersion { get; }
}