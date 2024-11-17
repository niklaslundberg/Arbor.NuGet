using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Arbor.NuGet.Tests.Integration;

public sealed class ActionSink(string outputTemplate = "{Message:l}") : ILogEventSink, IDisposable
{
    private readonly List<string> _logEvents = [];
    private readonly MessageTemplateTextFormatter _formatter = new(outputTemplate, CultureInfo.InvariantCulture);

    public ImmutableArray<string> LogEvents => [.._logEvents];

    public void Dispose() => _logEvents.Clear();

    public void Emit(LogEvent logEvent)
    {
        var stringBuilder = new StringBuilder();
        _formatter.Format(logEvent, new StringWriter(stringBuilder));
        _logEvents.Add(stringBuilder.ToString());
    }
}