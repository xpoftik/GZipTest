using GZipTest.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipTest.Arch
{
    internal enum ArchProcessStatus { 
        New = 0,
        InProcess = 1,
        Success = 2,
        Fault = 4,
        Interrupted = 8
    }

    internal class ArchiverWorker
    {
        private object lockObject = new object();

        private readonly string _target;
        private readonly long _filesizeInBytes;
        private readonly string _archFilename;
        private readonly int _blockSizeInBytes;
        private readonly int _readBufferSizeInBytes;
        private readonly int _compressorsCount;
        private readonly int _readersCount;
        private readonly int _writersCount;

        private readonly ConcurrentQueue<Block> ReadBuffer = new ConcurrentQueue<Block>();
        private readonly ConcurrentDictionary<long, Block> CompressedBuffer = new ConcurrentDictionary<long, Block>();
        private readonly ConcurrentQueue<Block> ReadyToWrite = new ConcurrentQueue<Block>();

        private bool _runToComplete = false;
        private int _counter = -1;
        private int _blocksCount;

        private Thread[] _readers;
        private Thread[] _compressors;
        private Thread[] _writers;
        private Thread _offsetCalculator;
        private Thread _processThread;

        private bool _interrupt = false;
        private List<Exception> _exceptions = new List<Exception>();

        public ArchiverWorker(string target, string archFilename, int blockSizeInBytes, int readBufferSizeInBytes, int compressors)
        {
            if (String.IsNullOrWhiteSpace(target)) {
                throw new ArgumentNullException(nameof(target));
            }
            if (String.IsNullOrWhiteSpace(archFilename)) {
                throw new ArgumentNullException(nameof(archFilename));
            }

            if (!File.Exists(target)) {
                throw new FileNotFoundException("File not found.", target);
            }
            if (blockSizeInBytes <= 0) {
                throw new ArgumentException($"{nameof(blockSizeInBytes)} must be positive and greater than zero.");
            }
            if (readBufferSizeInBytes <= 0) {
                throw new ArgumentException($"{nameof(readBufferSizeInBytes)} must be positive and greater than zero.");
            }

            _target = target;
            _filesizeInBytes = (new FileInfo(_target)).Length;
            _archFilename = archFilename;
            _blockSizeInBytes = blockSizeInBytes;
            _readBufferSizeInBytes = readBufferSizeInBytes;
            _compressorsCount = compressors;
            _readersCount = compressors / 2 ;
            if (_readersCount == 0) {
                _readersCount = 1;
            }
            _writersCount = compressors / 2;
            if (_writersCount == 0) {
                _writersCount = 1;
            }
        }

        public ArchProcessStatus Status { get; private set; }

        public void Start() {
            Status = ArchProcessStatus.InProcess;

            _blocksCount = (int)(new FileInfo(_target).Length / _blockSizeInBytes) + 1;

            //Readers
            _readers = new Thread[_readersCount];
            for (int idx = 0; idx < _readersCount; idx++) {
                var reader = new Thread(Reader);
                _readers[idx] = reader;

                reader.Start(_target);
            }
            
            //Compressors
            _compressors = new Thread[_compressorsCount];
            for (int idx = 0; idx < _compressorsCount; idx++) {
                var compressor = new Thread(Compressor);
                _compressors[idx] = compressor;

                compressor.Start();
            }
            
            //Offset calculator
            _offsetCalculator = new Thread(OffsetCalculator);
            _offsetCalculator.Start();

            //Writers
            File.Create(_archFilename).Close();
            _writers = new Thread[_writersCount];
            for (int idx = 0; idx < _writersCount; idx++) {
                var writer = new Thread(Writer);
                _writers[idx] = writer;

                writer.Start(_archFilename);
            }

            //Start the process
            _processThread = new Thread(Process);
            _processThread.Start();
        }

        public ArchResult Interrupt() {
            _interrupt = true;
            Status = ArchProcessStatus.Interrupted;

            return GetResult();
        }

        public ArchResult Result { get => GetResult(); } 

        private ArchResult GetResult() {
            switch (Status) {
                case ArchProcessStatus.New:
                    throw new ArchProcessFailedException("Operation wasn't started.");
                case ArchProcessStatus.Fault:
                    var aggregateException = new AggregateException(_exceptions);
                    throw new ArchProcessFailedException("Got an exception during the process.", aggregateException);
                case ArchProcessStatus.Interrupted:
                    return ArchResult.Interrupted(_target);
                case ArchProcessStatus.InProcess:
                    return RunToComplete();
                case ArchProcessStatus.Success:
                    return ArchResult.Success(_target, _archFilename);
                default:
                    throw new Exception("Unknown arch process status.");
            }
        }

        private ArchResult RunToComplete() {
            Debug.Assert(_processThread != null);

            _processThread.Join();

            return GetResult();
        }

        void Process() {
            //Sync raders;
            for (int idx = 0; idx < _readers.Length; idx++) {
                _readers[idx].Join();
            }

            //Sync offset calculator
            _offsetCalculator.Join();
            _runToComplete = true;

            //Sync compressors
            for (int idx = 0; idx < _compressors.Length; idx++) {
                _compressors[idx].Join();
            }

            //Sync writers
            for (int idx = 0; idx < _writers.Length; idx++) {
                _writers[idx].Join();
            }

            if (Status == ArchProcessStatus.InProcess) {
                Status = ArchProcessStatus.Success;
            }
            if (Status == ArchProcessStatus.Interrupted || Status == ArchProcessStatus.Fault) {
                //Try to remove file without any exceptions.
                try {
                    File.Delete(_archFilename);
                } catch { }
            }
        }

        void Reader(Object p)
        {
            var filename = (string)p;
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                while (true) {
                    if (_interrupt) break;

                    if (ReadBuffer.Sum(b => b.Size) >= _readBufferSizeInBytes) {
                        //Console.WriteLine($"Reader thread went to bed. ThreadId: {Thread.CurrentThread.ManagedThreadId}");
                        Thread.Sleep(10);
                        continue;
                    }

                    var counterValue = Interlocked.Increment(ref _counter);
                    var offset = counterValue * _blockSizeInBytes;

                    if (offset > _filesizeInBytes) break;

                    int readBytes;
                    byte[] buffer;
                    (readBytes, buffer) = ReadBlock(stream, offset, _blockSizeInBytes);
                    var block = new Block(
                        index: counterValue,
                        capacity: _blockSizeInBytes,
                        payload: buffer,
                        size: readBytes) ;
                    ReadBuffer.Enqueue(block);
                }
            }
        }

        private (int, byte[]) ReadBlock(Stream stream, long offset, int bufferSize)
        {
            var buffer = new byte[bufferSize];
            stream.Position = offset;
            var readBytes = stream.Read(buffer, 0, bufferSize);

            return (readBytes, buffer);
        }

        private void Compressor()
        {
            while (!_runToComplete || ReadBuffer.Count > 0) {
                if (_interrupt) break;

                if (ReadBuffer.TryDequeue(out Block block)) {
                    var index = block.Index;
                    int bytes;
                    byte[] compressed;
                    (bytes, compressed) = CompressBlock(block.Payload, block.Size, CompressionLevel.Optimal);
                    var compressedBlock = new Block(
                        index,
                        capacity: bytes,
                        payload: compressed,
                        size: bytes);
                    CompressedBuffer[index] = compressedBlock;
                } else {
                    //Console.WriteLine($"Compressor went to bed. ThreadId: {Thread.CurrentThread.ManagedThreadId}");
                    Thread.Sleep(10);
                }
            }
        }

        static (int, byte[]) CompressBlock(byte[] data, int blockSize, CompressionLevel compression)
        {
            byte[] buffer = new byte[1024]; ;
            int readBytes;
            int bytes = 0;
            byte[] output;
            if (data.Length != blockSize) {
                data = data.Take(blockSize).ToArray();
            }
            using (var source = new MemoryStream(data)) {
                using (var compressed = new MemoryStream()) {
                    //compression
                    using (var compressor = new GZipStream(compressed, compression, leaveOpen: true)) {
                        while ((readBytes = source.Read(buffer, offset: 0, buffer.Length)) > 0) {
                            compressor.Write(buffer, offset: 0, count: readBytes);
                        }
                    }

                    bytes = (int)compressed.Position;
                    compressed.Position = 0;
                    output = new byte[bytes];
                    bytes = 0;
                    while ((readBytes = compressed.Read(buffer, 0, buffer.Length)) > 0) {
                        if (readBytes != buffer.Length) {
                            buffer = buffer.Take(readBytes).ToArray();
                        }
                        buffer.CopyTo(output, bytes);
                        bytes += readBytes;
                    }
                }
            }
            return (bytes, output);
        }

        private void OffsetCalculator()
        {
            //We have only one offset calculator
            var currentIndex = 0;
            var currentOffset = 0;
            while (currentIndex < _blocksCount) {
                if (_interrupt) break;

                if (CompressedBuffer.TryRemove(currentIndex, out Block block)) {
                    block.Offset = currentOffset;
                    currentOffset += block.Size;

                    ReadyToWrite.Enqueue(block);

                    currentIndex++;
                } else {
                    //Console.WriteLine(currentIndex);
                    //Console.WriteLine($"Calculator went bed. ThradId: {Thread.CurrentThread.ManagedThreadId}");
                    Thread.Sleep(10);
                }
            }
        }

        private void Writer(Object p)
        {
            var filename = (string)p;
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Write, FileShare.Write)) {
                while (!_runToComplete || ReadyToWrite.Count > 0) {
                    if (_interrupt) break;

                    if (ReadyToWrite.TryDequeue(out Block block)) {
                        WriteBlock(stream, block.Payload, block.Offset, block.Size);
                    } else {
                        //Console.WriteLine($"Writer got to bed. {Thread.CurrentThread.ManagedThreadId}");
                        Thread.Sleep(10);
                    }
                }
                stream.Flush();
                //Console.WriteLine($"Writer done! ThreadId: {Thread.CurrentThread.ManagedThreadId}");
            }
        }

        private void WriteBlock(Stream stream, byte[] data, int offset, int bytesCount)
        {
            stream.Position = offset;
            stream.Write(data, 0, bytesCount);
        }

        private void SetException(Exception ex) {
            lock (lockObject) {
                _exceptions.Add(ex);
                _interrupt = true;
            }
        }
    }
}