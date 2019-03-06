using System;
using System.IO;

namespace Arbor.NuGet.Tests.Integration
{
    internal sealed class TempDirectory : IDisposable
    {
        private string _directory;

        private TempDirectory(string directory)
        {
            _directory = directory;
        }

        public DirectoryInfo Directory => new DirectoryInfo(_directory);

        public static TempDirectory Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            if (System.IO.Directory.Exists(directory))
            {
                throw new InvalidOperationException("The temp directory already exists");
            }

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            return new TempDirectory(directory);
        }

        public void Dispose()
        {
            if (_directory is null)
            {
                return;
            }

            if (System.IO.Directory.Exists(_directory))
            {
                System.IO.Directory.Delete(_directory, true);
            }

            _directory = null;
        }
    }
}