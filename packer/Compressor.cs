﻿using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace packer
{
    internal class Compressor
    {
        public byte[] Zip(byte[] array, long index)
        {
            Console.WriteLine("{1} Zipping from: {0}", Thread.CurrentThread.ManagedThreadId, index);
            var output = new MemoryStream();
            using (var zip = new GZipStream(output, CompressionMode.Compress))
            {
                using (var from = new MemoryStream(array))
                {
                    from.CopyTo(zip);
                    return output.ToArray();
                }
            }
        }
    }
}
