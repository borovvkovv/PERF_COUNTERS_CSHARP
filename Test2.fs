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
		public static void DBCheckAndCreate (string dbFileName)
		{	
			if (!System.IO.File.Exists(dbFileName))
			{
				SQLiteConnection.CreateFile(dbFileName);
			}
			
			using (var SQLiteConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;"))
			{
				SQLiteConn.Open();
				var SQLiteCmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Counters(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counter_name TEXT, instance_name TEXT, value TEXT, category TEXT);",SQLiteConn);
				SQLiteCmd.ExecuteNonQuery();
			}
		}
		
		public static void Main(string[] args)
		{
			DBCheckAndCreate("1.sqlite");
			
		}
	}
}
 
 