using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LBDCUpdater
{
    internal class LogStream
    {
        private TextWriter writer;

        public LogStream(string path)
        {
            writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        }

        public void Close()
        {
            writer.Close();
        }

        public void Log(LogMessage message)
        {
            string logSeverity;

            if (message.Severity == LogSeverity.Critical)
                Console.ForegroundColor = ConsoleColor.Red;
            Console.Write('[');
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    logSeverity = "CRITICAL";
                    break;

                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    logSeverity = "ERROR";
                    break;

                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    logSeverity = "DEBUG";
                    break;

                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    logSeverity = "WARNING";
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    logSeverity = "INFO";
                    break;
            }
            Console.Write(logSeverity);
            if (message.Severity != LogSeverity.Critical)
                Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"] {DateTime.Now} - {message.Message}");
            Console.ForegroundColor = ConsoleColor.White;
            writer.WriteLine($"[{logSeverity}] {DateTime.Now} - {message.Message}");
        }

        [Obsolete]
        public void WriteLine(LogMessage value) => Log(value);
    }
}
}