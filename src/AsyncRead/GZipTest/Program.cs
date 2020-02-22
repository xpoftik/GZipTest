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
            var process = archiver.Arch(target, archFilename, jobs: 1);
            try {
                var result = process.Result;
                if (result.IsSuccess) {
                    Console.WriteLine(0);
                } else {
                    Console.WriteLine(1);
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
