namespace packer
{
    internal class Chunk
    {
        public Chunk(long size, long offset, long index)
        {
            Size = size;
            Offset = offset;
            Index = index;
        }

        public long Size { get; }
        public long Offset { get; }
        public long Index { get; }
    }
}
