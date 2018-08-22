using System;
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

            try
            {
                using (var map = _manager.BeginWrite())
                {
                    Console.WriteLine("{0} nothing to wait for. Continue working...", Thread.CurrentThread.ManagedThreadId);
                    using (var accestor = map.CreateViewAccessor(offset, array.Length))
                    {
                        accestor.WriteArray(0, array, 0, array.Length);
                    }
                    return new Chunk(array.Length, offset, index);
                }
            }
            finally
            {
                _manager.EndWrite();
            }
        }
    }
}
