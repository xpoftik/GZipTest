using GZipTest.Arch.Abstract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace GZipTest.Arch
{
    internal sealed class Archiver
    {
        public Archiver()
        {
        }

        public IArchProcess CompressAsync(string target, string output, CancellationToken cancellationToken) {
            var scheduler = new SimpleAchScheduler(threadsCount: Environment.ProcessorCount);

            var reader = new FileReader(target, scheduler);
            var bufferReader = new BufferReader(reader, Consts.DEFAULT_BUFFER_SIZE_LIMIT, scheduler);
            var compressor = new Compressor(bufferReader, System.IO.Compression.CompressionLevel.Optimal, scheduler);

            var writer = new FileWriter(output, scheduler);

            var process = new ArchProcess(compressor, writer, scheduler);
            process.Start(cancellationToken);

            return process;
        }

        public IArchProcess DepompressAsync(string target, string output, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
        
    }
}
