using System;
using System.Collections.Generic;

namespace Mono.WebServer.FastCgi {
	public static class DictionaryExtensions {
		/// <summary>
		/// Adds the key/value pair or replaces the value if the key is present.
		/// </summary>
		/// <returns><c>true</c>, if the key was added, <c>false</c> otherwise.
		/// </returns>
		public static bool AddOrReplace<TKey, TValue> (
		                              this IDictionary<TKey, TValue> dictionary,
		                              TKey key, TValue value)
		{
			if (dictionary.ContainsKey (key)) {
				dictionary [key] = value;
				return false;
			}
			dictionary.Add (key, value);
			return true;
		}
	}
}

