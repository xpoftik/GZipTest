using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest.Arch.Abstract
{
    public interface IArchResult
    {
        int Status { get;  }
        string Message { get; }
    }

    internal sealed class ArchResult : IArchResult
    {
        private ArchResult(int status, string message) {
            Status = status;
            Message = message;
        }

        public int Status { get; }

        public string Message { get; }

        public static IArchResult Success() {
            return new ArchResult(0, "Success");    
        }

        public static IArchResult Cancelled() {
            return new ArchResult(1, "Operation has been cancelled.");
        }

        public static IArchResult Faulted(string message) {
            return new ArchResult(1, message);
        }
    }
}
