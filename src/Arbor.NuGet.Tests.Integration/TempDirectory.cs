using System;
using System.IO;
using Arbor.FS;
using Arbor.NuGet.NuSpec.GlobalTool.Extensions;
using Zio;

namespace Arbor.NuGet.Tests.Integration
{
    internal sealed class TempDirectory : IDisposable
    {
        private DirectoryEntry? _directory;

        private TempDirectory(DirectoryEntry directory) => _directory = directory;

        public DirectoryEntry Directory
        {
            get
            {
                if (_directory is null)
                {
                    throw new ObjectDisposedException(nameof(Directory));
                }

                return _directory;
            }
        }

        public void Dispose()
        {
            if (_directory is null)
            {
                return;
            }

            var tmp = _directory;
            _directory = null;

            tmp?.DeleteIfExists();
        }

        public static TempDirectory Create(IFileSystem fileSystem)
        {
            var directory = UPath.Combine(Path.GetTempPath().NormalizePath(), Guid.NewGuid().ToString());

            if (fileSystem.DirectoryExists(directory))
            {
                throw new InvalidOperationException("The temp directory already exists");
            }

            if (!fileSystem.DirectoryExists(directory))
            {
                fileSystem.CreateDirectory(directory);
            }

            var directoryEntry = fileSystem.GetDirectoryEntry(directory);

            return new TempDirectory(directoryEntry);
        }
    }
}