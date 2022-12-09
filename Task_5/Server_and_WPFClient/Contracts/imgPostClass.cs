using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public class imgPostClass
    {
        public byte[] byteSource { get; set; }
        public string path { get; set; }

        public imgPostClass() { }

        public imgPostClass(byte[] byteSource, string path)
        {
            this.byteSource = byteSource;
            this.path = path;
        }
    }
}
