CSC=mcs

%.dll: %.cs
	$(CSC) /r:System.Web /target:library $<

all: tabcontrol.dll tabcontrol2.dll

tabcontrol.dll: tabcontrol.cs

tabcontrol2.dll: tabcontrol2.cs

clean:
	rm -f *.dll *~ *.pdb

