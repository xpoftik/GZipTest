using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GZipTest.Arch.Consumers
{
    internal interface IReader<T>: IEnumerable<T>, IEnumerable
    {
    }
}
