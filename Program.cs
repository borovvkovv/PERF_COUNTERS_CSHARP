using System;

namespace PERF_COUNTERS_CSHARP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
























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
using Microsoft.Office.Interop.Excel;
using System.Text.RegularExpressions;


namespace PerfCounters
{
	class double_Point
	{
		public Single value {get; set;}
		public DateTime time {get; set;}
	}
	
	class Program
	{
		public static void ReadDataToConsole (string dbFileName, string CommandText)
		{
			SQLiteConnection m_dbConn;
			SQLiteCommand m_sqlCmd;
			
			m_sqlCmd = new SQLiteCommand();

			m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
			m_dbConn.Open();
			m_sqlCmd.Connection = m_dbConn;
			
			DataSet ds = new DataSet();
		   	m_sqlCmd.CommandText = @CommandText;
		   	m_sqlCmd.ExecuteNonQuery();
		   	SQLiteDataAdapter adapter = new SQLiteDataAdapter(m_sqlCmd.CommandText, m_dbConn);
		   	adapter.Fill(ds);
		   	
		   	foreach (System.Data.DataTable table in ds.Tables)
		   	{
		   		Console.WriteLine(table.TableName);
			   	foreach (DataColumn column in table.Columns)
			   		Console.WriteLine(column.ColumnName);
		   		foreach (DataRow row in table.Rows)
		   		{
		   			var cells = row.ItemArray;
		   			foreach (object cell in cells)
		   			{
		   				Console.WriteLine(""+cell+" ");
		   			}
		   			Console.WriteLine();
		   		}
		   	}
		}
		
		
		public static void CreateAndCheckSQLiteDB (string dbFileName)
		{
			System.Data.SQLite.SQLiteConnection SQLiteConn;
			System.Data.SQLite.SQLiteCommand SQLiteCmd;
			
			SQLiteCmd = new System.Data.SQLite.SQLiteCommand();
						
			if (!System.IO.File.Exists(dbFileName))
			{
				System.Data.SQLite.SQLiteConnection.CreateFile(dbFileName);
			}
			
			SQLiteConn = new System.Data.SQLite.SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
			SQLiteConn.Open();
			SQLiteCmd.Connection = SQLiteConn;
			SQLiteCmd.CommandText = "CREATE TABLE IF NOT EXISTS Counters(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counter_name TEXT, instance_name TEXT, value TEXT, category TEXT);";
			SQLiteCmd.ExecuteNonQuery();
			SQLiteCmd.CommandText = "CREATE TABLE IF NOT EXISTS Workflow(id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT, value TEXT);";
			SQLiteCmd.ExecuteNonQuery();
			SQLiteCmd.CommandText = "CREATE TABLE IF NOT EXISTS Correl(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counter_name TEXT, instance_name TEXT, Correl TEXT);";
			SQLiteCmd.ExecuteNonQuery();
			SQLiteCmd.CommandText = "CREATE TABLE IF NOT EXISTS StatAnalit(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counter_type TEXT, diff TEXT);";
			SQLiteCmd.ExecuteNonQuery();
			SQLiteConn.Close();
		}
	
	
		public static void Main078(string[] args)
		{
			
		}
	}
}
 
 