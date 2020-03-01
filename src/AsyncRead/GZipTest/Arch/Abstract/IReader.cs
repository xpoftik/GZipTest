using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace GZipTest.Arch.Abstract
{
    public interface IReader<T>
    {
        WaitHandle ReadAsync(Action<T> callback, CancellationToken cancellationToken);
    }
}
