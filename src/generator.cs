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

namespace Mono.ASP {
	using System;
	using System.Collections;
	using System.IO;
	using System.Reflection;
	using System.Text;

class Foundry {
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

class ArrayListWrapper {
	private ArrayList list;
	private int index;

	public ArrayListWrapper (ArrayList list){
		this.list = list;
		index = 0;
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

public class Generator {
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
	private Stack openedControlTags;
	private bool parse_ok;

	private string classDecl;
	private string className;
	private string interfaces;
	private string parent;
	private string fullPath;

	public Generator (string filename, ArrayList elements)
	{
		if (elements == null)
			throw new ArgumentNullException ();

		this.elements = new ArrayListWrapper (elements);
		this.className = filename.Replace ('.', '_');
		this.fullPath = Path.GetFullPath (filename);
		this.parent = "System.Web.UI.Page";
		 //FIXME: get it from directives
		this.interfaces = ", System.Web.SessionState.IRequiresSessionState";
		 //FIXME: codebehind...
		this.classDecl = "\tpublic class " + className + " : " + parent + interfaces + " {\n"; 
		Init ();
	}

	private void Init ()
	{
		prolog = new StringBuilder ();
		declarations = new StringBuilder ();
		openedControlTags = new Stack ();
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
		Directive directive;

		directive = (Directive) elements.Current;
		TagAttributes att = directive.Attributes;
		if (att == null)
			return;

		switch (directive.TagID.ToUpper ()){
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
			case "REGISTER":
				RegisterDirective (att);
				break;
		}
	}

	private void ControlTreeAddEscaped (string text)
	{
		current_function.AppendFormat ("\t\t\t__parser.AddParsedSubObject (" + 
				"new System.Web.UI.LiteralControl (\"{0}\"));\n", Escape (text));
	}

	private void ProcessPlainText ()
	{
		PlainText asis;
		while (true){
			asis = (PlainText) elements.Current;
			ControlTreeAddEscaped (asis.Text);

			if (!(elements.Peek () is PlainText))
				break;
			elements.MoveNext ();
		}
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
	
	private void CreateNewFunction (string tag_id, string control_id, string control_type)
	{
		StringBuilder func_code = new StringBuilder ();
		current_function = func_code;
		openedControlTags.Push (control_id);
		openedControlTags.Push (tag_id);
		functions.Push (current_function);
		current_function.AppendFormat ("\t\tprivate System.Web.UI.Control __BuildControl{0} ()\n" +
						"\t\t{{\n\t\t\t{1} __ctrl;\n\n\t\t\t__ctrl = new {1} ();\n" + 
						"\t\t\tthis.{0} = __ctrl;\n", control_id, control_type);
	}
	
	private void AddCodeToFunction (Type type, TagAttributes att)
	{
		EventInfo [] ev_info = type.GetEvents ();
		PropertyInfo [] prop_info = type.GetProperties ();
		bool is_processed = false;
		ArrayList processed = new ArrayList ();

		foreach (string id in att.Keys){
			if (0 == String.Compare (id, "runat", true))
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
				if (0 == String.Compare (prop.Name, id, true)){
					/* FIXME: should i check for this or let the compiler fail?
					 * if (!prop.CanWrite)
					 *    ....
					 */

					//TODO: anything more than Enum and String?
					Type prop_type = prop.PropertyType;
					if (prop_type == typeof (string)){
						current_function.AppendFormat ("\t\t\t__ctrl.{0} = \"{1}\";\n",
										prop.Name,
										Escape ((string) att [id]));
					} else if (prop_type.IsEnum){
						string enum_value = 
						       EnumValueNameToString (prop_type, (string) att [id]);

						current_function.AppendFormat ("\t\t\t__ctrl.{0} = {1};\n",
										prop.Name, enum_value);
					} else {
						throw new ApplicationException (
							"Unsupported type in property: " + 
							prop_type.ToString ());
					}
					is_processed = true;
					break;
				}
			}

			if (is_processed){
				is_processed = false;
				continue;
			}

			current_function.AppendFormat ("\t\t\t((System.Web.UI.IAttributeAccessor) __ctrl)." +
						"SetAttribute (\"{0}\", \"{1}\");\n",
						id, Escape ((string) att [id]));
		}
            	current_function.Append ("\t\t\tSystem.Web.UI.IParserAccessor __parser = " + 
					 "(System.Web.UI.IParserAccessor) __ctrl;\n");
	}
	
	private bool FinishFunction (string tag_id)
	{
		if (functions.Count == 0)
			throw new ApplicationException ("Unbalanced open/close tags");

		if (openedControlTags.Count == 0)
			return false;

		string saved_id = (string) openedControlTags.Peek ();
		if (0 != String.Compare (saved_id, tag_id, true))
			return false;

		openedControlTags.Pop ();
		StringBuilder old_function = (StringBuilder) functions.Pop ();
		current_function = (StringBuilder) functions.Peek ();

		string control_id = (string) openedControlTags.Pop ();
		old_function.Append ("\n\t\t\treturn __ctrl;\n\t\t}\n\n");
		current_function.AppendFormat ("\t\t\tthis.__BuildControl{0} ();\n\t\t\t__parser." +
						"AddParsedSubObject (this.{0});\n\n", control_id);
		init_funcs.Append (old_function);
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

			if (element is CloseTag){
				elements.MoveNext ();
			}
			return;
		}
		
		string controlType = html_ctrl.ControlType.ToString ();
		declarations.AppendFormat ("\t\tprotected {0} {1};\n", controlType, html_ctrl.ControlID);

		CreateNewFunction (html_ctrl.TagID, html_ctrl.ControlID, controlType); 
		AddCodeToFunction (html_ctrl.ControlType, html_ctrl.Attributes);

		if (!html_ctrl.SelfClosing)
			JustDoIt ();
		else
			FinishFunction (html_ctrl.TagID);
	}

	private void ProcessComponent ()
	{
		Component component = (Component) elements.Current;
		if (component.IsCloseTag){
			FinishFunction (component.TagID);
			return;
		}
		
		string component_type = component.ComponentType.ToString ();
		declarations.AppendFormat ("\t\tprotected {0} {1};\n", component_type, component.ControlID);

		CreateNewFunction (component.TagID, component.ControlID, component_type); 
		AddCodeToFunction (component.ComponentType, component.Attributes);
		if (!component.SelfClosing)
			JustDoIt ();
		else
			FinishFunction (component.TagID);
	}

	private Tag Map (Tag tag)
	{
		if (tag is CloseTag || tag.Attributes == null || !tag.Attributes.IsRunAtServer ())
			return tag;

		int pos = tag.TagID.IndexOf (":");
		if (pos == -1)
			return new HtmlControlTag (tag);

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
	
	public void ProcessCloseTag ()
	{
		CloseTag close_tag = (CloseTag) elements.Current;
		if (FinishFunction (close_tag.TagID))
				return;

		elements.Current = new PlainText (close_tag.PlainHtml);
		ProcessPlainText ();
	}

	public void ProcessElements ()
	{
		JustDoIt ();
		End ();
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
			} else {
				elements.Current = Map ((Tag) element);
				if (elements.Current is HtmlControlTag)
					ProcessHtmlControlTag ();
				else if (elements.Current is Component)
					ProcessComponent ();
				else if (elements.Current is CloseTag)
					ProcessCloseTag ();
				else {
					if (elements.Current is Tag)
						elements.Current = new PlainText (((Tag) elements.Current).PlainHtml);

					ProcessPlainText ();
				}
			}
		}

		parse_ok = true;
	}

	private void End ()
	{
		prolog.Append ("\n" + classDecl);
		declarations.Append (
			"\t\tprivate static bool __intialized = false;\n\n" +
			"\t\tprivate static System.Collections.ArrayList __fileDependencies;\n\n");

		// Closes the currently opened tags
		StringBuilder old_function = current_function;
		string control_id;
		while (functions.Count > 1){
			openedControlTags.Pop (); // Contains the TagID
			old_function.Append ("\n\t\t\treturn __ctrl;\n\t\t}\n\n");
			init_funcs.Append (old_function);
			control_id = (string) openedControlTags.Pop ();
			current_function.AppendFormat ("\t\t\tthis.__BuildControl{0} ();\n\t\t\t__parser." +
							"AddParsedSubObject (this.{0});\n\n", control_id);
			old_function = (StringBuilder) functions.Pop ();
			current_function = (StringBuilder) functions.Peek ();
		}
		current_function.Append ("\t\t}\n\n");
		init_funcs.Append (current_function);
		functions.Pop ();
	}

	private void error (string msg)
	{
		Console.WriteLine ("Error: " + msg);
	}
}
}

