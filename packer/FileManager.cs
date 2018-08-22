using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace packer
{
    public class FileManager : IDisposable
    {
        private string _file;
        private readonly int _threads;
        private long _fileSize;
        protected long _writeOffset;
        private SemaphoreSlim _semaphore;
        private MemoryMappedFile _mmf;
        private object _sync;
        private long _median;

        public FileManager(string file, int threads)
        {
            _file = file;
            _threads = threads;
            _writeOffset = 0;
            _semaphore = new SemaphoreSlim(threads, threads);
            _mmf = MemoryMappedFile.CreateFromFile(file, FileMode.OpenOrCreate, "map", 1);
            _sync = new object();
        }

        internal MemoryMappedFile BeginWrite()
        {
            _semaphore.Wait();
            return MemoryMappedFile.OpenExisting("map");
        }

        public long CurrentOffset => _writeOffset;

        public long GetOffset(byte[] array)
        {
            var sum = Interlocked.Add(ref _writeOffset, array.Length);
            _median = (_median + array.Length) / 2;
            var offset = sum - array.Length;
            Resize();
            return offset;
        }

        internal void EndWrite()
        {
            _semaphore.Release();
        }

        private bool IsThreshold()
        {
            return _fileSize < _writeOffset + _median * _threads;
        }

        private void Resize()
        {
            if (!IsThreshold() || Monitor.IsEntered(_sync)) return;

            lock (_sync)
            {
                if (!IsThreshold()) return;
                try
                {
                    for (int i = 0; i < _threads; i++)
                        _semaphore.Wait();
                    _fileSize += _median * _threads;
                    _mmf.Dispose();
                    _mmf = MemoryMappedFile.CreateFromFile(_file, FileMode.OpenOrCreate, "map", _fileSize, MemoryMappedFileAccess.ReadWrite);
                }
                finally
                {
                    _semaphore.Release(_threads);
                }
            }
        }

        public void Dispose()
        {
            _mmf.Dispose();
            _semaphore.Dispose();
            SetFileSize(_file, _writeOffset);
        }

        private static void SetFileSize(string file, long size)
        {
            using (var fs = new FileStream(file, FileMode.OpenOrCreate))
                fs.SetLength(size);
        }
    }
}
