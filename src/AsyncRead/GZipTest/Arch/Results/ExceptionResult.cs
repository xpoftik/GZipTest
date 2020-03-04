using GZipTest.Arch.Abstract;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch.Results
{
    internal sealed class ExceptionResult<T> : IAsyncResult<T>
    {
        private Exception _ex;
        private readonly WaitHandle _waitHandle;

        public ExceptionResult(Exception ex, WaitHandle waitHandle)
        {
            ThrowIf.Argument.IsNull(ex, nameof(ex));
            ThrowIf.Argument.IsNull(waitHandle, nameof(waitHandle));

            _ex = ex;
            _waitHandle = waitHandle;
        }

        public T Result => GetResult();

        public WaitHandle AsyncWaitHandle => _waitHandle;

        private T GetResult() {
            throw new AggregateException(_ex);
        }
    }
}
