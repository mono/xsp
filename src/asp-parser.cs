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
	}

	private void error (string msg)
	{
		Console.WriteLine ("Error: "+ msg + "\n" + tokenizer.location);
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

	public void parse ()
	{
		int token;
		Tag tag_element;
		PlainText text;
		string tag = "";

		while ((token = tokenizer.get_token ()) != Token.EOF){
			if (tokenizer.Verbatim){
				StringBuilder vb_text = new StringBuilder ();
				string end_verbatim = "</" + tag.ToUpper () + ">";
				int i = 0;

				while (token != Token.EOF){
					if (Char.ToUpper ((char) token) == end_verbatim [i]){
						i++;
						if (i >= end_verbatim.Length){
							elements.Add (new PlainText (vb_text));
							elements.Add (new CloseTag (tag));
							tokenizer.Verbatim = false;
							break;
						}
						token = tokenizer.get_token ();
						continue;
					} else {
						for (int j = 0; j < i; j++)
							vb_text.Append (end_verbatim [j]);
					}

					i = 0;
					vb_text.Append ((char) token);
					token = tokenizer.get_token ();
				} 

				if (token == Token.EOF)
					throw new ApplicationException ("Unexpeted EOF processing " + tag);
			} else if (token == '<'){
				tag_element = get_tag ();
				//TODO: nulls?
				if (tag_element != null)
					elements.Add (tag_element);

				tag = tag_element.TagID.ToUpper ();
				if (!tag_element.SelfClosing && 
				    (tag.ToUpper () == "SCRIPT" || tag.ToUpper () == "PRE"))
					tokenizer.Verbatim = true;
			} else {
				text = new PlainText ();
				do {
					text.Append (tokenizer.value);
					token = tokenizer.get_token ();
				} while (token != '<' && token != Token.EOF);
				tokenizer.put_back ();
				elements.Add (text);
			}
		}
	}

	private Tag get_tag ()
	{
		int token = tokenizer.get_token ();
		string id;
		TagAttributes attributes;

		switch (token){
			case '%':
				if (eat ('@')){
					if (eat (Token.DIRECTIVE))
						id = tokenizer.value;
					else
						id = "Page";

					attributes = get_attributes ();
					if (!eat ('%')){
						error ("expecting '%'");
						return null;
					}
					if (!eat ('>')){
						error ("expecting '>'");
						return null;
					}
					return new Directive (id, attributes);
				}

				if (eat ('=') || eat ('#')){
					//FIXME: get var name
					return null;
				}

				//FIXME: Code to insert directly
				return null;
			case '/':
				if (!eat (Token.IDENTIFIER)){
					error ("expecting TAGNAME");
					return null;
				}
				id = tokenizer.value;
				if (!eat ('>')){
					error ("expecting '>'");
					return null;
				}
				return new CloseTag (id);
			case '!':
				//FIXME
				if (eat ('-')){
					// Comment
				} else {
					//  <!DOCTYPE...
				}
				return null;
			case Token.IDENTIFIER:
				id = tokenizer.value;
				Tag tag = new Tag (id, get_attributes (), eat ('/'));
				if (!eat ('>')){
					error ("expecting '>'");
					return null;
				}
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
}

}

