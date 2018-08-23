using System.Collections.Generic;
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

        public IEnumerable<Chunk> Read()
        {
            var info = new FileInfo(_file);

            using (var fs = new FileStream(_file, FileMode.Open))
            {
                using (var reader = new BinaryReader(fs))
                {
                    fs.Seek(info.Length - Marshal.SizeOf<long>(), SeekOrigin.Begin);
                    var count = reader.ReadInt32();
                    var metaOffset = count * (Marshal.SizeOf<long>() + Marshal.SizeOf<long>() + Marshal.SizeOf<long>()) + Marshal.SizeOf<long>();
                    fs.Seek(info.Length - metaOffset, SeekOrigin.Begin);
                    var chunks = new List<Chunk>(count);
                    for (var i = 0; i < count; i++)
                        chunks.Add(new Chunk(reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt64()));
                    return chunks;
                }
            }
        }
    }
}
