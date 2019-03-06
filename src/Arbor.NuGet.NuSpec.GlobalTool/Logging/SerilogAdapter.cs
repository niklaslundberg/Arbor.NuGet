using System;
using System.CommandLine;

using Serilog;

namespace Arbor.NuGet.NuSpec.GlobalTool.Logging
{
    public sealed class SerilogAdapter : IConsole, IDisposable
    {
        public SerilogAdapter(ILogger logger)
        {
            Out = new SerilogStandardStreamWriterAdapter(logger);
            Error = new SerilogStandardStreamWriterAdapter(logger);
        }

        public IStandardStreamWriter Error { get; }

        public bool IsErrorRedirected { get; } = true;

        public bool IsInputRedirected { get; } = true;

        public bool IsOutputRedirected { get; } = true;

        public IStandardStreamWriter Out { get; }

        public void Dispose()
        {
            if (Out is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (Error is IDisposable errorDisposable)
            {
                errorDisposable.Dispose();
            }
        }
    }
}