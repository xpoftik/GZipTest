using System;
using System.IO;
using System.Threading;

namespace GZipTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var archiver = CompositionRoot.Current.GetArchiver();

            var target = "C:\\test\\test.txt";
            var archFilename = Path.Combine("C:\\test\\", $"{Guid.NewGuid().ToString()}.gz");
            try {
                var process = archiver.Arch(target, archFilename, jobs: Environment.ProcessorCount);
                if (process.Result.IsSuccess) {
                    Console.WriteLine(0);
                } else {
                    Console.WriteLine(1);
                    if (process.Result.IsInterrupted) {
                        Console.WriteLine("Interrupted by user.");
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
