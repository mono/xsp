//
// generator.cs: actually generates C# code from the output of the parser.
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Licensed under the terms of the GNU GPL
//
// (C) 2002 Ximian, Inc (http://www.ximian.com)
//

namespace Mono.ASP
{
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Drawing;
	using System.IO;
	using System.Reflection;
	using System.Text;
	using System.Web.UI.WebControls;

class Foundry
{
	private static Hashtable foundries;
	struct InternalFoundry {
		public string [] str;
		public Foundry foundry;

		public InternalFoundry (string assembly_name, string name_space)
		{
			str = new string [2];
			str [0] = assembly_name;
			str [1] = name_space;
			foundry = null;
		}
	}

	private string name_space;
	private Assembly assembly;

	static Foundry ()
	{
		foundries = new Hashtable (new CaseInsensitiveHashCodeProvider (),
					   new CaseInsensitiveComparer ());
		RegisterFoundry ("asp", "System.Web", "System.Web.UI.WebControls");
	}

	private Foundry (string assembly_name, string name_space)
	{
		assembly = Assembly.LoadWithPartialName (assembly_name);
		this.name_space = name_space;
	}

	public Component MakeComponent (string component_name, Tag tag)
	{
		Type type = assembly.GetType (name_space + "." + component_name, true, true);
		return new Component (tag, type);
	}

	public static void RegisterFoundry (string foundry_name, string assembly_name, string name_space)
	{
		InternalFoundry i_foundry = new InternalFoundry (assembly_name, name_space);
		foundries.Add (foundry_name, i_foundry);
	}

	public static Foundry LookupFoundry (string foundry_name)
	{
		if (!foundries.Contains (foundry_name))
			return null;

		InternalFoundry i_foundry = (InternalFoundry) foundries [foundry_name];
		if (i_foundry.foundry == null)
			i_foundry.foundry = new Foundry (i_foundry.str [0], i_foundry.str [1]);

		return i_foundry.foundry;
	}
}

class ArrayListWrapper
{
	private ArrayList list;
	private int index;

	public ArrayListWrapper (ArrayList list){
		this.list = list;
		index = -1;
	}

	public object Current {
		get { return list [index]; }
		set { list [index] = value; }
	}

	public bool MoveNext (){
		if (index < list.Count)
			index++;
		return index < list.Count;
	}

	public object Peek (){
		if (index + 1 >= list.Count)
			return null;
		return list [index + 1];
	}

	public object Next (){
		if (!MoveNext ())
			return null;
		return Current;
	}
}

class ControlStack
{
	private Stack controls;
	private ControlStackData top;
	private Type last_container_type;

	class ControlStackData 
	{
		public Type controlType;
		public string controlID;
		public string tagID;
		public ChildrenKind childKind;
		public string defaultPropertyName;
		public int childrenNumber;

		public ControlStackData (Type controlType,
					 string controlID,
					 string tagID,
					 ChildrenKind childKind,
					 string defaultPropertyName)
		{
			this.controlType = controlType;
			this.controlID = controlID;
			this.tagID = tagID;
			this.childKind = childKind;
			this.defaultPropertyName = defaultPropertyName;
			childrenNumber = 0;
		}

		public override string ToString ()
		{
			return controlType + " " + controlID + " " + tagID + " " + childKind + " " + childrenNumber;
		}
	}
	
	public ControlStack ()
	{
		controls = new Stack ();
	}

	private void SetContainerType (Type type)
	{
		if (type != typeof (System.Web.UI.Control) &&
		    !type.IsSubclassOf (typeof (System.Web.UI.Control)))
			return;
		
		if (type == typeof (System.Web.UI.WebControls.DataList))
			last_container_type = typeof (System.Web.UI.WebControls.DataListItem);
		else if (type == typeof (System.Web.UI.WebControls.DataGrid))
			last_container_type = typeof (System.Web.UI.WebControls.DataGridItem);
		else if (type == typeof (System.Web.UI.WebControls.Repeater))
			last_container_type = typeof (System.Web.UI.WebControls.RepeaterItem);
		else 
			last_container_type = type;
	}

	public void Push (Type controlType,
			  string controlID,
			  string tagID,
			  ChildrenKind childKind,
			  string defaultPropertyName)
	{
		if (controlType != null){
			AddChild ();
			SetContainerType (controlType);
		}
		top = new ControlStackData (controlType, controlID, tagID, childKind, defaultPropertyName);
		controls.Push (top);
	}

	public void Pop ()
	{
		controls.Pop ();
		if (controls.Count != 0)
			top = (ControlStackData) controls.Peek ();
	}

	public Type PeekType ()
	{
		return top.controlType;
	}

	public string PeekControlID ()
	{
		return top.controlID;
	}

	public string PeekTagID ()
	{
		return top.tagID;
	}

	public ChildrenKind PeekChildKind ()
	{
		return top.childKind;
	}

	public string PeekDefaultPropertyName ()
	{
		return top.defaultPropertyName;
	}

	public void AddChild ()
	{
		if (top != null)
			top.childrenNumber++;
	}
	
	public Type Container
	{
		get { return last_container_type; }
	}
	
	public int Count
	{
		get { return controls.Count; }
	}
}

public class Generator
{
	private object [] parts;
	private ArrayListWrapper elements;
	private StringBuilder prolog;
	private StringBuilder declarations;
	private StringBuilder script;
	private StringBuilder constructor;
	private StringBuilder init_funcs;
	private StringBuilder epilog;
	private StringBuilder current_function;
	private Stack functions;
	private ControlStack controls;
	private bool parse_ok;
	private bool has_form_tag;

