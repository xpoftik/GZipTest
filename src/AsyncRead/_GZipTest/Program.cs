﻿using GZipTest.Utils;
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

            var target = "C:\\test\\bigfile.txt";
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
                PrintException(ex);
#if DEBUG
                throw;
#endif
            }
        }

        static void PrintException(Exception ex) {
            Console.WriteLine(ex.Message);
            if (ex.InnerException is AggregateException) {
                var inner = (ex.InnerException as AggregateException).Flatten();
                Console.WriteLine(inner.Message);
            } else {
                var inner = ex.InnerException;
                while (inner != null) {
                    Console.WriteLine(inner.Message);
                    inner = inner.InnerException;
                }
            }
        }
    }
}