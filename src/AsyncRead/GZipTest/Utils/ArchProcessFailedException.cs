using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Utils
{

    [Serializable]
    public class ArchProcessFailedException : Exception
    {
        public ArchProcessFailedException() { }
        public ArchProcessFailedException(string message) : base(message) { }
        public ArchProcessFailedException(string message, Exception inner) : base(message, inner) { }
        protected ArchProcessFailedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
