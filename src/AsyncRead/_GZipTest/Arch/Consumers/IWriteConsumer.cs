using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch.Consumers
{
    internal interface IWriteConsumer<in T>
    {
        WaitHandle AsyncWaitHandle { get; }

        void Write(T data);
    }
}
