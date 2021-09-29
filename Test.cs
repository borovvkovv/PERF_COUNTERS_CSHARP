using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.IO;
using System.Data.SqlClient;
using System.Data.Common;

namespace Test
{
	class Program
	{	
		public static bool InsertSQLCounters(string Server, string DB, string dbFileName)
		{
			var FormattedDS = new List<List<string>>();
			System.DateTime StartDateTime;
			System.DateTime EndDateTime;
			
			#region Выборка счетчиков из SQL, помещение их в DataSet'ы DSFirst, DSSecond
			
			var SQLConn = new System.Data.SqlClient.SqlConnection(@"Data Source="+Server+";Initial Catalog="+DB+";Integrated Security=SSPI;");
			try
			{
				SQLConn.Open();
			}
			catch (System.Data.SqlClient.SqlException ex)
			{
				Console.WriteLine("Error: "+ex.Message);
				return false;
			}
			var SQLCmd = new System.Data.SqlClient.SqlCommand(@"SELECT [counter_name],[instance_name],[cntr_value],[cntr_type] FROM [sqldiag].[sys].[dm_os_performance_counters]",SQLConn);
		   	StartDateTime = DateTime.Now;
		   	SQLCmd.ExecuteNonQuery();
		   	
		   	var DSFirst = new DataSet();
		   	var AdapterFirst = new System.Data.SqlClient.SqlDataAdapter(SQLCmd.CommandText, SQLConn);
		   	AdapterFirst.Fill(DSFirst);
		   	
		   	System.Threading.Thread.Sleep(1000);
		   	SQLCmd.ExecuteNonQuery();
		   	EndDateTime = DateTime.Now;
		   	
		   	var DSSecond = new DataSet();
		   	var AdapterSecond = new System.Data.SqlClient.SqlDataAdapter(SQLCmd.CommandText, SQLConn);
		   	AdapterSecond.Fill(DSSecond);
		   	SQLConn.Close();
		   	#endregion
		  
			foreach (var row in DSSecond.Tables[0].Rows)
	   		{
	   			var cells = row.ItemArray;
	   			switch (cells[3].ToString())
	   				{
	   					case "65792":
	   						FormattedDS.Add(new List<string> {EndDateTime.ToString(),Server, cells[0].ToString(), cells[1].ToString(), cells[2].ToString()});
	   						break;
	   					case "272696320":
	   						FormattedDS.Add(new List<string> {EndDateTime.ToString(),Server, cells[0].ToString(), cells[1].ToString(), cells[2].ToString()});
	   						break;
	   					case "1073874176":
	   						
	   						break;
	   					case "272696576":
	   						break;
	   					case "1073939712":
	   						break;
	   					case "537003264":
	   						break;
	   					default:
	   						Console.WriteLine("Error!!! Unknown SQL counter type");
	   						break;
	   				}
	   		}
		   	Console.WriteLine("konec");
			return true;
		}

		
		/*public static int FindCount (string name, string inst, DataSet DS) // date, server, name, inst, value
		{
			Console.WriteLine(DS.Tables[0].TableName);
	   		foreach (DataRow row in table.Rows)
	   		{
	   			var cells = row.ItemArray;
   				if (cells[0].ToString() == name && cells[1].ToString() == inst)
				    {
   					return Convert.ToInt32(cells[2]);
				    }
	   			Console.WriteLine();
	   		}		
		}	*/
		
		
		public static DataSet ReadDataToDS (string dbFileName, string CommandText)
		{
			

			var m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
			try
			{
				m_dbConn.Open();
			}
			catch (SQLiteException ex)
			{
				Console.WriteLine("Error: "+ex.Message);
			}
			var m_sqlCmd = new SQLiteCommand(@CommandText,m_dbConn);
		   	m_sqlCmd.ExecuteNonQuery();
			var ds = new DataSet();
		   	var adapter = new SQLiteDataAdapter(m_sqlCmd.CommandText, m_dbConn);
		   	adapter.Fill(ds);
		   	return ds;
		}
		
