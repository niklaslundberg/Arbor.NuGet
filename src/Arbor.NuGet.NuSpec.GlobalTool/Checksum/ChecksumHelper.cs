using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Arbor.NuGet.NuSpec.GlobalTool.Extensions;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Arbor.NuGet.NuSpec.GlobalTool.Checksum
{
    public static class ChecksumHelper
    {
        public static FileListWithChecksumFile CreateFileListForDirectory(
            [NotNull] DirectoryInfo baseDirectory,
            DirectoryInfo targetDirectory)
        {
            if (baseDirectory == null)
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            var files = baseDirectory.GetFiles("*", SearchOption.AllDirectories).OrderBy(file => file.FullName)
                .Select(file => file.FullName).Select(
                    file => new
                    {
                        file = file[baseDirectory.FullName.Length..],
                        sha512Base64Encoded = GetFileHashSha512Base64Encoded(file)
                    }).ToArray();

            string? json = JsonConvert.SerializeObject(new {files}, Formatting.Indented);

            var tempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
                .EnsureExists();

            string contentFilesFile = Path.Combine(tempDirectory.FullName, "contentFiles.json");

            File.WriteAllText(contentFilesFile, json, Encoding.UTF8);

            string contentFilesFileChecksum = GetFileHashSha512Base64Encoded(contentFilesFile);

            string hashFile = Path.Combine(tempDirectory.FullName, "contentFiles.json.sha512");

            File.WriteAllText(hashFile, contentFilesFileChecksum, Encoding.UTF8);

            string hashFileName = Path.GetFileName(hashFile);
            string hashTargetPath = Path.Combine(targetDirectory.FullName, hashFileName);
            File.Copy(hashFile, hashTargetPath);

            string contentFilesFileName = Path.GetFileName(contentFilesFile);
            string contentFilesTargetPath = Path.Combine(targetDirectory.FullName, contentFilesFileName);
            File.Copy(contentFilesFile, contentFilesTargetPath);

            var fileListWithChecksumFile = new FileListWithChecksumFile(contentFilesFileName, hashFileName);

            return fileListWithChecksumFile;
        }

        private static string GetFileHashSha512Base64Encoded(string fileName)
        {
            using var hashAlgorithm = SHA512.Create();

            using var fs = new FileStream(fileName, FileMode.Open);

            byte[] fileHash = hashAlgorithm.ComputeHash(fs);

            return Convert.ToBase64String(fileHash);
        }
    }
}