	private string classDecl;
	private string className;
	private string interfaces;
	private string parent;
	private string fullPath;
	private static string enableSessionStateLiteral =  ", System.Web.SessionState.IRequiresSessionState";

	public Generator (string filename, ArrayList elements)
	{
		if (elements == null)
			throw new ArgumentNullException ();

		this.elements = new ArrayListWrapper (elements);
		this.className = filename.Replace ('.', '_'); // Overridden by @ Page classname
		this.className = className.Replace ('-', '_'); 
		this.className = className.Replace (' ', '_');
		this.fullPath = Path.GetFullPath (filename);
		//FIXME: get them from directives
		this.parent = "System.Web.UI.Page";
		//
		this.interfaces = enableSessionStateLiteral;
		this.has_form_tag = false;
		Init ();
	}

	private void Init ()
	{
		controls = new ControlStack ();
		controls.Push (typeof (System.Web.UI.Control), null, null, ChildrenKind.CONTROLS, null);
		prolog = new StringBuilder ();
		declarations = new StringBuilder ();
		script = new StringBuilder ();
		constructor = new StringBuilder ();
		init_funcs = new StringBuilder ();
		epilog = new StringBuilder ();

		current_function = new StringBuilder ();
		functions = new Stack ();
		functions.Push (current_function);

		parts = new Object [6];
		parts [0] = prolog;
		parts [1] = declarations;
		parts [2] = script;
		parts [3] = constructor;
		parts [4] = init_funcs;
		parts [5] = epilog;

		prolog.Append ("namespace ASP {\n" +
			      "\tusing System;\n" + 
			      "\tusing System.Collections;\n" + 
			      "\tusing System.Collections.Specialized;\n" + 
			      "\tusing System.Configuration;\n" + 
			      "\tusing System.IO;\n" + 
			      "\tusing System.Text;\n" + 
			      "\tusing System.Text.RegularExpressions;\n" + 
			      "\tusing System.Web;\n" + 
			      "\tusing System.Web.Caching;\n" + 
			      "\tusing System.Web.Security;\n" + 
			      "\tusing System.Web.SessionState;\n" + 
			      "\tusing System.Web.UI;\n" + 
			      "\tusing System.Web.UI.WebControls;\n" + 
			      "\tusing System.Web.UI.HtmlControls;\n");

		declarations.Append ("\t\tprivate static int __autoHandlers;\n");

		current_function.Append ("\t\tprivate void __BuildControlTree (System.Web.UI.Control __ctrl)\n\t\t{\n" + 
					"\t\t\tSystem.Web.UI.IParserAccessor __parser = " + 
					"(System.Web.UI.IParserAccessor) __ctrl;\n\n");
	}

	public void Print ()
	{
		if (!parse_ok){
			Console.WriteLine ("//Warning!!!: Elements not correctly parsed.");
		}

		int i;
		StringBuilder code_chunk;
		
		for (i = 0; i < parts.Length; i++){
			code_chunk = (StringBuilder) parts [i];
			Console.Write (code_chunk.ToString ());
		}
	}

	// Regexp.Escape () make some illegal escape sequences for a C# source.
	private string Escape (string input)
	{
		string output = input.Replace ("\\", "\\\\");
		output = output.Replace ("\"", "\\\"");
		output = output.Replace ("\t", "\\t");
		output = output.Replace ("\r", "\\r");
		output = output.Replace ("\n", "\\n");
		output = output.Replace ("\n", "\\n");
		return output;
	}
	
	private void PageDirective (TagAttributes att)
	{
		if (att ["ClassName"] != null)
			this.className = (string) att ["ClassName"];

		if (att ["EnableSessionState"] != null){
			string est = (string) att ["EnableSessionState"];
			if (0 == String.Compare (est, "false", true))
				interfaces = interfaces.Replace (enableSessionStateLiteral, "");
			else if (0 != String.Compare (est, "true", true))
				throw new ApplicationException ("EnableSessionState in Page directive not set to " +
								"a correct value: " + est);

		}
		//FIXME: add support for more attributes.
	}

	private void RegisterDirective (TagAttributes att)
	{
		string tag_prefix = (string) (att ["tagprefix"] == null ?  "" : att ["tagprefix"]);
		string name_space = (string) (att ["namespace"] == null ?  "" : att ["namespace"]);
		string assembly_name = (string) (att ["assembly"] == null ?  "" : att ["assembly"]);
		string tag_name =  (string) (att ["tagname"] == null ?  "" : att ["tagname"]);
		string src = (string) (att ["src"] == null ?  "" : att ["src"]);

		if (tag_prefix != "" && name_space != "" && assembly_name != ""){
			if (tag_name != "" || src != "")
				throw new ApplicationException ("Invalid attributes for @ Register: " +
								att.ToString ());
			prolog.AppendFormat ("\tusing {0};\n", name_space);
			Foundry.RegisterFoundry (tag_prefix, assembly_name, name_space);
			return;
		}

		if (tag_prefix != "" && tag_name != "" && src != ""){
			if (name_space != "" && assembly_name != "")
				throw new ApplicationException ("Invalid attributes for @ Register: " +
								att.ToString ());
			//FIXME:
			throw new ApplicationException ("<%@ Register Tagprefix=xxx " +
							"Tagname=yyy Src=zzzz %> " +
							"not supported yet.");
		}

		throw new ApplicationException ("Invalid combination of attributes in " +
						"@ Register: " + att.ToString ());
	}

