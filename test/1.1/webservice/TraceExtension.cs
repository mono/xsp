using System;
using System.Text;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.IO;
using System.Net;

  // Define a SOAP Extension that traces the SOAP request and SOAP
  // response for the XML Web service method the SOAP extension is
  // applied to.

public class TraceExtension : SoapExtension 
{
	string filename = "trace.log";

	// Save the Stream representing the SOAP request or SOAP response into
	// a local memory buffer.
	public override Stream ChainStream( Stream stream )
	{
		return stream;
	}

	// When the SOAP extension is accessed for the first time, the XML Web
	// service method it is applied to is accessed to store the file
	// name passed in, using the corresponding SoapExtensionAttribute.   
	public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute) 
	{
		return ((TraceExtensionAttribute) attribute).Filename;
	}

	// The SOAP extension was configured to run using a configuration file
	// instead of an attribute applied to a specific XML Web service
	// method.
	public override object GetInitializer(Type WebServiceType) 
	{
		// Return a file name to log the trace information to, based on the
		// type.
		return WebServiceType.GetType().ToString() + ".log";    
	}

	// Receive the file name stored by GetInitializer and store it in a
	// member variable for this specific instance.
	public override void Initialize(object initializer) 
	{
		filename = (string) initializer;
	}

	//  If the SoapMessageStage is such that the SoapRequest or
	//  SoapResponse is still in the SOAP format to be sent or received,
	//  save it out to a file.
	public override void ProcessMessage(SoapMessage message) 
	{
		switch (message.Stage) 
		{
			case SoapMessageStage.BeforeSerialize:
				WriteOutput(message);
				break;
			case SoapMessageStage.AfterSerialize:
				break;
			case SoapMessageStage.BeforeDeserialize:
				break;
			case SoapMessageStage.AfterDeserialize:
				WriteInput(message);
				break;
			default:
				throw new Exception("invalid stage");
		}
	}

	public void WriteOutput(SoapMessage message)
	{
		FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write);
		StreamWriter w = new StreamWriter(fs);

		if (message is SoapServerMessage)
		{
			w.WriteLine("METHOD RESPONSE at " + DateTime.Now);
			int opc = message.MethodInfo.OutParameters.Length;
			if (opc > 0) w.WriteLine ("  Out parameters:");
			for (int n=0; n<opc; n++)
				w.WriteLine ("     " + message.GetOutParameterValue (n));
			w.WriteLine ("  Return value: " + message.GetReturnValue ());
		}

		w.Flush();
		w.Close();
	}

	public void WriteInput(SoapMessage message)
	{
		FileStream fs = new FileStream(filename, FileMode.Append, FileAccess.Write);
		StreamWriter w = new StreamWriter(fs);

		if (message is SoapServerMessage)
		{
			w.WriteLine("METHOD CALL at " + DateTime.Now);
			int ipc = message.MethodInfo.InParameters.Length;
			if (ipc > 0) w.WriteLine ("  Parameters:");
			for (int n=0; n<ipc; n++)
				w.WriteLine ("     " + message.GetInParameterValue (n));
		}

		w.Flush();
		w.Close();
	}
}

// Create a SoapExtensionAttribute for the SOAP Extension that can be
// applied to an XML Web service method.
[AttributeUsage(AttributeTargets.Method)]
public class TraceExtensionAttribute : SoapExtensionAttribute 
{

	private string filename = "trace.log";
	private int priority;
	string tag;

	public override Type ExtensionType 
	{
		get { return typeof(TraceExtension); }
	}

	public string Tag
	{
		get { return tag; }
		set { tag = value; }
	}

	public override int Priority 
	{
		get { return priority; }
		set { priority = value; }
	}

	public string Filename 
	{
		get 
		{
			return filename;
		}
		set 
		{
			filename = value;
		}
	}
}