		public static bool ReadDataToConsole (string dbFileName, string CommandText)
		{
			var m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
			try
			{
				m_dbConn.Open();
			}
			catch (SQLiteException ex)
			{
				Console.WriteLine("Error: "+ex.Message);
				return false;
			}
			var m_sqlCmd = new SQLiteCommand(@CommandText,m_dbConn);
		   	m_sqlCmd.ExecuteNonQuery();
			var ds = new DataSet();
		   	var adapter = new SQLiteDataAdapter(m_sqlCmd.CommandText, m_dbConn);
		   	adapter.Fill(ds);
		   	
		   	foreach (var table in ds.Tables)
		   	{
		   		Console.WriteLine(table.TableName);
			   	foreach (var column in table.Columns)
			   		Console.WriteLine(column.ColumnName);
		   		foreach (var row in table.Rows)
		   		{
		   			var cells = row.ItemArray;
		   			foreach (var cell in cells)
		   			{
		   				Console.WriteLine(""+cell+" ");
		   			}
		   			Console.WriteLine();
		   		}
		   	}
			return true;
		}
		
		public static void PerfFormattedData (string dbFileName)
		{
			var DS = ReadDataToDS("Sample.sqlite",@"SELECT * FROM Workflow");
			
			var FormattedDS_2 = new List<List<string>>();
			
		   		foreach (var row in DS.Tables[0].Rows)
		   		{
		   			var cells = row.ItemArray;
		   			
		   			DateTime WFDateTime = Convert.ToDateTime(cells[1]);	//время выборки очереди WF
		   			DateTime RangeForWFDateTime = WFDateTime.AddMinutes(4);
		   			//Console.WriteLine(WFDateTime);
		   			//Console.WriteLine(RangeForWFDateTime);
		   			var DS_2 = ReadDataToDS("Sample.sqlite", "SELECT * FROM Counters WHERE Server='sng-drmdb-sql' AND Date>='"+WFDateTime+"' AND Date<='"+RangeForWFDateTime+"'");
		   			List<List<string>> ListDS = new List<List<string>>();
		   			foreach (var row2 in DS_2.Tables[0].Rows)
		   			{
		   				var cells_2 = row2.ItemArray;
		   				ListDS.Add(new List<string>{cells_2[1].ToString(),cells_2[2].ToString(),cells_2[3].ToString().Trim(),cells_2[4].ToString().Trim(),cells_2[5].ToString(),cells_2[7].ToString()});
		   			}
		   			
		   			foreach (var item in ListDS)
		   			{
		   				switch (item[5].ToString())
		   				{
		   					case "65792":
		   						FormattedDS_2.Add(new List<string> {item[0],item[1],item[2],item[3],item[4]});
		   						break;
		   					case "272696320":
		   						FormattedDS_2.Add(new List<string> {item[0],item[1],item[2],item[3],item[4]});
		   						break;
		   					case "1073874176":
		   						/*if (IsItFirstSelection())
		   						var A2 = FindCount(WFDateTime, RangeForWFDateTime, ListDS[0][3].ToString(), ListDS[0][4].ToString(), 2)[4];
		   						var A1 = FindCount(WFDateTime, RangeForWFDateTime, ListDS[0][3].ToString(), ListDS[0][4].ToString(), 1)[4];
		   						var B2 = FindCount(WFDateTime, RangeForWFDateTime, ListDS[0][3].ToString().Replace("/sec","Base").Replace("(ms)","Base"), ListDS[0][4].ToString(), 2)[4];
		   						var B1 = FindCount(WFDateTime, RangeForWFDateTime, ListDS[0][3].ToString().Replace("/sec","Base").Replace("(ms)","Base"), ListDS[0][4].ToString(), 1)[4];
		   						FormattedDS_2.Add(new List<string> {ListDS[0][1].ToString(),ListDS[0][2].ToString(),ListDS[0][3].ToString(),ListDS[0][4].ToString(),});*/
		   						break;
		   					case "272696576":
		   						break;
		   					case "1073939712":
		   						break;
		   					case "537003264":
		   						break;
		   					default:
		   						Console.WriteLine("Error!!! Unknown SQL counter type");
		   						break;
		   				}
		   			}
		   		//ReadListToConsole(FormattedDS_2);
		   		
				}
		}
		
		public static void Main7(string[] args)
		{
			PerfFormattedData("Sample.sqlite");
			
			Console.ReadKey(true);
		}
	}
}