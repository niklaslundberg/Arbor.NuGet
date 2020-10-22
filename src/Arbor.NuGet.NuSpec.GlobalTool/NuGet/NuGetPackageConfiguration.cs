using JetBrains.Annotations;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet
{
    internal class NuGetPackageConfiguration
    {
        public NuGetPackageConfiguration(
            [NotNull] PackageDefinition packageDefinition,
            [NotNull] DirectoryEntry sourceDirectory,
            UPath outputFile)
        {
            PackageDefinition = packageDefinition;
            SourceDirectory = sourceDirectory;
            OutputFile = outputFile;
        }

        public UPath OutputFile { get; }

        [NotNull] public PackageDefinition PackageDefinition { get; }

        [NotNull] public DirectoryEntry SourceDirectory { get; }
    }
}