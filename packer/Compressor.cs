using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace packer
{
    public class Compressor
    {
        public byte[] Zip(byte[] array, int index)
        {
            Console.WriteLine("{1} Zipping from: {0}", Thread.CurrentThread.ManagedThreadId, index);
            var output = new MemoryStream();
            var zip = new GZipStream(output, CompressionMode.Compress);
            new MemoryStream(array).CopyTo(zip);
            return output.ToArray();
        }
    }
}
