using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch.Abstract
{
    public interface IWriter<T>: IDisposable
    {
        WaitHandle WriteAsync(T data, long offset, Action<IAsyncResult<T>> callback, CancellationToken cancellationToken);
    }
}
