#region Copyright (c) 2002, 2003 Brian Knowles, Jim Shore
/********************************************************************************************************************
'
' Copyright (c) 2002, Brian Knowles, Jim Shore
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
'*******************************************************************************************************************/
#endregion

using System;
using System.Xml;
using NUnit.Framework;
using NUnit.Extensions.Asp.HtmlTester;

namespace NUnit.Extensions.Asp.AspTester
{
	/// <summary>
	/// Tester for System.Web.UI.WebControls.DataGrid
	/// </summary>
	public class DataGridTester : AspControlTester
	{
		/// <summary>
		/// Create the tester and link it to an ASP.NET control.
		/// </summary>
		/// <param name="aspId">The ID of the control to link to.</param>
		/// <param name="container">The control that contains the control to link to</param>
		public DataGridTester(string aspId, Tester container) : base(aspId, container)
		{
		}

		/// <summary>
		/// The number of rows in the data grid, not counting the header.
		/// </summary>
		public int RowCount 
		{
			get
			{
				return Element.GetElementsByTagName("tr").Count - 1;
			}
		}

		/// <summary>
		/// An array of string arrays containing the contents of the data grid, 
		/// not counting the header.  The outer array represents rows and the inner arrays
		/// represents cells within the rows.  Whitespace has been trimmed from the front and
		/// back of the cells.
		/// </summary>
		public string[][] TrimmedCells
		{
			get
			{
				string[][] result = new string[RowCount][];
				for (int i = 0; i < RowCount; i++)
				{
					result[i] = GetRow(i).TrimmedCells;
				}
				return result;
			}
		}

		/// <summary>
		/// The data grid's header row.  The first row is always assumed to be the header row.
		/// </summary>
		public Row GetHeaderRow()
		{
			return new Row(0, this);
		}

		/// <summary>
		/// Returns a row from the data grid.  Row number zero is the first row <b>after</b>
		/// the header row.
		/// </summary>
		public Row GetRow(int rowNumber)
		{
			return new Row(rowNumber + 1, this);
		}

		/// <summary>
		/// Returns a row containing a specific cell.
		/// </summary>
		/// <param name="columnNumber">The column containing the cell to look for (zero-based).</param>
		/// <param name="trimmedValue">The cell to look for.</param>
		public Row GetRowByCellValue(int columnNumber, string trimmedValue)
		{
			string[][] cells = TrimmedCells;
			for (int i = 0; i < cells.GetLength(0); i++)
			{
				if (cells[i][columnNumber] == trimmedValue) return GetRow(i);
			}
			Assertion.Fail(string.Format("Expected to find a row with '{0}' in column {1} of {2}", trimmedValue, columnNumber, HtmlIdAndDescription));
			throw new ApplicationException("This line cannot execute.  Fail() throws an exception.");
		}

		/// <summary>
		/// Click a column heading link that was generated with the "allowSorting='true'" attribute.
		/// </summary>
		/// <param name="columnNumberZeroBased">The column to sort (zero-based)</param>
		public void Sort(int columnNumberZeroBased)
		{
			Row header = GetHeaderRow();
			XmlElement element = header.GetCellElement(columnNumberZeroBased);
			XmlNodeList links = element.GetElementsByTagName("a");
			Assertion.Assert("Attempted to sort non-sortable grid (" + HtmlIdAndDescription + ")", links.Count != 0);
			Assertion.Assert("Expect sort link to have exactly one anchor tag", links.Count == 1);

			XmlElement link = (XmlElement)links[0];
			PostBack(link.GetAttribute("href"));
		}

		private XmlElement GetRowElement(int rowNumber)
		{
			XmlNodeList rows = Element.GetElementsByTagName("tr");
			return (XmlElement)rows[rowNumber];
		}

        protected internal override string GetChildElementHtmlId(string aspId)
		{
			try 
			{
				int rowNumber = int.Parse(aspId);
				return HtmlId + "__ctl" + (rowNumber + 1);
			}
			catch (FormatException) 
			{
				throw new ContainerMustBeRowException(aspId, this);
			}
		}
		
		/// <summary>
		/// Tests a row within a data grid.
		/// </summary>
		public class Row : AspControlTester
		{
			private int rowNumber;
			private DataGridTester container;

			/// <summary>
			/// Create the tester and link it to a row in a specific data grid.
			/// </summary>
			/// <param name="rowNumberWhereZeroIsHeader">The row to test.</param>
			/// <param name="container">The data grid that contains the row.</param>
			public Row(int rowNumberWhereZeroIsHeader, DataGridTester container) : base(rowNumberWhereZeroIsHeader.ToString(), container)
			{
				this.rowNumber = rowNumberWhereZeroIsHeader;
				this.container = container;
			}

			protected internal override string GetChildElementHtmlId(string inAspId)
			{
				return HtmlId + "_" + inAspId;
			}

			protected internal override XmlElement Element
			{
				get
				{
					return container.GetRowElement(rowNumber);
				}
			}

			/// <summary>
			/// The cells in the row.  Whitespace has been trimmed from the front and back
			/// of the cells.
			/// </summary>
			public string[] TrimmedCells
			{
				get
				{
					XmlNodeList cells = Element.GetElementsByTagName("td");
					string[] cellText = new string[cells.Count];
					for (int i = 0; i < cells.Count; i++) 
					{
						XmlElement cell = (XmlElement)cells[i];
						cellText[i] = cell.InnerText.Trim();
					}
					return cellText;
				}
			}

			internal XmlElement GetCellElement(int columnNumberZeroBased)
			{
				XmlNodeList cells = Element.GetElementsByTagName("td");
				Assertion.Assert("There is no column #" + columnNumberZeroBased + " in " + HtmlIdAndDescription, columnNumberZeroBased >= 0 && columnNumberZeroBased < cells.Count);
				return (XmlElement)cells[columnNumberZeroBased];
			}
		}
	}

	/// <summary>
	/// The container of the control being tested was a DataGridTester, but it should be a 
	/// Row.  Change "new MyTester("foo", datagrid)" to 
	/// "new MyTester("foo", datagrid.getRow(rowNum))".
	/// </summary>
	public class ContainerMustBeRowException : ApplicationException 
	{
		internal ContainerMustBeRowException(string aspId, DataGridTester dataGrid) 
			: base(GetMessage(aspId, dataGrid))
		{
		}

		private static string GetMessage(string aspId, DataGridTester dataGrid) 
		{
			return String.Format(
				"Tester '{0}' has DataGridTester '{1}' as its container. That isn't allowed. "
				+ "It should be a DataGridTester.Row. When constructing {0}, pass '{1}.getRow(#)' "
				+ "as the container argument.",
				aspId, dataGrid.AspId);
		}
	}
}
