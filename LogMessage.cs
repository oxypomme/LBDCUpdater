using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LBDCUpdater
{
    internal enum LogSeverity
    {
        Critical,
        Debug,
        Error,
        Info,
        Warning
    }

    internal record LogMessage(string Message, LogSeverity Severity = LogSeverity.Info, Exception? Exception = null);
}