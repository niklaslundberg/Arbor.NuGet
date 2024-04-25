using System;

namespace Arbor.NuGet.NuSpec.GlobalTool.Checksum;

internal class FileListWithChecksumFile
{
    public FileListWithChecksumFile(string contentFilesFile, string checksumFile)
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