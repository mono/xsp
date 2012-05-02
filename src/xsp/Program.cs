using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace xsp
{
    class Program
    {
        static void Main(string[] args)
        {
            //Assembly ass = Assembly.Load("Mono.WebServer.XSP");
            Mono.WebServer.XSP.Server.Main(args);
        }
    }
}
