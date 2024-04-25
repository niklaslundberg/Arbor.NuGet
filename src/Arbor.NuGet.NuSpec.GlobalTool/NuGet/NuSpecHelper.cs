using System;
using System.IO;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.NuGet;

internal static class NuSpecHelper
{
    public static string IncludedFile(FileEntry file, DirectoryEntry baseDirectory)
    {
        if (!file.FullName.StartsWith(baseDirectory.FullName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"The file must {file} be in base directory {baseDirectory}",
                nameof(file));
        }

        string internalTargetPath = file.FileSystem.ConvertPathToInternal(file.Path);

        string targetFilePath = internalTargetPath[file.FileSystem.ConvertPathToInternal(baseDirectory.Path).Length..];

        string fileItem = $@"<file src=""{internalTargetPath}"" target=""Content{Path.DirectorySeparatorChar}{targetFilePath}"" />";

        return fileItem.Replace("\\\\", "\\", StringComparison.Ordinal);
    }
}