namespace console
{
    internal struct ZipedData
    {
        private byte[] array;
        private int index;

        public ZipedData(byte[] array, int index)
        {
            this.array = array;
            this.index = index;
        }

        public byte[] Payload { get { return array; } }
        public int Index { get { return index; } }
        public int Size { get { return array.Length; } }
    }
}