//
// asp-parser.cs: Parser for ASP.NET pages.
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
	using System.Text;

class AspParser {
	private AspTokenizer tokenizer;
	private ArrayList elements; // List of processed elements in the HTML page.

	private void error ()
	{
		Console.WriteLine ("Error: " + tokenizer.location);
		Environment.Exit (-1);
	}

	private void error (string msg)
	{
		Console.WriteLine ("Error: "+ msg + "\n" + tokenizer.location);
		Environment.Exit (-1);
	}

	public AspParser (AspTokenizer tokenizer)
	{
		this.tokenizer = tokenizer;
		elements = new ArrayList ();
	}

	public AspParser (string filename, Stream input) : 
		this (new AspTokenizer (filename, input)) {}

	public ArrayList Elements
	{
		get { return elements; }
	}

	private bool eat (int expected_token)
	{
		if (tokenizer.get_token () != expected_token){
			tokenizer.put_back ();
			return false;
		}
		return true;
	}

	private void AddPlainText (string newText)
	{
		if (elements.Count > 0){
			Element element = (Element) elements [elements.Count - 1];
			if (element is PlainText){
				((PlainText) element).Append (newText);
				return;
			}
		}
		elements.Add (new PlainText (newText));
	}
	
	public void parse ()
	{
		int token;
		Element element;
		Tag tag_element;
		string tag = "";

		while ((token = tokenizer.get_token ()) != Token.EOF){
			if (tokenizer.Verbatim){
				string end_verbatim = "</" + tag + ">";
				string verbatim_text = get_verbatim (token, end_verbatim);

				if (verbatim_text == null)
					error ("Unexpected EOF processing " + tag);

				AddPlainText (verbatim_text);
				elements.Add (new CloseTag (tag));
				tokenizer.Verbatim = false;
			}
			else if (token == '<'){
				element = get_tag ();
				if (element == null)
					error ();
				elements.Add (element);
				if (!(element is Tag)){
					AddPlainText (((PlainText) element).Text);
					continue;
				}

				tag_element = element as Tag;
				tag = tag_element.TagID.ToUpper ();
				if (!tag_element.SelfClosing && (tag == "SCRIPT" || tag == "PRE"))
					tokenizer.Verbatim = true;
			}
			else {
				StringBuilder text =  new StringBuilder ();
				do {
					text.Append (tokenizer.value);
					token = tokenizer.get_token ();
				} while (token != '<' && token != Token.EOF);
				tokenizer.put_back ();
				AddPlainText (text.ToString ());
			}
		}
	}

	private Element get_tag ()
	{
		int token = tokenizer.get_token ();
		string id;
		TagAttributes attributes;

		switch (token){
		case '%':
			if (eat ('@')){
				id = (eat (Token.DIRECTIVE) ? tokenizer.value : "Page");
				attributes = get_attributes ();
				if (!eat ('%') || !eat ('>'))
					error ("expecting '%>'");

				return new Directive (id, attributes);
			}

			bool varname = eat ('=');
			bool databinding = !varname && eat ('#');
			tokenizer.Verbatim = true;
			string inside_tags = get_verbatim (tokenizer.get_token (), "%>");
			tokenizer.Verbatim = false;
			if (databinding)
				return new DataBindingTag (inside_tags);
			return new CodeRenderTag (varname, inside_tags);
		case '/':
			if (!eat (Token.IDENTIFIER))
				error ("expecting TAGNAME");
			id = tokenizer.value;
			if (!eat ('>'))
				error ("expecting '>'");
			return new CloseTag (id);
		case '!':
			if (eat (Token.DOUBLEDASH)){
				tokenizer.Verbatim = true;
				string comment = get_verbatim ('-', "-->");
				tokenizer.Verbatim = false;
				if (comment == null)
					error ("Unfinished HTML comment");

				return new PlainText ("<!-" + comment + "-->");
			} else {
				//FIXME
				//  <!DOCTYPE...
			}
			return null;
		case Token.IDENTIFIER:
			id = tokenizer.value;
			Tag tag = new Tag (id, get_attributes (), eat ('/'));
			if (!eat ('>'))
				error ("expecting '>'");
			return tag;
		default:
			return null;
		}
	}

	private TagAttributes get_attributes ()
	{
		int token;
		TagAttributes attributes;
		string id;

		attributes = new TagAttributes ();
		while ((token = tokenizer.get_token ())  != Token.EOF){
			if (token != Token.IDENTIFIER)
				break;
			id = tokenizer.value;
			if (eat ('=')){
				if (eat (Token.ATTVALUE)){
					attributes.Add (id, tokenizer.value);
				} else {
					//TODO: support data binding syntax without quotes
					error ("expected ATTVALUE");
					return null;
				}
				
			} else {
				attributes.Add (id, null);
			}
		}

		tokenizer.put_back ();
		if (attributes.Count == 0)
			return null;

		return attributes;
	}

	private string get_verbatim (int token, string end)
	{
		StringBuilder vb_text = new StringBuilder ();
		int i = 0;

		if (tokenizer.value.Length > 1){
			// May be we have a put_back token that is not a single character
			vb_text.Append (tokenizer.value);
			token = tokenizer.get_token ();
		}

		while (token != Token.EOF){
			if (Char.ToUpper ((char) token) == end [i]){
				if (++i >= end.Length)
					break;
				token = tokenizer.get_token ();
				continue;
			}
			else {
				for (int j = 0; j < i; j++)
					vb_text.Append (end [j]);
			}

			i = 0;
			vb_text.Append ((char) token);
			token = tokenizer.get_token ();
		} 

		if (token == Token.EOF)
			return null;

		return vb_text.ToString ();
	}
}

}

