using CommandLine;

namespace console
{
    public abstract class Options
    {
        [Option('s', "source", Required = true, HelpText = "Path to source file")]
        public string Source { get; set; }

        [Option('d', "destination", Required = true, HelpText = "Path to destination file")]
        public string Destination { get; set; }

        [Option('p', "poolsize", Required = false, Default = 4, HelpText = "Threads amount used for operations")]
        public int Poolsize { get; set; }
    }
}
