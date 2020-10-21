using System;
using JetBrains.Annotations;

namespace Arbor.NuGet.NuSpec.GlobalTool.Checksum
{
    public class FileListWithChecksumFile
    {
        public FileListWithChecksumFile([NotNull] string contentFilesFile, string checksumFile)
        {
            if (string.IsNullOrWhiteSpace(contentFilesFile))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(contentFilesFile));
            }

            ContentFilesFile = contentFilesFile;
            ChecksumFile = checksumFile;
        }

        public string ChecksumFile { get; }

        public string ContentFilesFile { get; }
    }
}