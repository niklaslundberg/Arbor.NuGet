using System;
using System.IO;
using System.Threading.Tasks;
using Arbor.FS;
using Arbor.NuGet.NuSpec.GlobalTool.Extensions;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool
{
    internal sealed class TempDirectory : IDisposable, IAsyncDisposable
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

        public async ValueTask DisposeAsync()
        {
            int attempt = 0;

            const int maxAttempts = 10;

            while (attempt <= maxAttempts)
            {
                try
                {
                    Dispose();
                    return;
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    attempt++;
                    await Task.Delay(TimeSpan.FromMilliseconds(value: 50)).ConfigureAwait(false);
                }
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

        public static TempDirectory Create(IFileSystem fileSystem, string? prefix = null)
        {
            var directoryPath = UPath.Combine(Path.GetTempPath().ParseAsPath(), prefix + "_" + Guid.NewGuid());

            if (fileSystem.DirectoryExists(directoryPath))
            {
                throw new InvalidOperationException("The temp directory already exists");
            }

            var directoryEntry = new DirectoryEntry(fileSystem, directoryPath);

            directoryEntry.EnsureExists();

            return new TempDirectory(directoryEntry);
        }
    }
}