CSC=mcs

%.dll: %.cs
	$(CSC) /r:System.Web.dll /target:library $<

all: tabcontrol.dll tabcontrol2.dll codebehind1.dll

tabcontrol.dll: tabcontrol.cs

tabcontrol2.dll: tabcontrol2.cs

codebehind1.dll: codebehind1.cs

clean:
	rm -f *.dll *~ *.pdb

