CSC=csc.exe
CSCFLAGS=/nologo /debug+ /debug:full /optimize $(REFS)
REFERENCES= System.Web		\
	    System.Drawing

SOURCES = asp-parser.cs \
	  asp-tokenizer.cs \
	  aspelements.cs \
	  driver.cs \
	  generator.cs

REFS= $(addsuffix .dll, $(addprefix /r:, $(REFERENCES)))

TARGET= xsp.exe

all: $(TARGET)

$(TARGET): $(SOURCES)
	$(CSC) $(CSCFLAGS) /out:$@ $^

clean:
	rm -f $(TARGET) *~ *.pdb

