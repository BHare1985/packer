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
        public byte[] Read(long index, long offset, long length)
        {
            Console.WriteLine("{1} Reading bytes from: {0}", Thread.CurrentThread.ManagedThreadId, index);

            using (var file = File.OpenRead(_file))
            {
                file.Seek(offset, SeekOrigin.Begin);
                var chunk = new byte[length];
                var size = file.Read(chunk, 0, (int)length);
                if (size != length)
                    Array.Resize(ref chunk, size);
                return chunk;
            }
        }
    }
}
