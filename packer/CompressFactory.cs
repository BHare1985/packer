﻿namespace packer
{
    public class CompressFactory
    {
        private readonly int _poolSize;

        public CompressFactory(int poolSize)
        {
            _poolSize = poolSize;
        }

        public ThreadPool.ThreadPool GetThreadPool()
        {
            return new ThreadPool.ThreadPool(_poolSize);
        }

        internal CompressFileManager GetFileManager(string file)
        {
            return new CompressFileManager(file, _poolSize);
        }

        internal ByteReader GetByteReader(string file)
        {
            return new ByteReader(file);
        }

        internal Compressor GetCompressor()
        {
            return new Compressor();
        }

        internal ByteWriter GetByteWriter(CompressFileManager manager)
        {
            return new ByteWriter(manager);
        }

        internal MetadataWriter GetMetadataWriter(string file)
        {
            return new MetadataWriter(file);
        }
    }
}
