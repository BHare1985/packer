using SimpleLogger;
using System;

namespace packer
{
    internal class ByteWriter
    {
        private readonly IFileManager _manager;

        public ByteWriter(IFileManager manager)
        {
            _manager = manager;
        }

        public Chunk Write(byte[] array, long index)
        {
            var offset = _manager.GetOffset(array, index);
            return Write(array, index, offset);
        }

        public Chunk Write(byte[] array, long index, long offset)
        {
            Logger.Log(Level.Verbose, $"writing {array.Length} bytes for chunk number {index} with {offset} bytes offset");
            try
            {
                Logger.Log(Level.Verbose, $"accruing semaphore for chunk number {index}");
                using (var map = _manager.BeginWrite())
                {
                    Logger.Log(Level.Verbose, $"semaphore accrued write started");
                    using (var accestor = map.CreateViewAccessor(offset, array.Length))
                    {
                        accestor.WriteArray(0, array, 0, array.Length);
                    }
                    Logger.Log(Level.Verbose, "write finished");
                    Array.Clear(array, 0, array.Length);
                    return new Chunk(array.Length, offset, index);
                }
            }
            finally
            {
                _manager.EndWrite();
                Logger.Log(Level.Debug, $"semaphore released for chunk number {index}");
            }
        }
    }
}
