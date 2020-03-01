using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Arch.Consumers
{
    internal static class ReaderExtensions
    {
        public static IReader<T> To<T>(this IReader<T> reader, IWriteConsumer<T> writer) {
            throw new NotImplementedException();
        }

        public static IReader<T> Parallel<T>(this IReader<T> reader, uint jobs) {
            throw new NotImplementedException();
        }
    }
}
