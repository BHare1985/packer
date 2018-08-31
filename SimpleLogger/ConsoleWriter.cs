using System;

namespace SimpleLogger
{
    public sealed class ConsoleWriter : Writer
    {
        public ConsoleWriter(Level level) : base(level)
        {
        }

        protected override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
