using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace packer
{
    internal class Decompressor
    {
        public byte[] Zip(byte[] array, long index)
        {
            Console.WriteLine("{1} Unsipping from: {0}", Thread.CurrentThread.ManagedThreadId, index);
            var output = new MemoryStream();
            using (var zip = new GZipStream(new MemoryStream(array), CompressionMode.Decompress))
            {
                zip.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}
