//
// Mono.ASPNET.Tools.StateServer
//
// Author(s):
//  Jackson Harper (jackson@ximian.com)
//
// (C) 2003 Novell, Inc (http://www.novell.com)
//

using System;
using System.Runtime.Remoting;

namespace Mono.ASPNET.Tools {

        public class StateServer {

                [STAThread]
                public static void Main (string [] args)
                {
                        RemotingConfiguration.Configure ("asp_state.exe.config");
                        Console.ReadLine ();
                }

        }

}

