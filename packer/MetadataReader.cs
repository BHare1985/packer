using System.IO;
using System.Runtime.InteropServices;

namespace packer
{
    internal class MetadataReader
    {
        private string _file;

        public MetadataReader(string file)
        {
            _file = file;
        }

        public Metadata Read()
        {
            var info = new FileInfo(_file);

            using (var fs = new FileStream(_file, FileMode.Open))
            {
                using (var reader = new BinaryReader(fs))
                {
                    var general = Marshal.SizeOf<long>() + Marshal.SizeOf<long>() + Marshal.SizeOf<long>();
                    fs.Seek(info.Length - general, SeekOrigin.Begin);
                    var size = reader.ReadInt64();
                    var length = reader.ReadInt64();
                    var count = reader.ReadInt64();
                    
                    var metaOffset = count * (Marshal.SizeOf<long>() + Marshal.SizeOf<long>() + Marshal.SizeOf<long>()) + general;
                    fs.Seek(info.Length - metaOffset, SeekOrigin.Begin);
                    var chunks = new Chunk[count];
                    for (var i = 0; i < count; i++)
                        chunks[i] = new Chunk(reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt64());
                    return new Metadata() { Chunks = chunks, ChunkSize = size, Length = length };
                }
            }
        }
    }
}
