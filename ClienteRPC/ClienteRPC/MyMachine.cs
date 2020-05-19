using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace ClienteRPC
{
    public static class MyMachine
    {
        public static float Memory
        {
            get => new PerformanceCounter("Memory", "Available MBytes").NextValue();
        }

        public static float CPU
        {
            get
            {

                var perf = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                perf.NextValue();
                return perf.NextValue();
            }
        }

        public static float Disk 
        {
            get => new PerformanceCounter("LogicalDisk", "% Free Space", "C:").NextValue();
        }


        public static string Json()
        {
            var expando = new ExpandoObject();
            expando.TryAdd("Memory", Memory);
            expando.TryAdd("CPU", CPU);
            expando.TryAdd("Disk", Disk);
            return JsonConvert.SerializeObject(expando);


        }
    }
}
