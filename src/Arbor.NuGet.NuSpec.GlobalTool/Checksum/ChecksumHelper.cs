using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Arbor.FS;
using Newtonsoft.Json;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.Checksum;

internal static class ChecksumHelper
{
    public static async Task<FileListWithChecksumFile> CreateFileListForDirectory(
        DirectoryEntry baseDirectory,
        DirectoryEntry targetDirectory,
        Predicate<FileEntry> filter)
    {
        var fileEntries = baseDirectory.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(file => filter(file))
            .OrderBy(file => file.FullName)
            .Select(file => file);

        var files = new List<(string, string)>();

        foreach (var fileEntry in fileEntries)
        {
            string fileHashSha512Base64Encoded = await GetFileHashSha512Base64Encoded(fileEntry);

            string internalFullPath = fileEntry.FileSystem.ConvertPathToInternal(fileEntry.Path);

            string internalBaseDirectoryFullName = fileEntry.FileSystem.ConvertPathToInternal(baseDirectory.Path);

            string file = internalFullPath[internalBaseDirectoryFullName.Length..];

            files.Add((file, fileHashSha512Base64Encoded));
        }

        string json = JsonConvert.SerializeObject(new {files}, Formatting.Indented);

        var tempDirectory = new DirectoryEntry(baseDirectory.FileSystem,
                UPath.Combine(Path.GetTempPath().ParseAsPath(), Guid.NewGuid().ToString()))
            .EnsureExists();

        var contentFilesFile = new FileEntry(baseDirectory.FileSystem,
            UPath.Combine(tempDirectory.FullName, "contentFiles.json"));

        await contentFilesFile.WriteAllTextAsync(json, Encoding.UTF8);

        string contentFilesFileChecksum = await GetFileHashSha512Base64Encoded(contentFilesFile);

        var hashFile = new FileEntry(baseDirectory.FileSystem,
            UPath.Combine(tempDirectory.FullName, "contentFiles.json.sha512"));

        await hashFile.WriteAllTextAsync(contentFilesFileChecksum, Encoding.UTF8);

        string hashFileName = hashFile.Name;
        var hashTargetPath = UPath.Combine(targetDirectory.FullName, hashFileName);
        hashFile.CopyTo(hashTargetPath, overwrite: true);

        string contentFilesFileName = contentFilesFile.Name;
        var contentFilesTargetPath = UPath.Combine(targetDirectory.FullName, contentFilesFileName);
        contentFilesFile.CopyTo(contentFilesTargetPath, overwrite: true);

        return new(contentFilesFileName, hashFileName);
    }

    private static async Task<string> GetFileHashSha512Base64Encoded(FileEntry fileName)
    {
        using var hashAlgorithm = SHA512.Create();

        await using var fs = fileName.Open(FileMode.Open, FileAccess.Read);

        byte[] fileHash = await hashAlgorithm.ComputeHashAsync(fs);

        return Convert.ToBase64String(fileHash);
    }
}