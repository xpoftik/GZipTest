using GZipTest.Arch.Consumers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace GZipTest.Arch
{
    internal sealed class Archiver
    {
        private const int DEFAULT_BLOCK_SIZE = 1024 * 1024;
        private const int DEFAULT_READ_BUFFER_SIZE = DEFAULT_BLOCK_SIZE * 20;
        
        private readonly int _blockSizeInBytes;
        private readonly int _readBufferSizeInBytes;
        private readonly Func<string, string, ArchSettings, int, ArchiverWorker> _factory;

        public Archiver(
            Func<string, string, ArchSettings, int, ArchiverWorker> factory,
            int blockSizeInBytes = DEFAULT_BLOCK_SIZE,
            int readBufferSizeInBytes = DEFAULT_READ_BUFFER_SIZE)
        {
            if (factory is null) {
                throw new ArgumentNullException(nameof(factory));
            }
            
            if (blockSizeInBytes <= 0) {
                _blockSizeInBytes = DEFAULT_BLOCK_SIZE;
            } else {
                _blockSizeInBytes = blockSizeInBytes;
            }
            if (readBufferSizeInBytes <= 0) {
                _readBufferSizeInBytes = DEFAULT_READ_BUFFER_SIZE;
            } else {
                _readBufferSizeInBytes = readBufferSizeInBytes;
            }
            _factory = factory;
        }

        public ArchiverWorker Arch(string target, string archFilename, CompressionLevel compressionLevel = CompressionLevel.Optimal, int jobs = -1) {
            if (String.IsNullOrWhiteSpace(target)) {
                throw new ArgumentNullException(nameof(target));
            }
            if (!File.Exists(target)) {
                throw new FileNotFoundException("File not found.", target);
            }
            if (String.IsNullOrWhiteSpace(archFilename)) {
                throw new ArgumentNullException(archFilename);
            }
            if (jobs <= 0) {
                jobs = Environment.ProcessorCount;
            }

            var reader = new FileReader(target, (uint)_blockSizeInBytes, (uint)_readBufferSizeInBytes);
            reader.Parallel(jobs: 8).To(new FileWriter());

            var settings = new ArchSettings(_blockSizeInBytes, _readBufferSizeInBytes, compressionLevel);
            //var process = new ArchiverWorker(target, archFilename, _blockSizeInBytes, _readBufferSizeInBytes, compressors: jobs);
            var process = _factory(target, archFilename, settings, jobs);
            process.Start();

            return process;
        }
    }
}
