using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

namespace packer
{
    public class ByteWriter
    {
        private readonly FileManager _manager;

        public ByteWriter(FileManager manager)
        {
            _manager = manager;
        }

        public Chunk Write(byte[] array, int index)
        {
            Console.WriteLine("{1} Writing bytes from: {0}", Thread.CurrentThread.ManagedThreadId, index);
            var offset = _manager.GetOffset(array);

            using (var writer = _manager.GetWriter())
            {
                Console.WriteLine("{0} nothing to wait for. Continue working...", Thread.CurrentThread.ManagedThreadId);

                using (var file = MemoryMappedFile.OpenExisting("map"))
                {
                    using (var accestor = file.CreateViewAccessor(offset, array.Length))
                    {
                        accestor.WriteArray(0, array, 0, array.Length);
                    }
                    return new Chunk(array.Length, offset, index);
                }
            }
        }
    }
}
