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
                                file = file.Substring(baseDirectory.FullName.Length),
                                sha512Base64Encoded = ChecksumHelper.GetFileHashSha512Base64Encoded(file)
                            }).ToArray();

            var json = JsonConvert.SerializeObject(new { files }, Formatting.Indented);

            var tempDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
                .EnsureExists();

            var contentFilesFile = Path.Combine(tempDirectory.FullName, "contentFiles.json");

            File.WriteAllText(contentFilesFile, json, Encoding.UTF8);

            var contentFilesFileChecksum = ChecksumHelper.GetFileHashSha512Base64Encoded(contentFilesFile);

            var hashFile = Path.Combine(tempDirectory.FullName, "contentFiles.json.sha512");

            File.WriteAllText(hashFile, contentFilesFileChecksum, Encoding.UTF8);

            var hashFileName = Path.GetFileName(hashFile);
            var hashTargetPath = Path.Combine(targetDirectory.FullName, hashFileName);
            File.Copy(hashFile, hashTargetPath);

            var contentFilesFileName = Path.GetFileName(contentFilesFile);
            var contentFilesTargetPath = Path.Combine(targetDirectory.FullName, contentFilesFileName);
            File.Copy(contentFilesFile, contentFilesTargetPath);

            var fileListWithChecksumFile = new FileListWithChecksumFile(contentFilesFileName, hashFileName);

            return fileListWithChecksumFile;
        }

        private static string GetFileHashSha512Base64Encoded(string fileName)
        {
            byte[] fileHash;
            using (var hashAlgorithm = SHA512.Create())
            {
                using (var fs = new FileStream(fileName, FileMode.Open))
                {
                    fileHash = hashAlgorithm.ComputeHash(fs);
                }
            }

            return Convert.ToBase64String(fileHash);
        }
    }
}