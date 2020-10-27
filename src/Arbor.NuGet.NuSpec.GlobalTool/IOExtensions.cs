using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Arbor.NuGet.NuSpec.GlobalTool.Extensions;
using Zio;

namespace Arbor.NuGet.NuSpec.GlobalTool
{
    internal static class IOExtensions
    {
        public static Task WriteAllTextAsync(this IFileSystem fileSystem,
            UPath path,
            string text,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
        {
            var fileEntry = new FileEntry(fileSystem, path);

            return WriteAllTextAsync(fileEntry, text, encoding, cancellationToken);
        }

        public static async Task WriteAllTextAsync(this FileEntry file,
            string text,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
        {
            file.Directory.EnsureExists();

            await using var stream = file.Open(FileMode.OpenOrCreate, FileAccess.Write);

            await WriteAllTextAsync(stream, text, encoding, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task WriteAllTextAsync(this Stream stream,
            string text,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
        {
            await using var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8, leaveOpen: false);

            await writer.WriteAsync(text.AsMemory(), cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static Task<string> ReadAllTextAsync(this Stream stream,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Cancellation is requested");
            }

            return ReadAllTextInternalAsync(stream, encoding);
        }

        public static Task<string> ReadAllTextAsync(this IFileSystem fileSystem,
            UPath path,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
        {
            var fileEntry = new FileEntry(fileSystem, path);

            return ReadAllTextAsync(fileEntry, encoding, cancellationToken);
        }

        public static async Task<string> ReadAllTextAsync(this FileEntry file,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
        {
            await using var stream = file.Open(FileMode.Open, FileAccess.Read);

            return await ReadAllTextAsync(stream, encoding, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        private static async Task<string> ReadAllTextInternalAsync(this Stream stream,
            Encoding? encoding = null)
        {
            using var reader = new StreamReader(stream, encoding ?? Encoding.UTF8, leaveOpen: false);

            return await reader.ReadToEndAsync().ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}