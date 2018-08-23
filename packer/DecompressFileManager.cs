using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace packer
{
    internal class DecompressFileManager : IFileManager
    {
        private readonly string _file;
        private MemoryMappedFile _mmf;

        public DecompressFileManager(string file, long size)
        {
            _file = file;
            _mmf = MemoryMappedFile.CreateFromFile(file, FileMode.OpenOrCreate, "map", size);
        }

        public long CurrentOffset => throw new NotImplementedException();

        public MemoryMappedFile BeginWrite()
        {
            return MemoryMappedFile.OpenExisting("map");
        }

        public void Dispose()
        {
            _mmf.Dispose();
        }

        public void EndWrite()
        {

        }

        public long GetOffset(byte[] array, long index)
        {
            return array.Length * index;
        }
    }
}
