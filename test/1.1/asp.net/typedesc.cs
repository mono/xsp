//
// typedsc_property.cs: control with a property whose Type has a Typeconverter
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Licensed under the terms of the MIT X11 license
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Mono.Controls
{
	[ParseChildren(false)]
	public class WeirdControl : Label
	{
		public MyObject WeirdObject {
			get {
				object o = ViewState ["WeirdObject"];
				if (o == null)
					return null;
				return (MyObject) o;
			}
			set {
				ViewState ["WeirdObject"] = value;
				base.Text = value.Text;
			}
		}
	}

	[TypeConverter (typeof (MyObjectConverter))]
	public class MyObject
	{
		string text;

		public MyObject (string text)
		{
			this.text = text;
		}

		public string Text {
			get { return text; }
		}
	}

	public class MyObjectConverter : TypeConverter
	{
		public MyObjectConverter () {}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return (sourceType == typeof (string));
		}

		public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (!(value is string))
				return base.ConvertFrom (context, culture, value);

			return new MyObject ((string) value);
		}
	}
}

