using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.IO;
using System.Data.SqlClient;
using System.Data.Common;

namespace PERF_COUNTERS_CSHARP
{
	class Program
	{	
		/*public static int FindCount (string name, string instance, DataSet DS) // date, server, name, instance, value
		{
			Console.WriteLine(DS.Tables[0].TableName);
	   		foreach (DataRow row in table.Rows)
	   		{
	   			var cells = row.ItemArray;
   				if (cells[0].ToString() == name && cells[1].ToString() == instance)
				    {
   					return Convert.ToInt32(cells[2]);
				    }
	   			Console.WriteLine();
	   		}		
		}	*/
		
		
		public static DataSet ReadDataToDS (string CommandText)
		{
			

			var dbConnect = new SQLiteConnection("Data Source=" + GlobalConstant.DBNAME + ";Version=3;");
			try
			{
				dbConnect.Open();
			}
			catch (SQLiteException ex)
			{
				Console.WriteLine("Error: "+ex.Message);
			}
			var sqlCmd = new SQLiteCommand(@CommandText,dbConnect);
		   	sqlCmd.ExecuteNonQuery();
			var DS = new DataSet();
		   	var adapter = new SQLiteDataAdapter(sqlCmd.CommandText, dbConnect);
		   	adapter.Fill(DS);
		   	return DS;
		}
				
		public static void PerfFormattedData ()
		{
			var DS = ReadDataToDS(@"SELECT * FROM Workflow");
			
			var FormattedDS_2 = new List<List<string>>();
			
		   		foreach (var row in DS.Tables[0].Rows)
		   		{
		   			var cells = row.ItemArray;
		   			
		   			DateTime wfDateTime = Convert.ToDateTime(cells[1]);	//время выборки очереди WF
		   			DateTime RangeForWFDateTime = wfDateTime.AddMinutes(4);
		   			//Console.WriteLine(wfDateTime);
		   			//Console.WriteLine(RangeForWFDateTime);
		   			var DS_2 = ReadDataToDS("Sample.sqlite", "SELECT * FROM Counters WHERE server='sng-drmdb-sql' AND Date>='"+wfDateTime+"' AND Date<='"+RangeForWFDateTime+"'");
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
		   						var A2 = FindCount(wfDateTime, RangeForWFDateTime, ListDS[0][3].ToString(), ListDS[0][4].ToString(), 2)[4];
		   						var a1 = FindCount(wfDateTime, RangeForWFDateTime, ListDS[0][3].ToString(), ListDS[0][4].ToString(), 1)[4];
		   						var B2 = FindCount(wfDateTime, RangeForWFDateTime, ListDS[0][3].ToString().Replace("/sec","Base").Replace("(ms)","Base"), ListDS[0][4].ToString(), 2)[4];
		   						var b1 = FindCount(wfDateTime, RangeForWFDateTime, ListDS[0][3].ToString().Replace("/sec","Base").Replace("(ms)","Base"), ListDS[0][4].ToString(), 1)[4];
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