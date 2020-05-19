using System;
using System.Collections.Generic;
using System.Text;

namespace ServidorRPC
{
    public class Machine
    {
        public double Memory { get; set; }
        public double CPU { get; set; }
        public double Disk { get; set; }
        public string IP { get;  set; }
    }
}
