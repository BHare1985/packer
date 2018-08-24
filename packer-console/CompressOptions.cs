using CommandLine;

namespace console
{
    [Verb("compress", HelpText = "Compressing given source into destination")]
    public class CompressOptions : Options
    {
        [Option('c', "chunksize", Required = false, Default = 1, HelpText = "Size of chunk in megabytes that will be used for compress")]
        public int Chunksize { get; set; }
    }
}
