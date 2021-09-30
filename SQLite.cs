using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.IO;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.Data.Common;
//using Microsoft.Office.Interop.Excel;
using System.Text.RegularExpressions;

namespace PERF_COUNTERS_CSHARP
{
	class Program
	{
		public static bool PrintResponce (string CommandText)
		{
			using (var m_dbConn = new SQLiteConnection("Data Source=" + GlobalConstant.DBNAME + ";Version=3;"))
			{
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
				foreach (DataTable table in ds.Tables)
				{
					Console.WriteLine(table.TableName);
					foreach (DataColumn column in table.Columns)
						Console.WriteLine(column.ColumnName);
					foreach (DataRow row in table.Rows)
					{
						var cells = row.ItemArray;
						foreach (var cell in cells)
						{
							Console.WriteLine(""+cell+" ");
						}
						Console.WriteLine();
					}
				}
			}
			
			return true;
		}
	
	
		public static void DBCheckAndCreate ()
		{
			if (!System.IO.File.Exists(GlobalConstant.DBNAME))
			{
				SQLiteConnection.CreateFile(GlobalConstant.DBNAME);
			}
			
			using (var SQLiteConn = new SQLiteConnection("Data Source=" + GlobalConstant.DBNAME + ";Version=3;"))
			{
				SQLiteConn.Open();
				var SQLiteCmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Counters(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counter_name TEXT, instance_name TEXT, value TEXT, category TEXT);",SQLiteConn);
				SQLiteCmd.ExecuteNonQuery();
				SQLiteCmd.CommandText = "CREATE TABLE IF NOT EXISTS Workflow(id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT, value TEXT);";
				SQLiteCmd.ExecuteNonQuery();
				SQLiteCmd.CommandText = "CREATE TABLE IF NOT EXISTS Correl(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counter_name TEXT, instance_name TEXT, Correl TEXT);";
				SQLiteCmd.ExecuteNonQuery();
				SQLiteCmd.CommandText = "CREATE TABLE IF NOT EXISTS StatAnalit(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counter_type TEXT, diff TEXT);";
				SQLiteCmd.ExecuteNonQuery();
			}
		}
	
		public static void Main(string[] args)
		{ 
			var pcc = new PerformanceCounterCategory("Memory");   
		   	var p1 = pcc.ReadCategory();
		   	Thread.Sleep(1000);
		   	var p2 = pcc.ReadCategory();
		   		
	   		foreach (var cnt in p1.Keys)
		  	{
		       string CounterName = cnt.ToString();
		       foreach (var inst in p1[CounterName].Keys)
		       {
		           string InstanceName = inst.ToString();
		           //if (InstanceName.Contains("_total") || InstanceName.Contains("systemdiagnosticsperfcounterlibsingleinstance"))
		           //{
			           if (p2[CounterName].Contains(InstanceName))
			           {
			           	Console.WriteLine(CounterName+" "+InstanceName+" "+ CounterSample.Calculate(p1[CounterName][InstanceName].Sample,p2[CounterName][InstanceName].Sample));
			           }
			           else
			           {
							Console.WriteLine(CounterName+" "+InstanceName+" "+ CounterSample.Calculate(p1[CounterName][InstanceName].Sample));
			           }
		           //}
	     		}	
   			}
		}
	
	}
}