using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch.Abstract
{
    public interface IAsyncResult<T>
    {
        T Result { get; }
        WaitHandle AsyncWaitHandle { get; }
    }
}
