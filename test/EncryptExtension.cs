//
// EncryptExtension.cs
//
// Author:
//   Lluis Sanchez Gual (lluis@ximian.com)
//
// Copyright (C) Ximian, Inc. 2003
//

using System;
using System.Text;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.IO;
using System.Net;
using System.Security.Cryptography;

public class EncryptExtension : SoapExtension 
{
	Stream oldStream;
	MemoryStream newStream;

	byte[] key = { 0xEE, 0x9F, 0xAB, 0x79, 0x11, 0x3F, 0x53, 0x56, 0xEE, 0x9F, 0xAB, 0x79, 0x11, 0x3F, 0x53, 0x56 };
	byte[] iv = { 0xB0, 0x2A, 0x0F, 0x47, 0x4E, 0x47, 0xDB, 0x4A, 0xB0, 0x2A, 0x0F, 0x47, 0x4E, 0x47, 0xDB, 0x4A };
	byte[] filler = { 32,32,32,32,32,32,32,32,32,32,32,32,32,32,32,32 };

	SymmetricAlgorithm syma;

	public EncryptExtension ()
	{
	}

	public override Stream ChainStream( Stream stream )
	{
		if (syma == null) return stream;

		oldStream = stream;
		newStream = new MemoryStream ();
		return newStream;
	}

	SymmetricAlgorithm CreateAlgorithm ()
	{
		SymmetricAlgorithm algo = Rijndael.Create ();
		algo.Key = key;
		algo.IV = iv;
		algo.Mode = CipherMode.CBC;
		algo.Padding = PaddingMode.None;
		return algo;
	}

	public override object GetInitializer(LogicalMethodInfo methodInfo, SoapExtensionAttribute attribute) 
	{
		return CreateAlgorithm ();
	}

	public override object GetInitializer(Type webServiceType) 
	{
		if (webServiceType.GetCustomAttributes (typeof (EncryptAttribute), false).Length > 0)
			return CreateAlgorithm ();
		else
			return null;
	}

	public override void Initialize(object initializer) 
	{
		syma = (SymmetricAlgorithm) initializer;
	}

	public override void ProcessMessage(SoapMessage message) 
	{
		if (syma == null) return;

		switch (message.Stage) 
		{
			case SoapMessageStage.BeforeSerialize:
				break;
			case SoapMessageStage.AfterSerialize:
				Encrypt (message);
				break;
			case SoapMessageStage.BeforeDeserialize:
				Decrypt (message);
				break;
			case SoapMessageStage.AfterDeserialize:
				break;
			default:
				throw new Exception("invalid stage");
		}
	}

	public void Encrypt (SoapMessage message)
	{
		MemoryStream mems = new MemoryStream ();
		CryptoStream encStream = new CryptoStream (mems, syma.CreateEncryptor(), CryptoStreamMode.Write);
		encStream.Write (newStream.GetBuffer (), 0, (int) newStream.Length);
		int rn = (int) newStream.Length % (syma.BlockSize/8);
		if (rn > 0) encStream.Write (filler, 0, (syma.BlockSize/8) - rn);
		encStream.FlushFinalBlock ();
		encStream.Flush ();

		// Convert the encrypted content to a base 64 string
		string encString = Convert.ToBase64String (mems.GetBuffer (), 0, (int)mems.Length);
		byte[] encBytes = Encoding.UTF8.GetBytes (encString);
		oldStream.Write (encBytes, 0, encBytes.Length);
		oldStream.Flush ();

		encStream.Close ();
		mems.Close ();
	}

	public void Decrypt (SoapMessage message)
	{
		StreamReader sr = new StreamReader (oldStream, Encoding.UTF8);
		string encString = sr.ReadToEnd ();
		sr.Close ();

		byte[] encBytes = Convert.FromBase64String (encString);

		MemoryStream mems = new MemoryStream (encBytes);
		CryptoStream decStream = new CryptoStream (mems, syma.CreateDecryptor(), CryptoStreamMode.Write);
		decStream.Write (encBytes, 0, encBytes.Length);
		decStream.Close ();

		byte[] decArray = mems.ToArray ();
		newStream.Write (decArray, 0, decArray.Length);
		newStream.Position = 0;
		mems.Close ();
	}
}

[AttributeUsage(AttributeTargets.Class)]
public class EncryptAttribute: System.Attribute
{
}