	private void ProcessDirective ()
	{
		Directive directive = (Directive) elements.Current;
		TagAttributes att = directive.Attributes;
		if (att == null)
			return;

		switch (directive.TagID.ToUpper ()){
		case "PAGE":
			PageDirective (att);
			break;
		case "IMPORT":
			foreach (string key in att.Keys){
				if (0 == String.Compare (key, "NAMESPACE", true)){
					string _using = "using " + (string) att [key] + ";";
					if (prolog.ToString ().IndexOf (_using) == -1)
						prolog.AppendFormat ("\tusing {0};\n", (string) att [key]);
					break;
				}
			}
			break;
		case "IMPLEMENTS":
			string iface = (string) att ["interface"];
			interfaces += ", " + iface;
			break;
		case "REGISTER":
			RegisterDirective (att);
			break;
		}
	}

	private void ProcessPlainText ()
	{
		PlainText asis;
		if (controls.PeekChildKind () != ChildrenKind.CONTROLS){
			asis = (PlainText) elements.Current;
			string result = asis.Text.Trim ();
			if (result != ""){
				string tag_id = controls.PeekTagID ();
				throw new ApplicationException ("Literal content not allowed for " + tag_id);
			}
			return;
		}
		
		asis = (PlainText) elements.Current;
		current_function.AppendFormat ("\t\t\t__parser.AddParsedSubObject (" + 
					       "new System.Web.UI.LiteralControl (\"{0}\"));\n",
					       Escape (asis.Text));
		controls.AddChild ();
	}

	private string EnumValueNameToString (Type enum_type, string value_name)
	{
		if (value_name.EndsWith ("*"))
			throw new ApplicationException ("Invalid property value: '" + value_name + 
							". It must be a valid " + enum_type.ToString () + " value.");

		MemberInfo [] nested_types = enum_type.FindMembers (MemberTypes.Field, 
								    BindingFlags.Public | BindingFlags.Static,
								    Type.FilterNameIgnoreCase,
								    value_name);

		if (nested_types.Length == 0)
			throw new ApplicationException ("Value " + value_name + " not found in enumeration " +
							enum_type.ToString ());
		if (nested_types.Length > 1)
			throw new ApplicationException ("Value " + value_name + " found " + 
							nested_types.Length + " in enumeration " +
							enum_type.ToString ());

		return enum_type.ToString () + "." + nested_types [0].Name;
	}
	
	private void NewControlFunction (string tag_id,
					 string control_id,
					 Type control_type,
					 ChildrenKind children_kind,
					 string defaultPropertyName)
	{
		ChildrenKind prev_children_kind = controls.PeekChildKind ();
		if (prev_children_kind != ChildrenKind.CONTROLS &&
		    prev_children_kind != ChildrenKind.DBCOLUMNS &&
		    prev_children_kind != ChildrenKind.LISTITEM){
			string prev_tag_id = controls.PeekTagID ();
			throw new ApplicationException ("Child controls not allowed for " + prev_tag_id);
		}

		if (prev_children_kind == ChildrenKind.DBCOLUMNS &&
		    control_type != typeof (System.Web.UI.WebControls.DataGridColumn) &&
		    !control_type.IsSubclassOf (typeof (System.Web.UI.WebControls.DataGridColumn)))
			throw new ApplicationException ("Inside " + controls.PeekTagID () + " only " + 
							"System.Web.UI.WebControls.DataGridColum " + 
							"objects are allowed");
		else if (prev_children_kind == ChildrenKind.LISTITEM &&
			 control_type != typeof (System.Web.UI.WebControls.ListItem))
			throw new ApplicationException ("Inside " + controls.PeekTagID () + " only " + 
							"System.Web.UI.WebControls.ListItem " + 
							"objects are allowed");
	
					
		StringBuilder func_code = new StringBuilder ();
		current_function = func_code;
		if (0 == String.Compare (tag_id, "form", true)){
			if (has_form_tag)
				throw new ApplicationException ("Only one form server tag allowed.");
			has_form_tag = true;
		}

		controls.Push (control_type, control_id, tag_id, children_kind, defaultPropertyName);
		bool is_generic = control_type ==  typeof (System.Web.UI.HtmlControls.HtmlGenericControl);
		functions.Push (current_function);
		if (control_type != typeof (System.Web.UI.WebControls.ListItem))
			current_function.AppendFormat ("\t\tprivate System.Web.UI.Control __BuildControl_" +
							"{0} ()\n\t\t{{\n\t\t\t{1} __ctrl;\n\n\t\t\t__ctrl" +
							" = new {1} ({2});\n\t\t\tthis.{0} = __ctrl;\n",
							control_id, control_type,
							(is_generic? "\"" + tag_id + "\"" : ""));
		else
			current_function.AppendFormat ("\t\tprivate void __BuildControl_{0} ()\n\t\t{{" +
							"\n\t\t\t{1} __ctrl;\n\t\t\t__ctrl = new {1} ();" +
							"\n\t\t\tthis.{0} = __ctrl;\n",
							control_id, control_type);
	}
	
