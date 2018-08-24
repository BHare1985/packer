using System.IO;
using System.IO.Compression;

namespace packer
{
    internal class Compressor
    {
        public byte[] Zip(byte[] array, long index)
        {
            using (var compressedStream = new MemoryStream())
            {
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                {
                    zipStream.Write(array, 0, array.Length);
                    zipStream.Close();
                    return compressedStream.ToArray();
                }
            }
        }
    }
}
