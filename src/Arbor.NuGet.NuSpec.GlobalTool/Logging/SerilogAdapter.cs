using System;
using System.CommandLine;
using System.CommandLine.IO;
using Serilog;

namespace Arbor.NuGet.NuSpec.GlobalTool.Logging;

internal sealed class SerilogAdapter(ILogger logger) : IConsole, IDisposable
{
    public IStandardStreamWriter Error { get; } = new SerilogStandardStreamWriterAdapter(logger);

    public bool IsErrorRedirected { get; } = true;

    public bool IsInputRedirected { get; } = true;

    public bool IsOutputRedirected { get; } = true;

    public IStandardStreamWriter Out { get; } = new SerilogStandardStreamWriterAdapter(logger);

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