	/*
	 * Returns true if it generates some code for the specified property
	 */
	private void AddPropertyCode (Type prop_type, string var_name, string att)
	{
		/* FIXME: should i check for this or let the compiler fail?
		 * if (!prop.CanWrite)
		 *    ....
		 */
		if (prop_type == typeof (string)){
			if (att == null)
				throw new ApplicationException ("null value for attribute " + var_name );

			current_function.AppendFormat ("\t\t\t__ctrl.{0} = \"{1}\";\n", var_name,
							Escape (att)); // FIXME: really Escape this?
		} 
		else if (prop_type.IsEnum){
			if (att == null)
				throw new ApplicationException ("null value for attribute " + var_name );

			string enum_value = EnumValueNameToString (prop_type, att);

			current_function.AppendFormat ("\t\t\t__ctrl.{0} = {1};\n", var_name, enum_value);
		} 
		else if (prop_type == typeof (bool)){
			string value;
			if (att == null)
				value = "true"; //FIXME: is this ok for non Style properties?
			else if (0 == String.Compare (att, "true", true))
				value = "true";
			else if (0 == String.Compare (att, "false", true))
				value = "false";
			else
				throw new ApplicationException ("Value '" + att  + "' is not a valid boolean.");

			current_function.AppendFormat ("\t\t\t__ctrl.{0} = {1};\n", var_name, value);
		}
		else if (prop_type == typeof (System.Web.UI.WebControls.Unit)){
			 //FIXME: should use the culture specified in Page
			try {
				Unit value = Unit.Parse (att, System.Globalization.CultureInfo.InvariantCulture);
			} catch (Exception) {
				throw new ApplicationException ("'" + att + "' cannot be parsed as a unit.");
			}
			current_function.AppendFormat ("\t\t\t__ctrl.{0} = " + 
							"System.Web.UI.WebControls.Unit.Parse (\"{1}\", " + 
							"System.Globalization.CultureInfo.InvariantCulture);\n", 
							var_name, att);
		}
		else if (prop_type == typeof (System.Web.UI.WebControls.FontUnit)){
			 //FIXME: should use the culture specified in Page
			try {
				FontUnit value = FontUnit.Parse (att, System.Globalization.CultureInfo.InvariantCulture);
			} catch (Exception) {
				throw new ApplicationException ("'" + att + "' cannot be parsed as a unit.");
			}
			current_function.AppendFormat ("\t\t\t__ctrl.{0} = " + 
							"System.Web.UI.WebControls.FontUnit.Parse (\"{1}\", " + 
							"System.Globalization.CultureInfo.InvariantCulture);\n", 
							var_name, att);
		}
		else if (prop_type == typeof (Int16) ||
			 prop_type == typeof (Int32) ||
			 prop_type == typeof (Int64)){
			long value;
			try {
				value = Int64.Parse (att); //FIXME: should use the culture specified in Page
			} catch (Exception){
				throw new ApplicationException (att + " is not a valid signed number " + 
								"or is out of range.");
			}

			current_function.AppendFormat ("\t\t\t__ctrl.{0} = {1};\n", var_name, value);
		}
		else if (prop_type == typeof (UInt16) ||
			 prop_type == typeof (UInt32) ||
			 prop_type == typeof (UInt64)){
			ulong value;
			try {
				value = UInt64.Parse (att); //FIXME: should use the culture specified in Page
			} catch (Exception){
				throw new ApplicationException (att + " is not a valid unsigned number " + 
								"or is out of range.");
			}

			current_function.AppendFormat ("\t\t\t__ctrl.{0} = {1};\n", var_name, value);
		}
		else if (prop_type == typeof (float)){
			float value;
			try {
				value = Single.Parse (att);
			} catch (Exception){
				throw new ApplicationException (att + " is not  avalid float number or " +
								"is out of range.");
			}

			current_function.AppendFormat ("\t\t\t__ctrl.{0} = {1};\n", var_name, value);
		}
		else if (prop_type == typeof (double)){
			double value;
			try {
				value = Double.Parse (att);
			} catch (Exception){
				throw new ApplicationException (att + " is not  avalid double number or " +
								"is out of range.");
			}

			current_function.AppendFormat ("\t\t\t__ctrl.{0} = {1};\n", var_name, value);
		}
		else if (prop_type == typeof (System.Drawing.Color)){
			Color c;
			try {
				c = (Color) TypeDescriptor.GetConverter (typeof (Color)).ConvertFromString (att);
			} catch (Exception e){
				throw new ApplicationException ("Color " + att + " is not a valid color.", e);
			}

			// Should i also test for IsSystemColor?
			// Are KnownColor members in System.Drawing.Color?
			if (c.IsKnownColor){
				current_function.AppendFormat ("\t\t\t__ctrl.{0} = System.Drawing.Color." +
							       "{1};\n", var_name, c.Name);
			}
			else {
				current_function.AppendFormat ("\t\t\t__ctrl.{0} = System.Drawing.Color." +
							       "FromArgb ({1}, {2}, {3}, {4});\n",
							       var_name, c.A, c.R, c.G, c.B);
			}
		}	
		else {
			throw new ApplicationException ("Unsupported type in property: " + 
							prop_type.ToString ());
		}
	}

	private bool ProcessProperties (PropertyInfo prop, string id, TagAttributes att)
	{
		bool is_processed = false;

		if (0 == String.Compare (prop.Name, id, true)){
			AddPropertyCode (prop.PropertyType, prop.Name, (string) att [id]);
			is_processed = true;
		}
		else if (prop.PropertyType == typeof (System.Web.UI.WebControls.FontInfo) &&
			 id.IndexOf ('-') != -1){
			string prop_field = id.Replace ("-", ".");
			string [] parts = prop_field.Split (new char [] {'.'});
			if (parts.Length != 2 || 
			    0 != String.Compare (prop.Name, parts [0], true))
				return false;

			PropertyInfo [] subprops = prop.PropertyType.GetProperties ();
			foreach (PropertyInfo subprop in subprops){
				if (0 != String.Compare (subprop.Name, parts [1], true))
					return false;

				bool is_bool = subprop.PropertyType == typeof (bool);
				if (!is_bool && att == null){
					att [id] = ""; // Font-Size -> Font-Size="" as html
					return false;
				}

				string value;
				if (att == null && is_bool)
					value = "true"; // Font-Bold <=> Font-Bold="true"
				else
					value = (string) att [id];

				AddPropertyCode (subprop.PropertyType,
						 prop.Name + "." + subprop.Name,
						 value);
				is_processed = true;
			}
		}

		return is_processed;
	}
	
