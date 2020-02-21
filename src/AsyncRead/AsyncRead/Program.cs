using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections;
using System.IO.Compression;

namespace AsyncRead
{
    class Block {
        public Block(int index, int capacity, byte[] payload, int size)
        {
            Index = index;
            Capacity = capacity;
            Payload = payload;
            Size = size;
            Offset = -1;
        }

        public int Index { get; }
        public int Capacity { get; }
        public int Size { get; set; }
        public byte[] Payload { get; }
        public int Offset { get; set; }
    }

    class Program
    {
        static bool stopping = false;
        //static object counterLock = new object();
        static long filesize = 0;
        static int counter = -1;

        static int blockSize = 1024 * 1024;
        static int blockCount;
        static int readBufferSize = blockSize * 20;
        //TODO: Hashtable
        static ConcurrentQueue<Block> readBuffer = new ConcurrentQueue<Block>();
        static ConcurrentDictionary<long, Block> compressedBuffer = new ConcurrentDictionary<long, Block>();
        static ConcurrentQueue<Block> readyToWrite = new ConcurrentQueue<Block>();

        static void Main(string[] args)
        {
            var testFilename = "c:\\test\\bigfile.txt";
            filesize = new FileInfo(testFilename).Length;
            blockCount = (int)(filesize / blockSize) + 1;
            //blocks = new Block[blockCount];
            Console.WriteLine($"Filesize: {filesize} bytes");
            Console.WriteLine($"Blocks count: {blockCount}");

            //TODO: Use autoreset event instead of Thread.Sleep.

            var reader1 = new Thread(Reader);
            var reader2 = new Thread(Reader);
            var reader3 = new Thread(Reader);
            var reader4 = new Thread(Reader);

            var writer1 = new Thread(Writer);
            var writer2 = new Thread(Writer);
            var writer3 = new Thread(Writer);
            var writer4 = new Thread(Writer);

            var compressor1 = new Thread(Compressor);
            var compressor2 = new Thread(Compressor);
            var compressor3 = new Thread(Compressor);
            var compressor4 = new Thread(Compressor);
            var compressor5 = new Thread(Compressor);
            var compressor6 = new Thread(Compressor);
            var compressor7 = new Thread(Compressor);
            var compressor8 = new Thread(Compressor);

            var threadCalculator = new Thread(OffsetCalculator);

            threadCalculator.Start();
            reader1.Start(testFilename);
            reader2.Start(testFilename);
            reader3.Start(testFilename);
            reader4.Start(testFilename);

            compressor1.Start();
            compressor2.Start();
            compressor3.Start();
            compressor4.Start();
            compressor5.Start();
            compressor6.Start();
            compressor7.Start();
            compressor8.Start();

            var outputFilename = Path.Combine("C:\\test\\", Guid.NewGuid().ToString() + ".gz");
            File.Create(outputFilename).Close();
            writer1.Start(outputFilename);
            writer2.Start(outputFilename);
            writer3.Start(outputFilename);
            writer4.Start(outputFilename);


            reader1.Join();
            reader2.Join();
            reader3.Join();
            reader4.Join();
            //
            threadCalculator.Join();
            stopping = true;
            compressor1.Join();
            compressor2.Join();
            compressor3.Join();
            compressor4.Join();
            compressor5.Join();
            compressor6.Join();
            compressor7.Join();
            compressor8.Join();

            writer1.Join();
            writer2.Join();
            writer3.Join();
            writer4.Join();
            
            //var readBytes = readBuffer.Select(b => b.Value.Size).Sum();
            //Console.WriteLine(readBytes);

            //var emptyOffsetsCount = blocks.Count(b => b.Value.Offset == -1);
            //Console.WriteLine($"Empty offsets: {emptyOffsetsCount}");
        }

        static void PrintBlock(byte[] data, int size, Encoding encoding) {
            byte[] buffer;
            buffer = data.Take(size).ToArray();

            var s = encoding.GetString(buffer);
            Console.WriteLine(s);
        }

