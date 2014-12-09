samplesdir = $(pkglibdir)/test/1.1/webcontrols
samples_DATA = \
		listitem.aspx \
		temperature.aspx \
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
		web_comparevalidator.aspx \
		web_repeater.aspx \
		web_table2.aspx \
		web_table.aspx \
		web_textbox.aspx \
		web_xml.aspx \
		calendar.aspx \
		datalist.aspx \
		dbpage1.aspx \
		dbpage2.aspx \
		people.xml \
		web_adrotator.xml \
		peopletable.xsl \
		index.aspx

if USE_SQLITE
sqlitedir = $(pkglibdir)/test/1.1/webcontrols
sqlite_DATA = dbpage1.sqlite \
	      dbpage2.sqlite

samplesbindir = $(pkglibdir)/test/1.1/webcontrols
samplesbin_SCRIPTS = dbpage_test_setup.exe
dbpage_test_setup_build = dbpage_test_setup.cs

EXTRA_DIST = $(sqlite_DATA) $(samples_DATA) $(dbpage_test_setup_build)

dbpage_test_setup.exe: $(dbpage_test_setup_build)
	$(DMCS) -debug:full -r:Mono.Data.Sqlite.dll -r:System.Data.dll -out:$@ $^

dbpage1.sqlite: dbpage_test_setup.exe
	-@$(RUNTIME) dbpage_test_setup.exe 2> /dev/null || echo "Warning: could not create test SQLite database"

dbpage2.sqlite: dbpage_test_setup.exe
	-@$(RUNTIME) dbpage_test_setup.exe 2> /dev/null || echo "Warning: could not create test SQLite database"
endif