	private void AddCodeForAttributes (Type type, TagAttributes att)
	{
		EventInfo [] ev_info = type.GetEvents ();
		PropertyInfo [] prop_info = type.GetProperties ();
		bool is_processed = false;
		ArrayList processed = new ArrayList ();

		foreach (string id in att.Keys){
			if (0 == String.Compare (id, "runat", true) || 0 == String.Compare (id, "id", true))
				continue;

			if (id.Length > 2 && id.Substring (0, 2).ToUpper () == "ON"){
				string id_as_event = id.Substring (2);
				foreach (EventInfo ev in ev_info){
					if (0 == String.Compare (ev.Name, id_as_event, true)){
						current_function.AppendFormat (
								"\t\t\t__ctrl.{0} += " + 
								"new System.EventHandler (this.{1});\n", 
								ev.Name, att [id]);
						is_processed = true;
						break;
					}
				}
				if (is_processed){
					is_processed = false;
					continue;
				}
			} 

			foreach (PropertyInfo prop in prop_info){
				is_processed = ProcessProperties (prop, id, att);
				if (is_processed)
					break;
			}

			if (is_processed){
				is_processed = false;
				continue;
			}

			current_function.AppendFormat ("\t\t\t((System.Web.UI.IAttributeAccessor) __ctrl)." +
						"SetAttribute (\"{0}\", \"{1}\");\n",
						id, Escape ((string) att [id]));
		}

		if (controls.PeekChildKind () == ChildrenKind.CONTROLS)
			current_function.Append ("\t\t\tSystem.Web.UI.IParserAccessor __parser = " + 
						 "(System.Web.UI.IParserAccessor) __ctrl;\n");
	}
	
	private bool FinishControlFunction (string tag_id)
	{
		if (functions.Count == 0)
			throw new ApplicationException ("Unbalanced open/close tags");

		if (controls.Count == 0)
			return false;

		string saved_id = controls.PeekTagID ();
		if (0 != String.Compare (saved_id, tag_id, true))
			return false;

		StringBuilder old_function = (StringBuilder) functions.Pop ();
		current_function = (StringBuilder) functions.Peek ();

		string control_id = controls.PeekControlID ();
		Type control_type = controls.PeekType ();
		
		if (control_type == typeof (System.Web.UI.ITemplate)){
			old_function.Append ("\n\t\t}\n\n");
			current_function.AppendFormat ("\t\t\t__ctrl.{0} = new System.Web.UI." + 
						       "CompiledTemplateBuilder (new System.Web.UI." +
						       "BuildTemplateMethod (this.__BuildControl_{1}));\n",
						       saved_id, control_id);
		}
		else if (control_type == typeof (System.Web.UI.WebControls.DataGridColumnCollection)){
			old_function.Append ("\n\t\t}\n\n");
			current_function.AppendFormat ("\t\t\tthis.__BuildControl_{0} (__ctrl.{1});\n",
							control_id, saved_id);
		}
		else if (control_type == typeof (System.Web.UI.WebControls.DataGridColumn) ||
			 control_type.IsSubclassOf (typeof (System.Web.UI.WebControls.DataGridColumn)) ||
			 control_type == typeof (System.Web.UI.WebControls.ListItem)){
			old_function.Append ("\n\t\t}\n\n");
			current_function.AppendFormat ("\t\t\tthis.__BuildControl_{0} ();\n" +
						       "\t\t\t__ctrl.Add (this.{0});\n\n", control_id);
		}
		else if (controls.PeekChildKind () == ChildrenKind.LISTITEM){
			old_function.Append ("\n\t\t}\n\n");
			init_funcs.Append (old_function); // Closes the BuildList function
			old_function = (StringBuilder) functions.Pop ();
			current_function = (StringBuilder) functions.Peek ();
			old_function.AppendFormat ("\n\t\t\tthis.__BuildControl_{0} (__ctrl.{1});\n\t\t\t" +
						   "return __ctrl;\n\t\t}}\n\n",
						   control_id, controls.PeekDefaultPropertyName ());

			controls.Pop ();
			control_id = controls.PeekControlID ();
			current_function.AppendFormat ("\t\t\tthis.__BuildControl_{0} ();\n", control_id);
		}
		else {
			old_function.Append ("\n\t\t\treturn __ctrl;\n\t\t}\n\n");
			current_function.AppendFormat ("\t\t\tthis.__BuildControl_{0} ();\n\t\t\t__parser." +
						       "AddParsedSubObject (this.{0});\n\n", control_id);
		}

		init_funcs.Append (old_function);
		// Avoid getting empty stacks for unbalanced open/close tags
		if (controls.Count > 1)
			controls.Pop ();

		return true;
	}

