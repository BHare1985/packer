using System;
using System.Threading;

namespace SimpleLogger
{
    public abstract class Writer
    {
        protected readonly Level Level;

        public Writer(Level level)
        {
            Level = level;
        }

        protected virtual string Format(string type, Level level, string message)
        {
            return $"[{DateTime.UtcNow}] [{type}] [{level}] ({Thread.CurrentThread.ManagedThreadId}) {message}";
        }

        protected abstract void WriteLine(string message);

        public void Write(string type, Level level, string message)
        {
            if (!Level.HasFlag(level)) return;
            WriteLine(Format(type, level, message));
        }

        public void Write(string type, Level level, string message, Exception exception)
        {
            if (!Level.HasFlag(level)) return;
            WriteLine(Format(type, level, message));
            WriteLine(Format(type, level, exception.ToString()));
        }
    }

}
