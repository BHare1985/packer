using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SimpleLogger
{
    public class Logger
    {
        private static Writer _writer;

        static Logger()
        {
            _writer = new DefaultWriter();
        }

        public static void SetWriter(Writer writer)
        {
            _writer = writer;
        }

        public static void Log(Level level, string message, [CallerFilePathAttribute] string owner = "")
        {
            _writer.Write(GetOwnerType(owner), level, message);
        }

        public static void Log(Level level, string message, Exception exception, [CallerFilePathAttribute] string owner = "")
        {
            _writer.Write(GetOwnerType(owner), level, message, exception);
        }

        private static string GetOwnerType(string owner)
        {
            var parts = owner.Split('\\');
            var type = parts.LastOrDefault();
            return type ?? "EMPTY";
        }

        private static int FileNameIndex { get { return 1; } }
    }
}
