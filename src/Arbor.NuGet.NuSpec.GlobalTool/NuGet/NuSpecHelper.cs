using System;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet
{
    internal static class NuSpecHelper
    {
        public static string IncludedFile(UPath fileName, UPath baseDirectory)
        {
            if (fileName.IsNull ||
                fileName.IsEmpty)
            {
                throw new ArgumentException("Path cannot be null or empty.", nameof(fileName));
            }

            if (baseDirectory.IsNull ||
                baseDirectory.IsEmpty)
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(baseDirectory));
            }

            if (!fileName.FullName.StartsWith(baseDirectory.FullName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"The file must {fileName} be in base directory {baseDirectory}",
                    nameof(fileName));
            }

            var targetFilePath = new UPath(fileName.FullName[baseDirectory.FullName.Length..]);

            string fileItem = $@"<file src=""{fileName}"" target=""Content\{targetFilePath}"" />";

            return fileItem.Replace("\\\\", "\\", StringComparison.Ordinal);
        }
    }
}