using System;
using System.Collections.Generic;

namespace Mono.WebServer.FastCgi.Compatibility
{
	public static class ArrayExtensions
	{
		public static IReadOnlyList<T> ToReadOnlyList<T> (this T[] array)
		{
			// TODO: don't use a segment for this
			return new CompatArraySegment<T> (array);
		}
	}
}
