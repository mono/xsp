#region Copyright (c) 2002, 2003 by Brian Knowles and Jim Shore
/********************************************************************************************************************
'
' Copyright (c) 2002, 2003 by Brian Knowles and Jim Shore
'
' Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
' documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
' the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and
' to permit persons to whom the Software is furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in all copies or substantial portions 
' of the Software.
'
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
' THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
'
'******************************************************************************************************************/
#endregion

using System;
using NUnit.Framework;

namespace NUnit.Extensions.Asp.Test
{
	public class WebAssertionTest : NUnitAspTestCase
	{
		public void TestAssertSortOrder_WhenSorted()
		{
			string[][] testData = new string[][]
			{
				new string[] {"1"},
				new string[] {"2"},
				new string[] {"3"},
				new string[] {"4"}
			};
			AssertSortOrder("testData", testData, 0, true, DataType.String);
		}

		public void TestAssertSortOrder_WhenSortedAndStartingWithEmptyString()
		{
			string[][] testData = new string[][]
			{
				new string[] {""},
				new string[] {"2"},
				new string[] {"3"},
				new string[] {"4"}
			};
			AssertSortOrder("testData", testData, 0, true, DataType.String);
		}
		
		public void TestAssertSortOrder_WhenSortedAndManyRepeatingValues()
		{
			string[][] testData = new string[][]
			{
				new string[] {""},
				new string[] {"2"},
				new string[] {"2"},
				new string[] {"2"},
				new string[] {"3"},
				new string[] {"4"}
			};
			AssertSortOrder("testData", testData, 0, true, DataType.String);
		}

		public void TestAssertSortOrder_WhenSortedDescending()
		{
			string[][] testData = new string[][]
			{
				new string[] {"4"},
				new string[] {"3"},
				new string[] {"2"},
				new string[] {"1"},
			};
			AssertSortOrder("testData", testData, 0, false, DataType.String);
		}

		public void TestAssertSortOrder_WhenSortedDescendingAndEndingWithEmptyString()
		{
			string[][] testData = new string[][]
			{
				new string[] {"4"},
				new string[] {"3"},
				new string[] {"2"},
				new string[] {""},
			};
			AssertSortOrder("testData", testData, 0, false, DataType.String);
		}

		public void TestAssertSortOrder_WhenSortedDescendingAndManyRepeatValues()
		{
			string[][] testData = new string[][]
			{
				new string[] {"4"},
				new string[] {"3"},
				new string[] {"3"},
				new string[] {"3"},
				new string[] {"2"},
				new string[] {"1"},
			};
			AssertSortOrder("testData", testData, 0, false, DataType.String);
		}

		public void TestAssertSortOrder_WhenSortingOnLastColumn()
		{
			string[][] testData = new string[][]
			{
				new string[] {"1", "4"},
				new string[] {"3", "3"},
				new string[] {"2", "2"},
				new string[] {"4", "1"},
			};
			AssertSortOrder("testData", testData, 1, false, DataType.String);
		}

		public void TestAssertSortOrder_WhenNotSorted()
		{
			string[][] testData = new string[][]
			{
				new string[] {"2"},
				new string[] {"1"},
				new string[] {"3"},
				new string[] {"4"}
			};
			AssertSortOrderFails(testData, DataType.String);
		}

		public void TestAssertSortOrder_WhenNoData()
		{
			AssertSortOrder("no data", new string[][] {}, 0, true, DataType.String);
			AssertSortOrder("no data", new string[][] {}, 0, false, DataType.String);
		}

		public void TestAssertSortOrder_WhenNumber()
		{
			string[][] testData = new string[][]
			{
				new string[] {"9"},
				new string[] {"10"},
			};
			AssertSortOrder("testData", testData, 0, true, DataType.Int);
		}

		public void TestAssertSortOrder_WhenBlankNumber()
		{
			string[][] testData = new string[][]
			{
				new string[] {""},
				new string[] {"9"},
				new string[] {"10"},
			};
			AssertSortOrder("testData", testData, 0, true, DataType.Int);
		}

		public void TestAssertSortOrder_WhenBlankNumberAtEnd()
		{
			string[][] testData = new string[][]
			{
				new string[] {"9"},
				new string[] {"10"},
				new string[] {""},
			};
			AssertSortOrderFails(testData, DataType.Int);
		}

		public void TestAssertSortOrder_WhenDate()
		{
			string[][] testData = new string[][]
			{
				new string[] {"7/4/2002"},
				new string[] {"7/16/2002"},
			};
			AssertSortOrder("testData", testData, 0, true, DataType.DateTime);
		}

		public void TestAssertSortOrder_WhenBlankDate()
		{
			string[][] testData = new string[][]
			{
				new string[] {""},
				new string[] {"7/4/2002"},
				new string[] {"7/16/2002"},
			};
			AssertSortOrder("testData", testData, 0, true, DataType.DateTime);
		}

		public void TestAssertSortOrder_WhenBlankDateAtEnd()
		{
			string[][] testData = new string[][]
			{
				new string[] {"7/4/2002"},
				new string[] {"7/16/2002"},
				new string[] {""},
			};
			AssertSortOrderFails(testData, DataType.DateTime);
		}

		private void AssertSortOrderFails(string[][] testData, DataType dataType)
		{
			try
			{
				AssertSortOrder("testData", testData, 0, true, dataType);
			}
			catch (AssertionException)
			{
				return;
			}
			Fail("Expected assertion");
		}
	}
}
