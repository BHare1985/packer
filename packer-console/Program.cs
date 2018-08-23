using packer;

namespace console
{
    class Program
    {
        //fsutil file createnew C:\testfile.txt 1000
        static void Main(string[] args)
        {
            string source = @"C:\Users\John\source\repos\testfile.txt";
            string destination = @"C:\Users\John\source\repos\90b3a02b-43cf-4ce2-abb4-d7639eb405b4.txt";

            {
                var settings = new CompressSettings { ChunkSize = 1 * 1024 * 1024, PoolSize = 4 }; //1B * 1024 => 1KB * 1024 => 1MB 
                IStrategy strategy = new CompressStrategy(settings);
                strategy.Work(source, destination);
            }

            {
                IStrategy strategy = new DecompressStrategy(4);
                strategy.Work(destination, source);
            }
        }
    }
}
