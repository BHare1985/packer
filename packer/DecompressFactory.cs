using SimpleLogger;
using System;

namespace packer
{
    public class DecompressFactory
    {
        private readonly int _poolSize;

        public DecompressFactory(int poolSize)
        {
            _poolSize = poolSize;
        }

        public ThreadPool.ThreadPool GetThreadPool()
        {
            Logger.Log(Level.Debug, $"initialize thread pool with {_poolSize} threads");
            return new ThreadPool.ThreadPool(_poolSize);
        }

        internal DecompressFileManager GetFileManager(string file, long size)
        {
            Logger.Log(Level.Debug, $"initialize file manager");
            return new DecompressFileManager(file, size);
        }

        internal ByteReader GetByteReader(string file)
        {
            Logger.Log(Level.Debug, $"initialize byte reader");
            return new ByteReader(file);
        }

        internal Decompressor GetDecompressor()
        {
            Logger.Log(Level.Debug, $"initialize decompressor");
            return new Decompressor();
        }

        internal ByteWriter GetByteWriter(IFileManager manager)
        {
            Logger.Log(Level.Debug, $"initialize byte writer");
            return new ByteWriter(manager);
        }

        internal MetadataReader GetMetadataReader(string file)
        {
            Logger.Log(Level.Debug, $"initialize metadata reader");
            return new MetadataReader(file);
        }
    }
}
