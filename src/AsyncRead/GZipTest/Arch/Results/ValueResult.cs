using GZipTest.Arch.Abstract;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch.Results
{
    internal sealed class ValueResult<T> : IAsyncResult<T>
    {
        private readonly T _value;
        private readonly WaitHandle _waitHandle;

        public ValueResult(T value, WaitHandle waitHandle)
        {
            ThrowIf.Argument.IsNull(waitHandle, nameof(waitHandle));

            _value = value;
            _waitHandle = waitHandle;
        }

        public T Result => _value;

        public WaitHandle AsyncWaitHandle => _waitHandle;
    }
}
