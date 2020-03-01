using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace GZipTest.Arch
{
    public sealed class ArchSettings
    {
        public int BlockSizeInBytes { get; }
        public int ReadBufferSizeInBytes { get; }
        public CompressionLevel CompressionLevel { get; }

        public ArchSettings(int blockSizeInBytes, int bufferSizeInBytes, CompressionLevel compressionLevel)
        {
            BlockSizeInBytes = blockSizeInBytes;
            ReadBufferSizeInBytes = bufferSizeInBytes;
            CompressionLevel = CompressionLevel;
        }
    }
}
