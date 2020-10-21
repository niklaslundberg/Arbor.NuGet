﻿using JetBrains.Annotations;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet
{
    public class NuGetPackageConfiguration
    {
        public NuGetPackageConfiguration(
            [NotNull] PackageDefinition packageDefinition,
            [NotNull] DirectoryEntry sourceDirectory,
            string outputFile)
        {
            PackageDefinition = packageDefinition;
            SourceDirectory = sourceDirectory;
            OutputFile = outputFile;
        }

        public string OutputFile { get; }

        [NotNull] public PackageDefinition PackageDefinition { get; }

        [NotNull] public DirectoryEntry SourceDirectory { get; }
    }
}