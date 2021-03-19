using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Arbor.FS;
using NuGet.Packaging;
using Serilog;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet
{
    internal static class NuGetPacker
    {
        public static Task<int> PackNuSpec(FileEntry nuSpecFile,
            UPath packagePath,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            if (!nuSpecFile.Exists)
            {
                throw new ArgumentException($"The nuspec file {nuSpecFile.Path} does not exist");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Cancellation is requested");
            }

            return PackNuSpecInternal(nuSpecFile, packagePath, logger);
        }

        private static async Task<int> PackNuSpecInternal(FileEntry nuSpecFile,
            UPath packagePath,
            ILogger logger)
        {
            new DirectoryEntry(nuSpecFile.FileSystem, packagePath.GetDirectory()).EnsureExists();

            await using var nuspecStream = nuSpecFile.Open(FileMode.Open, FileAccess.Read);

            string baseDirectory = nuSpecFile.FileSystem.ConvertPathToInternal(nuSpecFile.Directory.Path);

            var builder = new PackageBuilder(nuspecStream, baseDirectory);

            await using var packageStream = nuSpecFile.FileSystem.CreateFile(packagePath);

            builder.Save(packageStream);

            logger.Debug("Created NuGet package {Package}", nuSpecFile.FileSystem.ConvertPathToInternal(packagePath));

            return 0;
        }
    }
}