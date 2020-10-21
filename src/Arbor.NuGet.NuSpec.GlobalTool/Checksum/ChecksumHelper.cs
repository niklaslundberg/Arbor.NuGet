using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Arbor.FS;
using Arbor.NuGet.NuSpec.GlobalTool.Extensions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.Checksum
{
    public static class ChecksumHelper
    {
        public static async Task<FileListWithChecksumFile> CreateFileListForDirectory(
            [NotNull] DirectoryEntry baseDirectory,
            DirectoryEntry targetDirectory)
        {
            if (baseDirectory == null)
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            var files = baseDirectory.EnumerateFiles("*", SearchOption.AllDirectories).OrderBy(file => file.FullName)
                .Select(file => file).Select(
                    file => new
                    {
                        file = file.FullName[baseDirectory.FullName.Length..],
                        sha512Base64Encoded = GetFileHashSha512Base64Encoded(file)
                    }).ToArray();

            string? json = JsonConvert.SerializeObject(new {files}, Formatting.Indented);

            var tempDirectory = new DirectoryEntry(baseDirectory.FileSystem, UPath.Combine(Path.GetTempPath().NormalizePath(), Guid.NewGuid().ToString()))
                .EnsureExists();

            var contentFilesFile = new FileEntry(baseDirectory.FileSystem, UPath.Combine(tempDirectory.FullName, "contentFiles.json"));

            await contentFilesFile.WriteAllTextAsync(json, Encoding.UTF8).ConfigureAwait(false);;

            string contentFilesFileChecksum = await GetFileHashSha512Base64Encoded(contentFilesFile).ConfigureAwait(false);;

            FileEntry hashFile = new FileEntry(baseDirectory.FileSystem, UPath.Combine(tempDirectory.FullName, "contentFiles.json.sha512"));

            await hashFile.WriteAllTextAsync(contentFilesFileChecksum, Encoding.UTF8).ConfigureAwait(false);

            string hashFileName = hashFile.Name;
            var hashTargetPath = UPath.Combine(targetDirectory.FullName, hashFileName);
            hashFile.CopyTo(hashTargetPath, true);

            var contentFilesFileName = contentFilesFile.Name;
            var contentFilesTargetPath = UPath.Combine(targetDirectory.FullName, contentFilesFileName);
            contentFilesFile.CopyTo(contentFilesTargetPath, true);

            var fileListWithChecksumFile = new FileListWithChecksumFile(contentFilesFileName, hashFileName);

            return fileListWithChecksumFile;
        }

        private static async Task<string> GetFileHashSha512Base64Encoded(FileEntry fileName)
        {
            using var hashAlgorithm = SHA512.Create();

            await using var fs = fileName.Open(FileMode.Open, FileAccess.Read);

            byte[] fileHash = hashAlgorithm.ComputeHash(fs);

            return Convert.ToBase64String(fileHash);
        }
    }
}