using SimpleLogger;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace packer
{
    internal class Compressor
    {
        public byte[] Zip(byte[] array, long index)
        {
            Logger.Log(Level.Verbose, $"compressing chunk number {index} of {array.Length} bytes");
            using (var compressedStream = new MemoryStream())
            {
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                {
                    zipStream.Write(array, 0, array.Length);
                    zipStream.Close();
                    var result = compressedStream.ToArray();
                    Logger.Log(Level.Verbose, $"chunk number {index} compressed from {array.Length} bytes to {result.Length} bytes");
                    Array.Clear(array, 0, array.Length);
                    return result;
                }
            }
        }
    }
}
