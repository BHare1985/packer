using System.Collections.Generic;
using System.IO;

namespace packer
{
    public class MetadataWriter
    {
        private string _file;

        public MetadataWriter(string file)
        {
            _file = file;
        }

        public void Write(IEnumerable<Chunk> chunks)
        {
            using (var fs = new FileStream(_file, FileMode.Append))
            {
                using (var writer = new BinaryWriter(fs))
                {
                    long count = 0;
                    foreach (var chunk in chunks)
                    {
                        writer.Write(chunk.Size);
                        writer.Write(chunk.Offset);
                        writer.Write(chunk.Index);
                        count++;
                    }
                    writer.Write(count);
                }
            }
        }
    }
}
