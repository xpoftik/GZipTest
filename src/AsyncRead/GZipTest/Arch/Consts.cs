using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Arch
{
    public static class Consts
    {
        public const int DEFAULT_BLOCK_SIZE = 1024 * 1024;
        public const int DEFAULT_BUFFER_SIZE_LIMIT = DEFAULT_BLOCK_SIZE * 20;
    }
}
