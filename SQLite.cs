using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.Data.Common;
using System.Globalization;
//using Microsoft.Office.Interop.Excel;
using System.Text.RegularExpressions;

namespace PERF_COUNTERS_CSHARP
{
  class Program
  {
    public static bool PrintResponce(string CommandText)
    {
      using (var dbConnect = new SQLiteConnection("Data Source=" + GlobalConstant.DBNAME + ";Version=3;"))
      {
        try
        {
          dbConnect.Open();
        }
        catch (SQLiteException ex)
        {
          Console.WriteLine("Error: " + ex.Message);
          return false;
        }
        var sqlCmd = new SQLiteCommand(@CommandText, dbConnect);
        sqlCmd.ExecuteNonQuery();
        var DS = new DataSet();
        var adapter = new SQLiteDataAdapter(sqlCmd.CommandText, dbConnect);
        adapter.Fill(DS);
        foreach (DataTable table in DS.Tables)
        {
          Console.WriteLine(table.TableName);
          foreach (DataColumn column in table.Columns)
            Console.WriteLine(column.ColumnName);
          foreach (DataRow row in table.Rows)
          {
            var cells = row.ItemArray;
            foreach (var cell in cells)
            {
              Console.WriteLine("" + cell + " ");
            }
            Console.WriteLine();
          }
        }
      }

      return true;
    }

    public static async void DBCheckAndCreateAsync()
    {
      await Task.Run(() => DBCheckAndCreate());
    }
    public static void DBCheckAndCreate()
    {
      if (!System.IO.File.Exists(GlobalConstant.DBNAME))
      {
        SQLiteConnection.CreateFile(GlobalConstant.DBNAME);
      }

      using (var dbConnect = new SQLiteConnection("Data Source=" + GlobalConstant.DBNAME + ";Version=3;"))
      {
        dbConnect.Open();
        var sqlCmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS Counters(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counterName TEXT, instanceName TEXT, value TEXT, category TEXT);", dbConnect);
        sqlCmd.ExecuteNonQuery();
        sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS Workflow(id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT, value TEXT);";
        sqlCmd.ExecuteNonQuery();
        sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS Correl(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counterName TEXT, instanceName TEXT, Correl TEXT);";
        sqlCmd.ExecuteNonQuery();
        sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS StatAnalit(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counter_type TEXT, diff TEXT);";
        sqlCmd.ExecuteNonQuery();

        /*var sqlCmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS StatAnalit(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counter_type TEXT, diff TEXT);",dbConnect);
				sqlCmd.ExecuteNonQuery();

				sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS Counters(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, identifier INTEGER, value TEXT);";
				sqlCmd.ExecuteNonQuery();

				sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS Workflow(id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT, value TEXT);";
				sqlCmd.ExecuteNonQuery();

				sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS Correl(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, provider TEXT, counterName TEXT, instanceName TEXT, Correl TEXT);";
				sqlCmd.ExecuteNonQuery();

				sqlCmd.CommandText = "CREATE TABLE IF NOT EXISTS CounterIdentifier(id INTEGER PRIMARY KEY AUTOINCREMENT, server TEXT, provider TEXT, counterName TEXT, instanceName TEXT);";
				sqlCmd.ExecuteNonQuery();*/
      }
    }


    public static System.Data.DataSet SqliteQuery(string query, out DateTime dt)
    {
      using (var dbConnect = new SQLiteConnection("Data Source=" + GlobalConstant.DBNAME + ";Version=3;"))
      {
        try
        {
          dbConnect.Open();
        }
        catch (SQLiteException ex)
        {
          Console.WriteLine("Error: " + ex.Message);
          dt = DateTime.MinValue;
          return null;
        }
        var sqlCmd = new SQLiteCommand(query, dbConnect);
        dt = DateTime.Now;
        sqlCmd.ExecuteNonQuery();
        var adapter = new SQLiteDataAdapter(sqlCmd.CommandText, dbConnect);
        var DS = new DataSet();
        adapter.Fill(DS);
        return DS;
      }
    }

    public static void Main(string[] args)
    {
      static void Factorial()
      {

        int result = 1;
        for (int i = 1; i <= 6; i++)
        {
          result *= i;
        }
        Thread.Sleep(2000);
        Console.WriteLine($"Факториал равен {result}");
      }
      // определение асинхронного метода
      static async void FactorialAsync()
      {
        Console.WriteLine("Начало метода FactorialAsync"); // выполняется синхронно
        await Task.Run(() => Factorial()).ConfigureAwait(false);                // выполняется асинхронно
        Console.WriteLine("Конец метода FactorialAsync");
      }

      FactorialAsync();


      while (true)
      {
        Console.WriteLine("ping");
        Thread.Sleep(1000);
      }
      DBCheckAndCreateAsync();

      Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
      Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
      var pccs = PerformanceCounterCategory.GetCategories();
      foreach (var category in pccs)
      {
        InstanceDataCollectionCollection p1 = null;
        InstanceDataCollectionCollection p2 = null;
        var pcc = new PerformanceCounterCategory(category.CategoryName);
        p1 = pcc.ReadCategory();

        if (pcc.CategoryType == PerformanceCounterCategoryType.SingleInstance)
        {
          p2 = pcc.ReadCategory();
          Thread.Sleep(1000);
        }

        using (var dbConnect = new System.Data.SQLite.SQLiteConnection("Data Source=" + GlobalConstant.DBNAME + ";Version=3;"))
        {
          dbConnect.Open();
          var transaction = dbConnect.BeginTransaction();
          var sqlCmd = new System.Data.SQLite.SQLiteCommand(@"INSERT INTO Counters (datetime,server,counterName,instanceName,value,category) VALUES (?,?,?,?,?,?)", dbConnect);
          sqlCmd.Parameters.Add("@datetime", DbType.String);
          sqlCmd.Parameters.Add("@server", DbType.String);
          sqlCmd.Parameters.Add("@counterName", DbType.String);
          sqlCmd.Parameters.Add("@instanceName", DbType.String);
          sqlCmd.Parameters.Add("@value", DbType.String);
          sqlCmd.Parameters.Add("@category", DbType.String);

          sqlCmd.Parameters["@server"].Value = "asu-03-bde";
          sqlCmd.Parameters["@category"].Value = category.CategoryName;

          foreach (var counter in p1.Keys)
          {
            string counterName = counter.ToString();
            sqlCmd.Parameters["@counterName"].Value = counterName;
            foreach (var instance in p1[counterName].Keys)
            {
              string instanceName = instance.ToString();
              //Console.WriteLine(counterName + " " + instanceName + " " + CounterSample.Calculate(p1[counterName][instanceName].Sample, p2[counterName][instanceName].Sample));
              sqlCmd.Parameters["@datetime"].Value = DateTime.Now.ToString();
              sqlCmd.Parameters["@instanceName"].Value = instanceName;
              if (p2 == null)
              {
                sqlCmd.Parameters["@value"].Value = CounterSample.Calculate(p1[counterName][instanceName].Sample);
              }
              else
              {
                sqlCmd.Parameters["@value"].Value = CounterSample.Calculate(p1[counterName][instanceName].Sample, p2[counterName][instanceName].Sample);
              }
              sqlCmd.ExecuteNonQuery();
            }
          }

          transaction.Commit();
        }


      }




    }
  }
}