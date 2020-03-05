using GZipTest.Arch;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GZipTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"ProcessorCount: {Environment.ProcessorCount}");

            //var cls = new CancellationTokenSource();
            //var scheduler = new SimpleAchScheduler(threadsCount: 8);

            //var stopwatch = new Stopwatch();
            //stopwatch.Start();
            //while (true) {
            //    var filename = Path.Combine("C:\\test", "bigfile.txt");
            //    var reader = new FileReader(filename, scheduler);
            //    var buffer = new BufferReader(reader, Consts.DEFAULT_BUFFER_SIZE_LIMIT, scheduler);
            //    var compressor = new Compressor(buffer, System.IO.Compression.CompressionLevel.Optimal, scheduler);

            //    var _reader = compressor;
            //    bool stop = false;
            //    try {
            //        while (!stop) {
            //            var w1 = _reader.ReadAsync(block => {
            //                try{
            //                    if(!stop && block.Result.Index == -1) {
            //                        stop = true;
            //                    }
            //                    Console.WriteLine($"\r{block.Result.Index} (Size: {block.Result.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}");
            //                } catch(Exception ex) {
            //                    Console.WriteLine(ex.Message);
            //                    cls.Cancel();
            //                }
            //            }, cls.Token);
            //            var w2 = _reader.ReadAsync(block => { Console.Write($"\r{block.Result.Index} (Size: {block.Result.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cls.Token);
            //            var w3 = _reader.ReadAsync(block => { Console.Write($"\r{block.Result.Index} (Size: {block.Result.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cls.Token);
            //            //var w4 = _reader.ReadAsync(block => { Console.Write($"\r{block.Result.Index} (Size: {block.Result.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cls.Token);
            //            //var w5 = _reader.ReadAsync(block => { Console.Write($"\r{block.Result.Index} (Size: {block.Result.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cls.Token);
            //            //var w6 = _reader.ReadAsync(block => { Console.Write($"\r{block.Result.Index} (Size: {block.Result.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cls.Token);
            //            //var w7 = _reader.ReadAsync(block => { Console.Write($"\r{block.Result.Index} (Size: {block.Result.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cls.Token);
            //            //var w8 = _reader.ReadAsync(block => { Console.Write($"\r{block.Result.Index} (Size: {block.Result.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cls.Token);

            //            WaitHandle.WaitAll(new WaitHandle[] { w1, w2, w3 });
            //            //Console.WriteLine("===================================");
            //        }
            //    } catch (Exception ex) {
            //        while (ex != null) {
            //            Console.WriteLine(ex);
            //            ex = ex.InnerException;
            //        }
            //        cls.Cancel();
            //    } finally {
            //        _reader.Dispose();
            //    }
            //    Console.WriteLine("\r\n\r\n");
            //    Console.WriteLine($"Done! {stopwatch.Elapsed.Minutes} min. {stopwatch.Elapsed.Seconds} sec.");
            //    stopwatch.Restart();

            //    break;
            //}



            //// DECOMPRESSION TEST
            //var cls = new CancellationTokenSource();
            //var scheduler = new SimpleAchScheduler(threadsCount: 8);

            //var stopwatch = new Stopwatch();
            //stopwatch.Start();
            //while (true) {
            //    var filename = Path.Combine("C:\\test", "test.gz");
            //    var decompressorReader = new DecompressorReader(filename, scheduler);
            //    var bufferReader = new BufferReader(decompressorReader, Consts.DEFAULT_BUFFER_SIZE_LIMIT, scheduler);

            //    var _reader = bufferReader;
            //    bool stop = false;
            //    try {
            //        while (!stop) {
            //            var w1 = _reader.ReadAsync(block => {
            //                try{
            //                    if(!stop && block.Result.Index == -1) {
            //                        stop = true;
            //                    }
            //                    Console.WriteLine($"\r{block.Result.Index} (Size: {block.Result.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}");
            //                } catch(Exception ex) {
            //                    Console.WriteLine(ex.Message);
            //                    cls.Cancel();
            //                }
            //            }, cls.Token);
            //            var w2 = _reader.ReadAsync(block => { Console.Write($"\r{block.Result.Index} (Size: {block.Result.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cls.Token);
            //            var w3 = _reader.ReadAsync(block => { Console.Write($"\r{block.Result.Index} (Size: {block.Result.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cls.Token);

            //            WaitHandle.WaitAll(new WaitHandle[] { w1, w2, w3 });
            //        }
            //    } catch (Exception ex) {
            //        while (ex != null) {
            //            Console.WriteLine(ex);
            //            ex = ex.InnerException;
            //        }
            //        cls.Cancel();
            //    } finally {
            //        _reader.Dispose();
            //    }
            //    Console.WriteLine("\r\n\r\n");
            //    Console.WriteLine($"Done! {stopwatch.Elapsed.Minutes} min. {stopwatch.Elapsed.Seconds} sec.");
            //    stopwatch.Restart();

            //    break;
            //}





            var cls = new CancellationTokenSource();
            var arch = new Archiver();

            //var target = Path.Combine("C:\\test", "bigfile.txt");
            //var output = Path.Combine("C:\\test", $"{Guid.NewGuid().ToString()}.gz");

            //var compressProcess = arch.CompressAsync(target, output, cls.Token);
            //var result = compressProcess.Result;

            //Console.WriteLine($"{result.Status} ({result.Message})");

            var target = Path.Combine("C:\\test", "bigtest.gz");
            var decompressed = Path.Combine("C:\\test", $"{Guid.NewGuid()}_decompressed.txt");

            var decompressProcess = arch.DepompressAsync(target, decompressed, cls.Token);
            var result = decompressProcess.Result;

            Console.WriteLine($"{result.Status} ({result.Message})");
            Console.WriteLine("Done!");








            //var scheduler = new SimpleAchScheduler(threadsCount: Environment.ProcessorCount);

            //var filename = Path.Combine("C:\\test", "bigfile.txt");
            //var reader = new FileReader(filename, scheduler);
            //var bufferReader = new BufferReader(reader, Consts.DEFAULT_BUFFER_SIZE_LIMIT, scheduler);
            //var compressor = new Compressor(bufferReader, System.IO.Compression.CompressionLevel.Optimal, scheduler);

            //var _reader = compressor;
            //bool stop = false;
            //try {
            //    while (!stop) {
            //        var w1 = _reader.ReadAsync(block => {
            //            if(!stop && block.Index == -1) {
            //                stop = true;
            //            }
            //            Console.WriteLine($"{block.Index} ThreadId: {Thread.CurrentThread.ManagedThreadId}");
            //        }, cancellationToken: CancellationToken.None);
            //        var w2 = _reader.ReadAsync(block => { Console.WriteLine($"{block.Index} (Size: {block.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cancellationToken: CancellationToken.None);
            //        var w3 = _reader.ReadAsync(block => { Console.WriteLine($"{block.Index} (Size: {block.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cancellationToken: CancellationToken.None);
            //        var w4 = _reader.ReadAsync(block => { Console.WriteLine($"{block.Index} (Size: {block.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cancellationToken: CancellationToken.None);
            //        var w5 = _reader.ReadAsync(block => { Console.WriteLine($"{block.Index} (Size: {block.Size}) ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cancellationToken: CancellationToken.None);

            //        WaitHandle.WaitAll(new WaitHandle[] { w1, w2, w3, w4, w5 });
            //    }
            //} finally {
            //    _reader.Dispose();
            //}
            //Console.WriteLine("1111111111111111111111111111");
        }
    }
}