        static void Compressor()
        {
            while (!stopping || readBuffer.Count > 0) {
                if (readBuffer.TryDequeue(out Block block)) {
                    var index = block.Index;
                    int bytes;
                    byte[] compressed;
                    (bytes, compressed) = CompressBlock(block.Payload, CompressionLevel.Optimal);
                    var compressedBlock = new Block(
                        index,
                        capacity: bytes, 
                        payload: compressed, 
                        size: bytes);
                    compressedBuffer[index] = compressedBlock;
                } else {
                    Console.WriteLine($"Compressor went to bed. ThreadId: {Thread.CurrentThread.ManagedThreadId}");
                    Thread.Sleep(10);
                }
            }
        }

        static (int, byte[]) CompressBlock(byte[] data, CompressionLevel compression)
        {
            byte[] buffer = new byte[1024]; ;
            int readBytes;
            int bytes = 0;
            byte[] output;
            using (var source = new MemoryStream(data)) {
                using (var compressed = new MemoryStream()) {
                    //compression
                    using (var compressor = new GZipStream(compressed, compression, leaveOpen: true)) {
                        while ((readBytes = source.Read(buffer, offset: 0, buffer.Length)) > 0) {
                            compressor.Write(buffer, 0, readBytes);
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

        //static void 

        static void OffsetCalculator()
        {
            var currentIndex = 0;
            var currentOffset = 0;
            while (currentIndex < blockCount) {
                if (compressedBuffer.TryRemove(currentIndex, out Block block)) {
                    block.Offset = currentOffset;
                    currentOffset += block.Size;

                    readyToWrite.Enqueue(block);

                    currentIndex++;
                } else {
                    //Console.WriteLine(currentIndex);
                    //Console.WriteLine($"Calculator went bed. ThradId: {Thread.CurrentThread.ManagedThreadId}");
                    Thread.Sleep(100);
                }
            }
        }


        static void Reader(Object p)
        {
            var filename = (string)p;
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                while (true) {
                    if (readBuffer.Sum(b => b.Size) >= readBufferSize) {
                        //Console.WriteLine($"Reader thread went to bed. ThreadId: {Thread.CurrentThread.ManagedThreadId}");
                        Thread.Sleep(10);
                        continue;
                    }

                    var counterValue = Interlocked.Increment(ref counter);
                    var offset = counterValue * blockSize;

                    if (offset > filesize) break;

                    int readBytes;
                    byte[] buffer;
                    (readBytes, buffer) = ReadBlock(stream, offset, blockSize);
                    //readBuffer[counterValue] = new Block(counterValue, blockSize, buffer, readBytes);
                    var block = new Block(
                        index: counterValue,
                        capacity: blockSize,
                        payload: buffer,
                        size: readBytes) ;
                    readBuffer.Enqueue(block);
                }
            }
        }

        static void Writer(Object p) {
            var filename = (string)p;
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Write, FileShare.Write)) {
                while (!stopping || readyToWrite.Count > 0) {
                    if (readyToWrite.TryDequeue(out Block block)) {
                        WriteBlock(stream, block.Payload, block.Offset, block.Size);
                    } else {
                        //Console.WriteLine($"Writer got to bed. {Thread.CurrentThread.ManagedThreadId}");
                        Thread.Sleep(50);
                    }
                }
                Console.WriteLine($"Writer done! ThreadId: {Thread.CurrentThread.ManagedThreadId}");
            }
        }

        static void WriteBlock(Stream stream, byte[] data, int offset, int bytesCount) {
            stream.Position = offset;
            stream.Write(data, 0, bytesCount);
        }

        static (int, byte[]) ReadBlock(Stream stream, long offset, int bufferSize) {
            var buffer = new byte[bufferSize];
            stream.Position = offset;
            var readBytes = stream.Read(buffer, 0, bufferSize);

            return (readBytes, buffer);
        }






        static void GenerateFile(string filename, int stringsCount = 1024 * 1024) {
            using (var stream = File.OpenWrite(filename)) {
                using (var writer = new StreamWriter(stream, encoding: Encoding.UTF8)) {
                    foreach (var s in GetValues(stringsCount)) {
                        writer
                            .Write(s);
                    }
                }
            }
        }

        static IEnumerable<string> GetValues(int limitValuesCount = 1024)
        {
            var r = new Random();
            var itemsProduced = 0;
            while (!stopping) {
                if (itemsProduced++ >= limitValuesCount) {
                    stopping = true;
                    break;
                }
                var randomValue = r.Next() * 11111111111111;
                yield return $"{randomValue}";
            }
        }
    }
}
