using GZipTest.Arch.Abstract;
using GZipTest.Arch.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch.Results
{
    internal sealed class OperationCancelledResult<T> : IAsyncResult<T>
    {
        private readonly WaitHandle _waitHandle;

        public OperationCancelledResult(WaitHandle waitHandle)
        {
            _waitHandle = waitHandle;
        }

        public T Result => throw new OperationCanceledException("Operation cancelled by user.");

        public WaitHandle AsyncWaitHandle => _waitHandle;
    }
}
