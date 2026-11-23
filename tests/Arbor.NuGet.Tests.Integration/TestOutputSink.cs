using System.Globalization;
using System.IO;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;
using Xunit;

namespace Arbor.NuGet.Tests.Integration;

public sealed class TestOutputSink(ITestOutputHelper testOutputHelper) : ILogEventSink
{
    private readonly MessageTemplateTextFormatter _formatter = new(
        "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        CultureInfo.InvariantCulture);

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        testOutputHelper.WriteLine(writer.ToString().TrimEnd());
    }
}

public static class TestOutputSinkExtensions
{
    public static LoggerConfiguration TestOutput(
        this LoggerSinkConfiguration configuration,
        ITestOutputHelper testOutputHelper) => configuration.Sink(new TestOutputSink(testOutputHelper));
}
