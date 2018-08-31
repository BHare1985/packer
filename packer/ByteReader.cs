using SimpleLogger;
using System;
using System.IO;

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
            Logger.Log(Level.Verbose, $"reading bytes for chunk number {index} from {offset} offset of {length} bytes");
            using (var file = File.OpenRead(_file))
            {
                Logger.Log(Level.Verbose, $"file {_file} opened for read");
                file.Seek(offset, SeekOrigin.Begin);
                Logger.Log(Level.Verbose, $"set read position to {offset} offset");
                var chunk = new byte[length];
                var size = file.Read(chunk, 0, (int)length);
                Logger.Log(Level.Verbose, $"have read {size} bytes");
                if (size != length)
                    Array.Resize(ref chunk, size);
                return chunk;
            }
        }
    }
}
