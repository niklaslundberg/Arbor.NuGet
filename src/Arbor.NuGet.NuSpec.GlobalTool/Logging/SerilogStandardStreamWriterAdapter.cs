﻿using System;
using System.Collections.Generic;
using System.CommandLine.IO;
using System.Linq;
using Serilog;

namespace Arbor.NuGet.NuSpec.GlobalTool.Logging
{
    internal sealed class SerilogStandardStreamWriterAdapter : IStandardStreamWriter, IDisposable
    {
        private readonly List<string> _buffer = new();
        private readonly ILogger _logger;

        public SerilogStandardStreamWriterAdapter(ILogger logger) => _logger = logger;

        public void Dispose() => Flush();

        public void Write(string value)
        {
            _buffer.Add(value);

            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            Flush();
        }

        private void Flush()
        {
            if (_buffer.Any())
            {
                _logger.Information("{Message}", string.Concat(_buffer));
            }

            _buffer.Clear();
        }
    }
}