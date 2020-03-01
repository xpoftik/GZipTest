using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch.Consumers
{
    internal sealed class FileWriter : IWriteConsumer<Block>
    {
        public WaitHandle AsyncWaitHandle => throw new NotImplementedException();

        public void Write(Block data)
        {
            throw new NotImplementedException();
        }
    }
}
