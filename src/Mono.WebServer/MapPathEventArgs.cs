//
// Mono.WebServer.MapPathEventArgs
//
// Authors:
//	Daniel Lopez Ridruejo
// 	Gonzalo Paniagua Javier
//
// Documentation:
//	Brian Nickel
//
// Copyright (c) 2002 Daniel Lopez Ridruejo.
//           (c) 2002,2003 Ximian, Inc.
//           All rights reserved.
// (C) Copyright 2004-2010 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace Mono.WebServer
{
	/// <summary>
	///    This class extends <see cref="EventArgs" /> to provide arguments
	///    for <see cref="MapPathEventHandler" />.
	/// </summary>
	/// <remarks>
	///    When <see cref="MonoWorkerRequest.MapPathEvent" /> is called, the
	///    handler has an option of setting <see
	///    cref="MapPathEventArgs.MappedPath" /> to a mapped path.
	/// </remarks>
	public class MapPathEventArgs : EventArgs
	{
		/// <summary>
		///    Contains the virtual path, as used in the request.
		/// </summary>
		string path;
		
		/// <summary>
		///    Contains the physical "mapped" path.
		/// </summary>
		string mapped;
		
		/// <summary>
		///    Indicates whether or not the path has been mapped.
		/// </summary>
		bool isMapped;
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="MapPathEventArgs" /> for a specified virtual path.
		/// </summary>
		/// <param name="path">
		///    A <see cref="string" /> containing the virtual path, as
		///    contained in the request.
		/// </param>
		public MapPathEventArgs (string path)
		{
			this.path = path;
			isMapped = false;
		}

		/// <summary>
		///    Gets the virtual path of the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the virtual path of
		///    the current instance.
		/// </value>
		public string Path {
			get { return path; }
		}
		
		/// <summary>
		///    Gets whether or not the path is mapped.
		/// </summary>
		/// <value>
		///    A <see cref="bool" /> indicating whether or not the path
		///    has been mapped.
		/// </value>
		public bool IsMapped {
			get { return isMapped; }
		}

		/// <summary>
		///    Gets and sets the physical "mapped" path for the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> containing the physical "mapped"
		///    path of the current instance.
		/// </value>
		public string MappedPath {
			get { return mapped; }
			set {
				mapped = value;
				isMapped = (value != null && value != "");
			}
		}
	}
}
