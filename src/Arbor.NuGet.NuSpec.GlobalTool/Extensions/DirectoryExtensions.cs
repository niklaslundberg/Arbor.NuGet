using System;
using System.IO;
using System.Linq;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool.Extensions
{
    internal static class DirectoryExtensions
    {
        public static void DeleteIfExists(this DirectoryEntry? directoryInfo, bool recursive = true)
        {
            try
            {
                if (directoryInfo is null)
                {
                    return;
                }

                if (directoryInfo.Exists)
                {
                    FileEntry[] fileInfos;

                    try
                    {
                        fileInfos = directoryInfo.EnumerateFiles().ToArray();
                    }
                    catch (Exception ex)
                    {
                        if (ex.IsFatal())
                        {
                            throw;
                        }

                        throw new IOException(
                            $"Could not get files for directory '{directoryInfo.FullName}' for deletion",
                            ex);
                    }

                    foreach (var file in fileInfos)
                    {
                        file.Attributes = FileAttributes.Normal;

                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            if (ex.IsFatal())
                            {
                                throw;
                            }

                            throw new IOException($"Could not delete file '{file.FullName}'", ex);
                        }
                    }

                    foreach (var subDirectory in directoryInfo.EnumerateDirectories())
                    {
                        subDirectory.DeleteIfExists(recursive);
                    }
                }

                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(recursive);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                if (directoryInfo != null)
                {
                    throw new InvalidOperationException($"Could not delete directory '{directoryInfo.FullName}'", ex);
                }

                throw;
            }
        }

        public static DirectoryEntry EnsureExists(this DirectoryEntry directoryInfo)
        {
            if (directoryInfo == null)
            {
                throw new ArgumentNullException(nameof(directoryInfo));
            }

            try
            {
                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }
            }
            catch (PathTooLongException ex)
            {
                throw new PathTooLongException(
                    $"Could not create directory '{directoryInfo.FullName}', path length {directoryInfo.FullName.Length}",
                    ex);
            }

            return directoryInfo;
        }
    }
}