SUBDIRS = authtest

testfiles = 	browsercaps.aspx \
		button.aspx \
		calendar.aspx \
		codebehind1.aspx \
		code-render.aspx \
		databind-arraylist.aspx \
		databind-attribute.aspx \
		databind-class.aspx \
		databind-template.aspx \
		datalist.aspx \
		dbpage1.aspx \
		dbpage2.aspx \
		htmlanchor.aspx \
		htmlbutton.aspx \
		htmlgeneric.aspx \
		htmlimage.aspx \
		htmlinputbutton.aspx \
		htmlinputcheckbox.aspx \
		htmlinputfile.aspx \
		htmlinputhidden.aspx \
		htmlinputimage.aspx \
		htmlinputradiobutton.aspx \
		htmlinputtext.aspx \
		htmlselect.aspx \
		htmltable.aspx \
		htmltextarea.aspx \
		includetest.aspx \
		index.aspx \
		listitem.aspx \
		registertest.aspx \
		server-side-object.aspx \
		session1.aspx \
		tabcontrol2.aspx \
		tabcontrol.aspx \
		temperature.aspx \
		typedesc.aspx \
		validator1.aspx \
		web_adrotator.aspx \
		web_button.aspx \
		web_checkbox.aspx \
		web_checkboxlist.aspx \
		web_datagrid.aspx \
		web_datagrid_command.aspx \
		web_dropdownlist.aspx \
		web_hyperlink.aspx \
		web_image.aspx \
		web_imagebutton.aspx \
		web_label.aspx \
		web_linkbutton.aspx \
		web_listbox.aspx \
		web_literal.aspx \
		web_panel.aspx \
		web_placeholder.aspx \
		web_radiobutton.aspx \
		web_radiobuttonlist.aspx \
		web_regularexpressionvalidator.aspx \
		web_repeater.aspx \
		web_table2.aspx \
		web_table.aspx \
		web_textbox.aspx \
		web_xml.aspx \
		registertest1.ascx \
		registertest2.ascx \
		ConverterService.asmx \
		TestService.asmx \
		monodoc.ashx \
		webhandler.ashx \
		mod-mono-server.exe.config \
		ServiceClient.exe.config  \
		xsp.exe.config \
		web.config \
		global.asax \
		people.xml \
		web_adrotator.xml \
		peopletable.xsl \
		mono.png \
		monobutton.png \
		mono-powered-big.png \
		small-icon.png \
		body.inc \
		header.inc \
		CompressExtension.cs \
		sample.webapp \
		favicon.ico \
		mono-xsp.css

typedesc_src = typedesc.cs
tabcontrol_src = tabcontrol.cs
tabcontrol2_src = tabcontrol2.cs
codebehind_src = codebehind1.cs
extensions_src = TraceExtension.cs EncryptExtension.cs DumpExtension.cs
serviceclient_src = ServiceClient.cs Reference.cs

typedesc_build = $(addprefix $(srcdir)/, $(typedesc_src))
tabcontrol_build = $(addprefix $(srcdir)/, $(tabcontrol_src))
tabcontrol2_build = $(addprefix $(srcdir)/, $(tabcontrol2_src))
codebehind_build = $(addprefix $(srcdir)/, $(codebehind_src))
extensions_build = $(addprefix $(srcdir)/, $(extensions_src))
serviceclient_build = $(addprefix $(srcdir)/, $(serviceclient_src))


samplesdir = $(datadir)/doc/xsp/test
samples_DATA = $(testfiles)

samplesexedir = $(datadir)/doc/xsp/test
samplesexe_SCRIPTS = ServiceClient.exe extensions.dll

samplesbindir = $(datadir)/doc/xsp/test/bin
samplesbin_SCRIPTS = tabcontrol.dll tabcontrol2.dll codebehind1.dll extensions.dll typedesc.dll

CLEANFILES = $(samplesbin_SCRIPTS) $(samplesexe_SCRIPTS)

EXTRA_DIST = $(testfiles) \
	     $(typedesc_src) \
	     $(tabcontrol_src) \
	     $(tabcontrol2_src) \
	     $(codebehind_src) \
	     $(extensions_src) \
	     $(serviceclient_src)

%.dll: %.cs
	$(MCS) -r:System.Web.dll -t:library -out:$@ $^

tabcontrol.dll: $(tabcontrol_build)

tabcontrol2.dll: $(tabcontrol2_build)

codebehind1.dll: $(codebehind_build)

typedesc.dll: $(typedesc_build)

extensions.dll: $(extensions_build)
	$(MCS) -r:System.Web.dll -r:System.Web.Services.dll -target:library \
		-out:$@ $^

ServiceClient.exe: extensions.dll $(serviceclient_build)
	$(MCS) -r:System.Web.dll -r:System.Web.Services.dll -r:extensions.dll \
		-out:$@ \
		$(srcdir)/ServiceClient.cs $(srcdir)/Reference.cs

