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
            return new ThreadPool.ThreadPool(_poolSize);
        }

        internal DecompressFileManager GetFileManager(string file, long size)
        {
            return new DecompressFileManager(file, size);
        }

        internal ByteReader GetByteReader(string file)
        {
            return new ByteReader(file);
        }

        internal Decompressor GetDecompressor()
        {
            return new Decompressor();
        }

        internal ByteWriter GetByteWriter(IFileManager manager)
        {
            return new ByteWriter(manager);
        }

        internal MetadataWriter GetMetadataWriter(string file)
        {
            return new MetadataWriter(file);
        }
    }
}
