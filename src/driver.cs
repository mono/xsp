//
// driver.cs: Program to test the C# from ASP.NET page code generator.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Licensed under the terms of the GNU GPL
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
//

namespace Mono.ASP {
	using System;
	using System.Collections;
	using System.IO;
	using System.Text;

public class Driver {

	static void help ()
	{
		Console.WriteLine ("Usage: xsp [--control] filename [filename ...]\n");
	}
	
	public static void Main (string [] args){
		if (args.Length == 0){
			help ();
			return;
		}

		Stream input;
		AspParser ap;
		ArrayList al;
		bool as_control = false;
		int i = 0;
		if (args [0] == "--control"){
			as_control = true;
			i = 1;
		}
		for (; i < args.Length; i++){
			input = File.OpenRead (args [i]);
			ap = new AspParser (args [i], input);
			ap.parse ();
			al = ap.Elements;
			Generator gen = new Generator (args [i], al, as_control);
			gen.ProcessElements ();
			gen.Print ();
		}
	}
}
}

