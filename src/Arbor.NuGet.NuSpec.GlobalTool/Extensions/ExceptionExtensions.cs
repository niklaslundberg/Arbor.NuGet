﻿using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Arbor.NuGet.NuSpec.GlobalTool.Extensions
{
    internal static class ExceptionExtensions
    {
        public static bool IsFatal(this Exception? ex)
        {
            if (ex == null)
            {
                return false;
            }

            return ex is StackOverflowException || ex is OutOfMemoryException || ex is AccessViolationException
                   || ex is AppDomainUnloadedException || ex is ThreadAbortException || ex is SEHException;
        }
    }
}