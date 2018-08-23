namespace packer
{
    internal class Metadata
    {
        public Chunk[] Chunks { get; set; }
        public long ChunkSize { get; set; }
        public long Length { get; set; }
    }
}
