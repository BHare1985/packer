using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace packer
{
    public class FileResize
    {
        private readonly FileManager _manager;

        public FileResize(FileManager manager)
        {
            _manager = manager;
            _manager.OnFileSizeChanged += Manager_OnFileSizeChanged;
        }

        private void Manager_OnFileSizeChanged(object sender, FileSizeChangedArgs args)
        {
            
            if (fileSize > _manager.CurrentOffset + chunkSize * poolSize) return;

            if (Monitor.IsEntered(_fileExceedMaximumSize)) return;
            try
            {
                lock (_fileExceedMaximumSize)
                {
                    Console.WriteLine("Threads blocked for a write. Waiting pending write threads to exit...");
                    Enumerable.Range(0, poolSize).Select(_ => { _resize.Wait(); return _; }).ToArray();
                    Console.WriteLine("Writing threads finished working. Resizing file...");
                    fileSize += chunkSize * poolSize;
                    _mmf.Dispose();
                    _mmf = MemoryMappedFile.CreateFromFile(destination, FileMode.OpenOrCreate, "map", fileSize, MemoryMappedFileAccess.ReadWrite);
                    Console.WriteLine("File resized.");
                }
            }
            finally
            {
                _resize.Release(poolSize);
            }
        }
    }

    public class FileSizeChangedArgs : EventArgs { }

    public delegate void FileSizeChanged(object sender, FileSizeChangedArgs args);

    public class FileManager : IDisposable
    {
        private string _file;
        private long _fileSize;
        protected long _writeOffset;
        private SemaphoreSlim _resize;
        private MemoryMappedFile _mmf;

        public FileManager(string file, int threads)
        {
            _file = file;
            _writeOffset = 0;
            _resize = new SemaphoreSlim(threads, threads);
            _mmf = MemoryMappedFile.CreateFromFile(file, FileMode.OpenOrCreate, "map");
        }

        public event FileSizeChanged OnFileSizeChanged;

        public long CurrentOffset => _writeOffset;

        public long GetOffset(byte[] array)
        {
            var sum = Interlocked.Add(ref _writeOffset, array.Length);
            var offset = sum - array.Length;
            this.OnFileSizeChanged(this, new FileSizeChangedArgs { });
            return offset;
        }

        public FileWriter GetWriter()
        {
            return new FileWriter(_resize);
        }

        public void Dispose()
        {
            _mmf.Dispose();
            _resize.Dispose();

        }

        public sealed class FileWriter : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public FileWriter(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
                _semaphore.Wait();
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
