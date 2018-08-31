using SimpleLogger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace packer
{
    internal class MetadataWriter : IDisposable
    {
        private string _file;
        private ConcurrentBag<Chunk> _chunks;

        public MetadataWriter(string file)
        {
            _file = file;
            _chunks = new ConcurrentBag<Chunk>();
        }

        public void Add(Chunk chunk)
        {
            _chunks.Add(chunk);
        }

        public void Write(long size, long length)
        {
            Logger.Log(Level.Verbose, "writing compressing metadata");
            using (var fs = new FileStream(_file, FileMode.Append))
            {
                using (var writer = new BinaryWriter(fs))
                {
                    Logger.Log(Level.Debug, "file ready writing chunks information");
                    long count = 0;
                    foreach (var chunk in _chunks)
                    {
                        writer.Write(chunk.Size);
                        writer.Write(chunk.Offset);
                        writer.Write(chunk.Index);
                        Logger.Log(Level.Debug, $"writing chunk {chunk.Index} with {chunk.Offset} offset and {chunk.Size} length");
                        count++;
                    }
                    writer.Write(size);
                    writer.Write(length);
                    writer.Write(count);
                    Logger.Log(Level.Debug, $"writing chunk size {size} chunks count {count} source file length {length} bytes");
                }
            }
            Logger.Log(Level.Verbose, "compressing metadata has been writen");
        }

        public void Dispose()
        {
            while(_chunks.Count > 0)
                if(_chunks.TryTake(out Chunk chunk)) { }
            _chunks = null;
        }
    }
}
