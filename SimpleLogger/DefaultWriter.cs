namespace SimpleLogger
{
    public sealed class DefaultWriter : Writer
    {
        public DefaultWriter() : base(Level.None)
        {
        }

        protected override void WriteLine(string message)
        {
            
        }
    }
}
