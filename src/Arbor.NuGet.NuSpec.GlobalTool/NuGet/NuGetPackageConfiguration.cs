using System.IO;

using JetBrains.Annotations;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet
{
    public class NuGetPackageConfiguration
    {
        public NuGetPackageConfiguration(
            [NotNull] PackageDefinition packageDefinition,
            [NotNull] DirectoryInfo sourceDirectory,
            string outputFile)
        {
            PackageDefinition = packageDefinition;
            SourceDirectory = sourceDirectory;
            OutputFile = outputFile;
        }

        public string OutputFile { get; }

        [NotNull]
        public PackageDefinition PackageDefinition { get; }

        [NotNull]
        public DirectoryInfo SourceDirectory { get; }
    }
}