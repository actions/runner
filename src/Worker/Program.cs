using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace  Microsoft.VisualStudio.Services.Agent.Worker.CLI
{
    class Program
    {
        static void Main(string[] args)
        {            
            Console.WriteLine("Hello Worker!");
            
#if OS_OSX
            Console.WriteLine("Hello OSX");
#endif
        }
    }
}
