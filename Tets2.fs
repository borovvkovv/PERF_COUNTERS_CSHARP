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


namespace PERF_COUNTERS_CSHARP3
{	
	class Program
	{				
		public static void CreateAndCheckSQLiteDB (string dbFileName)
		{	
			System.Data.SQLite.SQLiteConnection SQLiteConn = new System.Data.SQLite.SQLiteConnection();
			if (!System.IO.File.Exists(dbFileName))
			{
				SQLiteConn.CreateFile(dbFileName);
			}
			
			using (System.Data.SQLite.SQLiteConnection SQLiteConn = new System.Data.SQLite.SQLiteConnection("Data Source=" + dbFileName + ";Version=3;"))
			{
				SQLiteConn.Open();
				System.Data.SQLite.SQLiteCommand SQLiteCmd = new System.Data.SQLite.SQLiteCommand("CREATE TABLE IF NOT EXISTS Counters(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counter_name TEXT, instance_name TEXT, value TEXT, category TEXT);",SQLiteConn);
				SQLiteCmd.ExecuteNonQuery();
			}
		}
		
		public static void Main(string[] args)
		{
			CreateAndCheckSQLiteDB("1.sqlite");
			
		}
	}
}
 
 