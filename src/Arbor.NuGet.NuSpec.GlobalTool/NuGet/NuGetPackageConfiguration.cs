using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet;

internal class NuGetPackageConfiguration(
    PackageDefinition packageDefinition,
    DirectoryEntry sourceDirectory,
    UPath outputFile)
{
    public UPath OutputFile { get; } = outputFile;

    public PackageDefinition PackageDefinition { get; } = packageDefinition;

    public DirectoryEntry SourceDirectory { get; } = sourceDirectory;
}