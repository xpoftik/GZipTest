using GZipTest.Arch;
using System;
using System.IO;
using System.Threading;

namespace GZipTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var scheduler = new SimpleAchScheduler(threadCount: Environment.ProcessorCount);

            var filename = Path.Combine("C:\\test", "bigfile.txt");
            var reader = new FileReader(filename, scheduler);
            var bufferReader = new BufferReader(reader, Consts.DEFAULT_BUFFER_SIZE_LIMIT, scheduler);

            bool stop = false;
            while (!stop) {
                var w1 = bufferReader.ReadAsync(block => {
                    if(!stop && block.Index == -1) {
                        stop = true;
                    }
                    Console.WriteLine($"{block.Index} ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cancellationToken: CancellationToken.None);
                var w2 = bufferReader.ReadAsync(block => { Console.WriteLine($"{block.Index} ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cancellationToken: CancellationToken.None);
                var w3 = bufferReader.ReadAsync(block => { Console.WriteLine($"{block.Index} ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cancellationToken: CancellationToken.None);
                var w4 = bufferReader.ReadAsync(block => { Console.WriteLine($"{block.Index} ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cancellationToken: CancellationToken.None);
                var w5 = bufferReader.ReadAsync(block => { Console.WriteLine($"{block.Index} ThreadId: {Thread.CurrentThread.ManagedThreadId}"); }, cancellationToken: CancellationToken.None);

                WaitHandle.WaitAll(new WaitHandle[] { w1, w2, w3, w4, w5 });
            }
            Console.WriteLine("1111111111111111111111111111");
        }
    }
}
