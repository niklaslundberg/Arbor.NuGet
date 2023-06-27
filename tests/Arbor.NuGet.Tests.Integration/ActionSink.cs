using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Arbor.NuGet.Tests.Integration
{
    public sealed class ActionSink : ILogEventSink, IDisposable
    {
        private readonly List<string> _logEvents;
        private readonly MessageTemplateTextFormatter _formatter;

        public ActionSink(string outputTemplate = "{Message:l}")
        {
            _formatter = new MessageTemplateTextFormatter(outputTemplate, CultureInfo.InvariantCulture);
            _logEvents = new List<string>();
        }

        public ImmutableArray<string> LogEvents => _logEvents.ToImmutableArray();

        public void Dispose() => _logEvents.Clear();

        public void Emit(LogEvent logEvent)
        {
            var stringBuilder = new StringBuilder();
            _formatter.Format(logEvent, new StringWriter(stringBuilder));
            _logEvents.Add(stringBuilder.ToString());
        }
    }
}