	private void ProcessHtmlControlTag ()
	{
		HtmlControlTag html_ctrl = (HtmlControlTag) elements.Current;
		if (html_ctrl.TagID.ToUpper () == "SCRIPT"){
			//FIXME: if the is script is to be read from disk, do it!
			if (html_ctrl.SelfClosing)
				throw new ApplicationException ("Read script from file not supported yet.");

			Element element = (Element) elements.Peek ();
			if (element == null)
				throw new ApplicationException ("Error after " + html_ctrl.ToString ());

			if (element is PlainText){
				elements.MoveNext ();
				script.Append (((PlainText) element).Text);
			}

			element = (Element) elements.Peek ();
			if (element == null)
				throw new ApplicationException ("Error after " + elements.Current.ToString ());

			if (element is CloseTag)
				elements.MoveNext ();
			return;
		}
		
		Type controlType = html_ctrl.ControlType;
		declarations.AppendFormat ("\t\tprotected {0} {1};\n", controlType, html_ctrl.ControlID);

		ChildrenKind children_kind = html_ctrl.IsContainer ? ChildrenKind.CONTROLS :
								     ChildrenKind.NONE;
		NewControlFunction (html_ctrl.TagID, html_ctrl.ControlID, controlType, children_kind, null); 

		if (!html_ctrl.HasDefaultID)
			current_function.AppendFormat ("\t\t\tthis.ID = \"{0}\";\n", html_ctrl.ControlID);

		AddCodeForAttributes (html_ctrl.ControlType, html_ctrl.Attributes);

		if (!html_ctrl.SelfClosing)
			JustDoIt ();
		else
			FinishControlFunction (html_ctrl.TagID);
	}

	// Closing is performed in FinishControlFunction ()
	private void NewBuildListFunction (Component component)
	{
		string control_id = Tag.GetDefaultID ();

		controls.Push (component.ComponentType,
			       control_id, 
			       component.TagID, 
			       ChildrenKind.LISTITEM, 
			       component.DefaultPropertyName);

		current_function = new StringBuilder ();
		functions.Push (current_function);
		current_function.AppendFormat ("\t\tprivate void __BuildControl_{0} " +
						"(System.Web.UI.WebControls.ListItemCollection __ctrl)\n" +
						"\t\t{{\n", control_id);
	}

	private void ProcessComponent ()
	{
		Component component = (Component) elements.Current;
		Type component_type = component.ComponentType;
		declarations.AppendFormat ("\t\tprotected {0} {1};\n", component_type, component.ControlID);

		NewControlFunction (component.TagID, component.ControlID, component_type,
				    component.ChildrenKind, component.DefaultPropertyName); 

		if (!component.HasDefaultID)
			current_function.AppendFormat ("\t\t\tthis.ID = \"{0}\";\n", component.ControlID);

		AddCodeForAttributes (component.ComponentType, component.Attributes);
		if (component.ChildrenKind == ChildrenKind.LISTITEM)
			NewBuildListFunction (component);

		if (!component.SelfClosing)
			JustDoIt ();
		else
			FinishControlFunction (component.TagID);
	}

	private void ProcessServerObjectTag ()
	{
		ServerObjectTag obj = (ServerObjectTag) elements.Current;
		declarations.AppendFormat ("\t\tprivate {0} cached{1};\n", obj.ObjectClass, obj.ObjectID);
		constructor.AppendFormat ("\n\t\tprivate {0} {1}\n\t\t{{\n\t\t\tget {{\n\t\t\t\t" + 
					  "if (this.cached{1} == null)\n\t\t\t\t\tthis.cached{1} = " + 
					  "new {0} ();\n\t\t\t\treturn cached{1};\n\t\t\t}}\n\t\t}}\n\n",
					  obj.ObjectClass, obj.ObjectID);
	}

	// Creates a new function that sets the values of subproperties.
	private void NewStyleFunction (PropertyTag tag)
	{
		current_function = new StringBuilder ();

		string prop_id = tag.PropertyID;
		Type prop_type = tag.PropertyType;
		// begin function
		current_function.AppendFormat ("\t\tprivate void __BuildControl_{0} ({1} __ctrl)\n" +
						"\t\t{{\n", prop_id, prop_type);
		
		// Add property initialization code
		PropertyInfo [] subprop_info = prop_type.GetProperties ();
		TagAttributes att = tag.Attributes;

		string subprop_name = null;
		foreach (string id in att.Keys){
			if (0 == String.Compare (id, "runat", true) || 0 == String.Compare (id, "id", true))
				continue;

			bool is_processed = false;
			foreach (PropertyInfo subprop in subprop_info){
				is_processed = ProcessProperties (subprop, id, att);
				if (is_processed){
					subprop_name = subprop.Name;
					break;
				}
			}

			if (subprop_name == null)
				throw new ApplicationException ("Property " + tag.TagID + " does not have " + 
								"a " + id + " subproperty.");
		}

		// Finish function
		current_function.Append ("\n\t\t}\n\n");
		init_funcs.Append (current_function);
		current_function = (StringBuilder) functions.Peek ();
		current_function.AppendFormat ("\t\t\tthis.__BuildControl_{0} (__ctrl.{1});\n",
						prop_id, tag.PropertyName);

		if (!tag.SelfClosing){
			// Next tag should be the closing tag
			controls.Push (null, null, null, ChildrenKind.NONE, null);
			bool closing_tag_found = false;
			Element elem;
			while (!closing_tag_found && elements.MoveNext ()){
				elem = (Element) elements.Current;
				if (elem is PlainText)
					ProcessPlainText ();
				else if (!(elem is CloseTag))
					throw new ApplicationException ("Tag " + tag.TagID + 
									" not properly closed.");
				else
					closing_tag_found = true;
			}

			if (!closing_tag_found)
				throw new ApplicationException ("Tag " + tag.TagID + " not properly closed.");

			controls.Pop ();
		}
	}

	// This one just opens the function. Closing is performed in FinishControlFunction ()
	private void NewTemplateFunction (PropertyTag tag)
	{
		/*
		 * FIXME
		 * This function does almost the same as NewControlFunction.
		 * Consider merging.
		 */
		string prop_id = tag.PropertyID;
		Type prop_type = tag.PropertyType;
		string tag_id = tag.PropertyName; // Real property name used in FinishControlFunction

		controls.Push (prop_type, prop_id, tag_id, ChildrenKind.CONTROLS, null);
		current_function = new StringBuilder ();
		functions.Push (current_function);
		current_function.AppendFormat ("\t\tprivate void __BuildControl_{0} " +
						"(System.Web.UI.Control __ctrl)\n" +
						"\t\t{{\n" +
						"\t\t\tSystem.Web.UI.IParserAccessor __parser " + 
						"= (System.Web.UI.IParserAccessor) __ctrl;\n" , prop_id);
	}

