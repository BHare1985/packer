using System;
using System.IO.MemoryMappedFiles;

namespace packer
{
    internal interface IFileManager : IDisposable
    {
        long CurrentOffset { get; }

        long GetOffset(byte[] array, long index);

        MemoryMappedFile BeginWrite();

        void EndWrite();
    }
}