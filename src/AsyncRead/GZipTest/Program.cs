using GZipTest.Arch;
using GZipTest.Arch.Abstract;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GZipTest
{
    internal class Program
    {
        public static int Main(string[] args)
            //=> CommandLineApplication.Execute<Program>(new string[] { "-m compress", "-t C:\\test\\bigfile.txt", $"-o c:\\test\\{Guid.NewGuid().ToString()}.gz" });
            => CommandLineApplication.Execute<Program>(args);

        [Required]
        [Option(Description = "Compress/Decompress")]
        [AllowedValues("compress", "decompress", IgnoreCase = true)]
        public string Mode { get; }

        [Required]
        [Option(Description = "Target filename")]
        public string TargetFilename { get; }

        [Required]
        [Option(Description = "Output filename")]
        public string OutputFilename { get; }

        // magic method
        private void OnExecute()
        {
            if (!File.Exists(TargetFilename)) {
                Console.WriteLine($"Target file not found. Filename: {new FileInfo(TargetFilename).FullName}");
                return;
            }

            var cls = new CancellationTokenSource();
            var processing = new Thread(()=>{
                try {
                    IArchProcess process = null;
                    if (Mode.ToLower() == "compress") {
                        process = Compress(TargetFilename, OutputFilename, cls.Token);
                    } else {
                        process = Decompress(TargetFilename, OutputFilename, cls.Token);
                    }

                    var result = process.Result;
                    Console.WriteLine(result.Status);
                    if(result.Status == 1) { // error
                        Console.WriteLine(result.Message);
                    }
                } catch(Exception ex) {
                    while (ex != null) {
                        Console.WriteLine(ex);
                        ex = ex.InnerException;
                    }
                }
            });
            processing.Start();
            processing.Join();
        }

        private IArchProcess Compress(string target, string output, CancellationToken cancellationToken) {
            var arch = new Archiver();

            var compressProcess = arch.CompressAsync(target, output, cancellationToken);
            return compressProcess;
        }

        private IArchProcess Decompress(string target, string output, CancellationToken cancellationToken) {
            var arch = new Archiver();

            var decompressProcess = arch.DepompressAsync(target, output, cancellationToken);
            return decompressProcess;
        }
    }
}
