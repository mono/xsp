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
		Console.WriteLine ("Usage: aspparser filename [filename ...]\n");
	}
	
	public static void Main (string [] args){
		if (args.Length == 0){
			help ();
			return;
		}

		Stream input;
		AspParser ap;
		ArrayList al;
		for (int i = 0; i < args.Length; i++){
			input = File.OpenRead (args [i]);
			ap = new AspParser (args [i], input);
			ap.parse ();
			al = ap.Elements;
			Generator gen = new Generator (args [i], al);
			gen.ProcessElements ();
			if (args.Length >= i + 1 || args [i + 1] != "no")
				gen.Print ();
			else
				i++;
		}
	}
}
}

