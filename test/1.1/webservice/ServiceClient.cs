using System;

namespace WebServiceTest
{
	public class ServiceClientTest
	{
		static void Main(string [] args) 
		{
			Console.WriteLine ("Testing simple web service");
			TestService s = new TestService ();
			if (args.Length > 0)
				s.Url = "http://" + args [0] + "/TestService.asmx";
			for (int n=0; n<500; n++)
			{
				string res = s.Echo ("hola");
				Console.WriteLine ("The server said: " + res);

				int r = s.Add (123,456);
				Console.WriteLine ("The server said that 123 + 456 is = " + r);
			}

			Console.WriteLine ("Testing converter service");
			ConverterService cs = new ConverterService ();
			if (args.Length > 0)
				cs.Url = "http://" + args [0] + "/ConverterService.asmx";

/*			try
			{
				Console.WriteLine ("Converting 6 EUR to USD");
				double value = cs.Convert ("EUR","USD",6);
			}
			catch (Exception ex)
			{
				Console.WriteLine ("Opps, call failed: " + ex.Message);
				Console.WriteLine ("(This was expected)");
			}
*/
			Console.WriteLine ("Logging in");
			cs.Login ("lluis");
			Console.WriteLine ("Logged");
			Console.WriteLine ();

			Console.WriteLine ("Converting 6 EUR to USD");
			Console.WriteLine ("6 EUR are: $" + cs.Convert ("EUR","USD",6));
			Console.WriteLine ();

			Console.WriteLine ("Current rates:");
			CurrencyInfo[] infos = cs.GetCurrencyInfo ();
			foreach (CurrencyInfo info in infos)
				Console.WriteLine ("  " + info.Name + " = $ " + info.Rate);
			Console.WriteLine ();

			Console.WriteLine ("Setting EUR rate to 0.9");
			cs.SetCurrencyRate ("EUR", 0.9);
			Console.WriteLine ();

			Console.WriteLine ("Checking EUR rate");
			Console.WriteLine ("EUR rate is: " + cs.GetCurrencyRate ("EUR"));
			Console.WriteLine ();

			Console.WriteLine ("Done");
		}
	}
}
