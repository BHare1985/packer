using SimpleLogger;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace packer
{
    internal class CompressFileManager : IFileManager
    {
        private string _file;
        private readonly int _threads;
        private long _fileSize;
        private long _actualSize = 1;
        protected long _writeOffset;
        private SemaphoreSlim _semaphore;
        private MemoryMappedFile _mmf;
        private object _sync;
        private readonly long _average;

        public CompressFileManager(string file, int threads, long chunkSize)
        {
            _file = file;
            _threads = threads;
            _writeOffset = 0;
            _semaphore = new SemaphoreSlim(threads, threads);
            _mmf = MemoryMappedFile.CreateFromFile(file, FileMode.OpenOrCreate, "map", _actualSize);
            _sync = new object();
            _average = chunkSize;
        }

        public MemoryMappedFile BeginWrite()
        {
            _semaphore.Wait();
            return MemoryMappedFile.OpenExisting("map");
        }

        public long CurrentOffset => _writeOffset;

        public long GetOffset(byte[] array, long index)
        {
            Interlocked.Add(ref _actualSize, array.Length);
            Logger.Log(Level.Verbose, $"calculating offset for {index} chunk");
            var sum = Interlocked.Add(ref _writeOffset, array.Length);
            var offset = sum - array.Length;
            Logger.Log(Level.Verbose, $"offset for {index} chunk with {array.Length} bytes is {offset}");
            Resize();
            return offset;
        }

        public void EndWrite()
        {
            _semaphore.Release();
        }

        private long Threashold()
        {
            return _writeOffset + _average * (_threads * 2);
        }

        private bool IsThreshold()
        {
            return _fileSize < Threashold();
        }

        private void Resize()
        {
            if (!IsThreshold() || Monitor.IsEntered(_sync)) return;
            Logger.Log(Level.Verbose, $"file size {_fileSize} exceed minimum limit {Threashold()} trying to accrue more disk space");
            lock (_sync)
            {
                if (!IsThreshold())
                {
                    Logger.Log(Level.Verbose, $"looks like file size was already increased by other thread then skipping");
                    return;
                }
                try
                {
                    Logger.Log(Level.Verbose, $"occurring write lock");
                    for (int i = 0; i < _threads; i++)
                    {
                        _semaphore.Wait();
                        Logger.Log(Level.Verbose, $"occurred {i + 1} threads of total {_threads + 1}");
                    }
                    Logger.Log(Level.Verbose, $"write lock occurred increasing file");
                    _fileSize += _average * (_threads * 2);
                    _mmf.Dispose();
                    _mmf = MemoryMappedFile.CreateFromFile(_file, FileMode.OpenOrCreate, "map", _fileSize, MemoryMappedFileAccess.ReadWrite);
                    Logger.Log(Level.Verbose, $"file size increased to {_fileSize}");
                }
                finally
                {
                    _semaphore.Release(_threads);
                    Logger.Log(Level.Debug, $"write lock released for {_threads} threads");
                }
            }
        }

        public void Dispose()
        {
            Logger.Log(Level.Debug, $"disposing file manager");
            _mmf.Dispose();
            _semaphore.Dispose();
            SetFileSize(_file, _actualSize);
            Logger.Log(Level.Debug, $"file manager disposed");
        }

        private static void SetFileSize(string file, long size)
        {
            using (var fs = new FileStream(file, FileMode.OpenOrCreate))
                fs.SetLength(size);
            
        }
    }
}
