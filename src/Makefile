CSC=csc.exe
CSCFLAGS=/nologo /debug+ /debug:full /optimize $(REFS)
REFS= /r:System.Web.dll /r:System.Drawing.dll

SOURCES = asp-parser.cs asp-tokenizer.cs aspelements.cs driver.cs generator.cs
TARGET= xsp.exe

all: $(TARGET)

$(TARGET): $(SOURCES)
	$(CSC) $(CSCFLAGS) /r:System.Web.dll /out:$(TARGET) ${SOURCES}

clean:
	rm -f $(TARGET) *~ *.pdb
	

