using GZipTest.Arch;
using System;
using System.Collections.Generic;
using System.Text;

namespace GZipTest
{
    internal sealed class CompositionRoot
    {
        public static CompositionRoot Current { get; }

        static CompositionRoot() {
            Current = new CompositionRoot();
        }

        private CompositionRoot() { 
            
        }

        public Archiver GetArchiver() {
            return new Archiver();
        }
    }
}
