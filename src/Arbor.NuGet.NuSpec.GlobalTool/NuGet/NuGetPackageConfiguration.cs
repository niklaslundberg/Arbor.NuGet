using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet;

internal class NuGetPackageConfiguration
{
    public NuGetPackageConfiguration(
        PackageDefinition packageDefinition,
        DirectoryEntry sourceDirectory,
        UPath outputFile)
    {
        PackageDefinition = packageDefinition;
        SourceDirectory = sourceDirectory;
        OutputFile = outputFile;
    }

    public UPath OutputFile { get; }

    public PackageDefinition PackageDefinition { get; }

    public DirectoryEntry SourceDirectory { get; }
}