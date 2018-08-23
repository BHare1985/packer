using System;
using System.IO;
using System.Threading;

namespace packer
{
    internal class ByteReader
    {
        private string _file;

        public ByteReader(string file)
        {
            _file = file;
        }
        public byte[] Read(int index, long offset, int length)
        {
            Console.WriteLine("{1} Reading bytes from: {0}", Thread.CurrentThread.ManagedThreadId, index);

            using (var file = File.OpenRead(_file))
            {
                file.Seek(offset, SeekOrigin.Begin);
                var chunk = new byte[length];
                var size = file.Read(chunk, 0, length);
                return chunk;
            }
        }
    }
}
