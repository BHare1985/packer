using System.IO;
using System.IO.Compression;

namespace packer
{
    internal class Decompressor
    {
        public byte[] Zip(byte[] array, long index)
        {
            using (var compressedStream = new MemoryStream(array))
            {
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        zipStream.CopyTo(resultStream);
                        return resultStream.ToArray();
                    }
                }
            }
        }
    }
}
