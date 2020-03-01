using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GZipTest.Utils
{
    public class DisposableObject : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DisposableObject()
        {
            Dispose(false);
        }

        private bool _Disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed) {
                if (disposing) {
                    DisposeCore();
                }

                // Unmanaged resources are released here.

                _Disposed = true;
            }
        }

        protected virtual void DisposeCore()
        {
        }
    }
}
