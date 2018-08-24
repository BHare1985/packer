using NUnit.Framework;
using System.IO;

namespace packer.tests
{
    [TestFixture]
    public class EndToEndTests
    {
        private string _source = Path.Combine(TestContext.CurrentContext.TestDirectory, "source.txt");
        private string _compressed = Path.Combine(TestContext.CurrentContext.TestDirectory, "compressed.txt");
        private string _decompressed = Path.Combine(TestContext.CurrentContext.TestDirectory, "decompressed.txt");


        [SetUp]
        public void Setup()
        {
            
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_source))
                File.Delete(_source);
            if (File.Exists(_compressed))
                File.Delete(_compressed);
            if (File.Exists(_decompressed))
                File.Delete(_decompressed);
        }

        [Test]
        public void Endtoend()
        {
            File.WriteAllText(_source, "Lorem!");
            var compres = new CompressStrategy(new CompressSettings() { ChunkSize = 1024, PoolSize = 1 });
            compres.Work(_source, _compressed);
            var decompress = new DecompressStrategy(1);
            decompress.Work(_compressed, _decompressed);
            CollectionAssert.AreEqual(File.ReadAllBytes(_source), File.ReadAllBytes(_decompressed));
        }
    }
}