	// Closing is performed in FinishControlFunction ()
	private void NewDBColumnFunction (PropertyTag tag)
	{
		/*
		 * FIXME
		 * This function also does almost the same as NewControlFunction.
		 * Consider merging.
		 */
		string prop_id = tag.PropertyID;
		Type prop_type = tag.PropertyType;
		string tag_id = tag.PropertyName; // Real property name used in FinishControlFunction

		controls.Push (prop_type, prop_id, tag_id, ChildrenKind.DBCOLUMNS, null);
		current_function = new StringBuilder ();
		functions.Push (current_function);
		current_function.AppendFormat ("\t\tprivate void __BuildControl_{0} " +
						"(System.Web.UI.WebControl.DataGridColumnCollection __ctrl)\n" +
						"\t\t{{\n", prop_id);
	}

	private void NewPropertyFunction (PropertyTag tag)
	{
		if (tag.PropertyType == typeof (System.Web.UI.WebControls.Style) ||
		    tag.PropertyType.IsSubclassOf (typeof (System.Web.UI.WebControls.Style)))
			NewStyleFunction (tag);
		else if (tag.PropertyType == typeof (System.Web.UI.ITemplate))
			NewTemplateFunction (tag);
		else if (tag.PropertyType == typeof (System.Web.UI.WebControls.DataGridColumnCollection))
			NewDBColumnFunction (tag);
		else
			throw new ApplicationException ("Other than Style and ITemplate not supported yet. " + 
							tag.PropertyType);
	}
	
	private void ProcessHtmlTag ()
	{
		Tag tag = (Tag) elements.Current;
		ChildrenKind child_kind = controls.PeekChildKind ();
		if (child_kind == ChildrenKind.NONE){
			string tag_id = controls.PeekTagID ();
			throw new ApplicationException (tag + " not allowed inside " + tag_id);
		}
					
		if (child_kind == ChildrenKind.CONTROLS){
			elements.Current = new PlainText (((Tag) elements.Current).PlainHtml);
			ProcessPlainText ();
			return;
		}

		// Now child_kind should be PROPERTIES, so only allow tag_id == property
		Type control_type = controls.PeekType ();
		PropertyInfo [] prop_info = control_type.GetProperties ();
		bool is_processed = false;
		foreach (PropertyInfo prop in prop_info){
			if (0 == String.Compare (prop.Name, tag.TagID, true)){
				PropertyTag prop_tag = new PropertyTag (tag, prop.PropertyType, prop.Name);
				NewPropertyFunction (prop_tag);
				is_processed = true;
				break;
			}
		}
		
		if (!is_processed){
			string tag_id = controls.PeekTagID ();
			throw new ApplicationException (tag.TagID + " is not a property of " + control_type);
		}
	}

	private Tag Map (Tag tag)
	{
		int pos = tag.TagID.IndexOf (":");
		if (tag is CloseTag || 
		    ((tag.Attributes == null || 
		    !tag.Attributes.IsRunAtServer ()) && pos == -1))
			return tag;

		if (pos == -1){
			if (0 == String.Compare (tag.TagID, "object", true))
				return new ServerObjectTag (tag);
			return new HtmlControlTag (tag);
		}

		string foundry_name = tag.TagID.Substring (0, pos);
		string component_name = tag.TagID.Substring (pos + 1);

		Foundry foundry = Foundry.LookupFoundry (foundry_name);
		if (foundry == null)
			throw new ApplicationException ("Cannot find foundry for alias'" + foundry_name + "'");

		Component component = foundry.MakeComponent (component_name, tag);
		if (component == null)
			throw new ApplicationException ("Cannot find component '" + component_name + 
							"' for alias '" + foundry_name + "'");

		return component;
	}
	
	private void ProcessCloseTag ()
	{
		CloseTag close_tag = (CloseTag) elements.Current;
		if (FinishControlFunction (close_tag.TagID))
				return;

		elements.Current = new PlainText (close_tag.PlainHtml);
		ProcessPlainText ();
	}

	private void ProcessDataBindingLiteral ()
	{
		DataBindingTag dataBinding = (DataBindingTag) elements.Current;
		string actual_value = dataBinding.Data.Trim ();
		if (actual_value == "")
			throw new ApplicationException ("Empty data binding tag.");

		if (controls.PeekChildKind () != ChildrenKind.CONTROLS)
			throw new ApplicationException ("Data bound content not allowed for " + 
							controls.PeekTagID ());

		StringBuilder db_function = new StringBuilder ();
		string control_id = Tag.GetDefaultID ();
		string control_type_string = "System.Web.UI.DataBoundLiteralControl";
		declarations.AppendFormat ("\t\tprotected {0} {1};\n", control_type_string, control_id);
		// Build the control
		db_function.AppendFormat ("\t\tprivate System.Web.UI.Control __BuildControl_{0} ()\n" +
					  "\t\t{{\n\t\t\t{1} __ctrl;\n\n" +
					  "\t\t\t__ctrl = new {1} (0, 1);\n" + 
					  "\t\t\tthis.{0} = __ctrl;\n" +
					  "\t\t\t__ctrl.DataBinding += new System.EventHandler " + 
					  "(this.__DataBind_{0});\n" +
					  "\t\t\treturn __ctrl;\n"+
					  "\t\t}}\n\n",
					  control_id, control_type_string);
		// DataBinding handler
		db_function.AppendFormat ("\t\tpublic void __DataBind_{0} (object sender, " + 
					  "System.EventArgs e) {{\n" +
					  "\t\t\t{1} Container;\n" +
					  "\t\t\t{2} target;\n" +
					  "\t\t\ttarget = ({2}) sender;\n" +
					  "\t\t\tContainer = ({1}) target.BindingContainer;\n" +
					  "\t\t\ttarget.SetDataBoundString (0, System.Convert." +
					  "ToString ({3}));\n" +
					  "\t\t}}\n\n",
					  control_id, controls.Container, control_type_string,
					  actual_value);

		init_funcs.Append (db_function);
		current_function.AppendFormat ("\t\t\tthis.__BuildControl_{0} ();\n\t\t\t__parser." +
					       "AddParsedSubObject (this.{0});\n\n", control_id);
	}

