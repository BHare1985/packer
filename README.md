# Welcome to packer!
packer is just for fun project that allow to zip and unzip large files with a help of [GZipStream](https://msdn.microsoft.com/ru-ru/library/system.io.compression.gzipstream%28v=vs.110%29.aspx) (take care of .NET Core 2.0 - it has bug in this library) [Memory Mapped Files](https://en.wikipedia.org/wiki/Memory-mapped_file) and custom Thread Pool implementation.

# Project
Solution consist of three projects: **packer** **packer-console** and **ThreadPool**.
Almost all logic connected to file compressing and decompressing aggregated into **packer**. **packer-console** is UI that allow to run pack operations via console. **ThreadPool** is rounded because of course so as you can use build in .net thread pool. But it was part of plan to take a challenge and implement my own (do not do this in real world :) ).
## Tools
To run this solution you have to install .NET Core 2.1 framework and any IDE (Visual Studio 2017 / Studio Online) that allow to build it or build solution via .NET Core CLI
## Run and Debug
F5 for Visual Studio on **packer-project** 
## Tests
TODO

# Architect
File compressing algorithm is out of scope here so as we use .NET GZipStream. More interesting is how to manage multi-thread reading and writing. Idea is quite simple once you have source file to compress you need to get chunk from it then you have to compress it and then write to destination file. 
Problems happens when you try to do this by several threads that are in race condition for source and destination files. To have correct result and to avoid corrupted state of compressed file *CompressFileManager* take act. This class used to track current offset for destination file and set barriers for writing threads of destination file. So as we don't know what will be destination file size we have to accrue some little disk space (number of threads * median of write chunk size) and extend this border from time to time. 
There is no order in writing threads. Any thread can take any chunk and write it to nearest position. Structure of compressed file isn't straightforward and could not be mapped one to one for source file. But this allow to avoid concurrency for writing threads and as a result no blocks happens for write operation (for real there is one tine atomic operation of calculating writing offset in *GetOffset*. This synchronization uses *Interlocked* under the hood.  Interlocked is highly performance blocking operation that implemented on a processor level).
To be able to decompress file back we need to know order of chunks we have written so we are writing chunks metadata to the end of the file. Also we write chinks count, initial file size and chunk size. With a help of metadata we recreate on order of chunks and write them back after decompression.

# Licence
MIT License

Copyright (c) 2018 packer

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
