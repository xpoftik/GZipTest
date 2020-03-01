using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Arch.Model
{
    internal sealed class Block
    {
        public Block(int index, int capacity, byte[] payload, int size)
        {
            Index = index;
            Capacity = capacity;
            Payload = payload;
            Size = size;
        }

        public static Block NullBlock() {
            return new Block(-1, 0, new byte[0], 0);
        }

        public int Index { get; }
        public int Capacity { get; }
        public int Size { get; }
        public byte[] Payload { get; }
    }
}
