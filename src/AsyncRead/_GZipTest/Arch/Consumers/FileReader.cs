using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch.Consumers
{
    internal sealed class FileReader : IReader<Block>, IDisposable
    {
        private readonly ManualResetEvent _asyncWaitHandle = new ManualResetEvent(initialState: false);

        private readonly Queue<Block> _readQueue = new Queue<Block>();

        public WaitHandle AsyncWaitHandle => _asyncWaitHandle;

        public FileReader(string filename, uint blockSizeInBytes, uint readBufferSizeInBytes)
        {
        }

        public IEnumerator<Block> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
