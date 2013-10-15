//
// ArraySegment.cs
//
// Authors:
//  Ben Maurer (bmaurer@ximian.com)
//  Jensen Somers <jensen.somers@gmail.com>
//  Marek Safar (marek.safar@gmail.com)
//
// Copyright (C) 2004 Novell
// Copyright (C) 2012 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections;
using System.Collections.Generic;

namespace Mono.WebServer.FastCgi.Compatibility
{
	[Serializable]
	public struct CompatArraySegment<T>
		: IReadOnlyList<T>
	{
		readonly T [] array;
		readonly int offset, count;

		public T [] Array {
			get { return array; }
		}

		public int Offset {
			get { return offset; }
		}

		public int Count {
			get { return count; }
		}

		T IReadOnlyList<T>.this[int index] {
			get {
				return this [index];
			}
		}

		public T this[int index] {
			get {
				if (index < 0 || count <= index)
					throw new ArgumentOutOfRangeException ("index");

				return array[offset + index];
			}
			set {
				if (index < 0 || count <= index)
					throw new ArgumentOutOfRangeException ("index");

				array[offset + index] = value;
			}
		}

		public CompatArraySegment (T [] array, int offset, int count)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (offset < 0)
				throw new ArgumentOutOfRangeException ("offset", "Non-negative number required.");

			if (count < 0)
				throw new ArgumentOutOfRangeException ("count", "Non-negative number required.");

			if (offset > array.Length)
				throw new ArgumentException ("out of bounds");

			// now offset is valid, or just beyond the end.
			// Check count -- do it this way to avoid overflow on 'offset + count'
			if (array.Length - offset < count)
				throw new ArgumentException ("out of bounds", "offset");

			this.array = array;
			this.offset = offset;
			this.count = count;
		}

		public CompatArraySegment (T [] array, int offset) : this (array, offset, array.Length - offset)
		{
		}

		public CompatArraySegment (T [] array)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			this.array = array;
			offset = 0;
			count = array.Length;
		}

		public CompatArraySegment<T> Trimmed (int size)
		{
			return new CompatArraySegment<T> (array, offset, size);
		}

		public override bool Equals (Object obj)
		{
			return obj is ArraySegment<T> && Equals ((ArraySegment<T>) obj);
		}

		public bool Equals (ArraySegment<T> obj)
		{
			return array == obj.Array && offset == obj.Offset && count == obj.Count;
		}

		public override int GetHashCode ()
		{
			// TODO: fix this
			return array.GetHashCode () ^ offset ^ count;
		}

		public static bool operator ==(CompatArraySegment<T> a, CompatArraySegment<T> b)
		{
			return a.Equals (b);
		}

		public static bool operator !=(CompatArraySegment<T> a, CompatArraySegment<T> b)
		{
			return !a.Equals (b);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			for (int i = 0; i < count; ++i)
				yield return array[offset + i];
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable<T>) this).GetEnumerator ();
		}

		public void CopyTo(T[] dest, int destIndex, int count)
		{
			if (dest == null)
				throw new ArgumentNullException ("dest");

			System.Array.Copy(array, offset, dest, destIndex, count);
		}
	}
}