	private void ProcessCodeRenderTag ()
	{
	}
	
	public void ProcessElements ()
	{
		JustDoIt ();
		End ();
		parse_ok = true;
	}
	
	private void JustDoIt ()
	{
		Element element;

		while (elements.MoveNext ()){
			element = (Element) elements.Current;
			if (element is Directive){
				ProcessDirective ();
			} else if (element is PlainText){
				ProcessPlainText ();
			} else if (element is DataBindingTag){
				ProcessDataBindingLiteral ();
			} else if (element is CodeRenderTag){
				ProcessCodeRenderTag ();
			} else {
				elements.Current = Map ((Tag) element);
				if (elements.Current is HtmlControlTag)
					ProcessHtmlControlTag ();
				else if (elements.Current is Component)
					ProcessComponent ();
				else if (elements.Current is CloseTag)
					ProcessCloseTag ();
				else if (elements.Current is ServerObjectTag)
					ProcessServerObjectTag ();
				else if (elements.Current is Tag)
					ProcessHtmlTag ();
				else
					throw new ApplicationException ("This place should not be reached.");
			}
		}
	}

	private void End ()
	{
		classDecl = "\tpublic class " + className + " : " + parent + interfaces + " {\n"; 
		prolog.Append ("\n" + classDecl);
		declarations.Append (
			"\t\tprivate static bool __intialized = false;\n\n" +
			"\t\tprivate static System.Collections.ArrayList __fileDependencies;\n\n");

		// adds the constructor
		constructor.AppendFormat (
			"\t\tpublic {0} ()\n\t\t{{\n" + 
			"\t\t\tSystem.Collections.ArrayList dependencies;\n\n" +
			"\t\t\tif (ASP.{0}.__intialized == false){{\n" + 
			"\t\t\t\tdependencies = new System.Collections.ArrayList ();\n" +
			"\t\t\t\tdependencies.Add (@\"{1}\");\n" +
			"\t\t\t\tASP.{0}.__fileDependencies = dependencies;\n" +
			"\t\t\t\tASP.{0}.__intialized = true;\n" +
			"\t\t\t}}\n" +
			"\t\t}}\n\n", className, fullPath);
         
		//FIXME: add AutoHandlers: don't know what for...yet!
		constructor.AppendFormat (
			"\t\tprotected override int AutoHandlers\n\t\t{{\n" +
			"\t\t\tget {{ return ASP.{0}.__autoHandlers; }}\n" +
			"\t\t\tset {{ ASP.{0}.__autoHandlers = value; }}\n" +
			"\t\t}}\n\n", className);

		//FIXME: add ApplicationInstance: don't know what for...yet!
		constructor.Append (
			"\t\tprotected System.Web.HttpApplication ApplicationInstance\n\t\t{\n" +
			"\t\t\tget { return (System.Web.HttpApplication) this.Context.ApplicationInstance; }\n" +
			"\t\t}\n\n");
		//FIXME: add TemplateSourceDirectory: don't know what for...yet!
		//FIXME: it should be the path from the root where the file resides
		constructor.Append (
			"\t\tpublic override string TemplateSourceDirectory\n\t\t{\n" +
			"\t\t\tget { return \"/dummypath\"; }\n" +
			"\t\t}\n\n");

		Random rnd = new Random ();
		epilog.AppendFormat (
			"\n" +
			"\t\tprotected override void FrameworkInitialize ()\n\t\t{{\n" +
			"\t\t\tthis.__BuildControlTree (this);\n" +
			"\t\t\tthis.FileDependencies = ASP.{0}.__fileDependencies;\n" +
			"\t\t\tthis.EnableViewStateMac = true;\n" +
			"\t\t}}\n\n" + 
			"\t\tpublic override int GetTypeHashCode ()\n\t\t{{\n" +
			"\t\t\treturn {1};\n" +
			"\t\t}}\n\t}}\n}}\n", className, rnd.Next ());

		// Closes the currently opened tags
		StringBuilder old_function = current_function;
		string control_id;
		while (functions.Count > 1){
			old_function.Append ("\n\t\t\treturn __ctrl;\n\t\t}\n\n");
			init_funcs.Append (old_function);
			control_id = controls.PeekControlID ();
			current_function.AppendFormat ("\t\t\tthis.__BuildControl_{0} ();\n\t\t\t__parser." +
							"AddParsedSubObject (this.{0});\n\n", control_id);
			controls.AddChild ();
			old_function = (StringBuilder) functions.Pop ();
			current_function = (StringBuilder) functions.Peek ();
			controls.Pop ();
		}
		current_function.Append ("\t\t}\n\n");
		init_funcs.Append (current_function);
		functions.Pop ();
	}
}

}

