CSC=mcs

%.dll: %.cs
	$(CSC) /r:System.Web.dll /target:library $<

all: tabcontrol.dll tabcontrol2.dll codebehind1.dll extensions.dll ServiceClient.exe

tabcontrol.dll: tabcontrol.cs

tabcontrol2.dll: tabcontrol2.cs

codebehind1.dll: codebehind1.cs

extensions.dll: TraceExtension.cs EncryptExtension.cs DumpExtension.cs
	$(CSC) TraceExtension.cs EncryptExtension.cs DumpExtension.cs /r:System.Web.dll /r:System.Web.Services.dll /target:library /out:extensions.dll

ServiceClient.exe: extensions.dll ServiceClient.cs Reference.cs
	$(CSC) ServiceClient.cs Reference.cs /r:System.Web.dll /r:System.Web.Services.dll /r:extensions.dll
	
clean:
	rm -f *.dll *~ *.pdb

