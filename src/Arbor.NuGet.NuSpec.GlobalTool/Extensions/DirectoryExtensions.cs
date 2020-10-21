using System;
using System.IO;

namespace Arbor.NuGet.NuSpec.GlobalTool.Extensions
{
    public static class DirectoryExtensions
    {
        public static void DeleteIfExists(this DirectoryInfo directoryInfo, bool recursive = true)
        {
            try
            {
                if (directoryInfo is null)
                {
                    return;
                }

                directoryInfo.Refresh();

                if (directoryInfo.Exists)
                {
                    FileInfo[] fileInfos;

                    try
                    {
                        fileInfos = directoryInfo.GetFiles();
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

                    foreach (var subDirectory in directoryInfo.GetDirectories())
                    {
                        subDirectory.DeleteIfExists(recursive);
                    }
                }

                directoryInfo.Refresh();

                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(recursive);
                }

                directoryInfo.Refresh();
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

        public static DirectoryInfo EnsureExists(this DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null)
            {
                throw new ArgumentNullException(nameof(directoryInfo));
            }

            try
            {
                directoryInfo.Refresh();

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

            directoryInfo.Refresh();

            return directoryInfo;
        }
    }
}