using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Arch
{
    internal sealed class Block
    {
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
}
