using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.PerformanceData;
using System.Threading;
using System.Data.SQLite;
using System.Linq;
using System.IO;
using System.Data.SqlClient;
using System.Data.Common;
//using Microsoft.Office.Interop.Excel;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;


namespace PERF_COUNTERS_CSHARP
{

	class Values
	{
		public List<double> CounterValues { get; set; }
		public List<double> WFValues { get; set; }

	}

	struct Args
	{
		public string server;
		public string category;
		public string dbFileName;

		public Args(string server, string category, string dbFileName)
		{
			this.server = server;
			this.category = category;
			this.dbFileName = dbFileName;
		}
	}

	struct Columns
	{
		public DateTime dateTime;
		public string server;
		public string counterName;
		public string instanceName;
		public float value;
		public string category;

		public Columns(DateTime dateTime, string server, string category, string counterName, float value, string instanceName = null)
		{
			this.dateTime = dateTime;
			this.server = server;
			this.counterName = counterName;
			this.instanceName = instanceName;
			this.value = value;
			this.category = category;
		}
	}

	struct ColumnsBrief
	{
		public int Id;
		public DateTime datetime;
		public string server;
		public string category;

		public ColumnsBrief(int Id, DateTime datetime, string server, string category)
		{
			this.Id = Id;
			this.datetime = datetime;
			this.server = server;
			this.category = category;
		}
	}

	struct ColumnsBriefKey
	{
		public DateTime datetime;
		public string server;
		public string category;

		public ColumnsBriefKey(DateTime datetime, string server, string category)
		{
			this.datetime = datetime;
			this.server = server;
			this.category = category;
		}
	}

	struct ColumnsCI
	{
		public int Id;
		public string server;
		public string category;

		public ColumnsCI(int Id, string server, string category)
		{
			this.Id = Id;
			this.server = server;
			this.category = category;
		}
	}

	struct ColumnsC
	{
		public DateTime datetime;
		public int Name;

		public ColumnsC(DateTime datetime, int Name)
		{
			this.datetime = datetime;
			this.Name = Name;
		}
	}

	class Program1
	{
		//public static ReaderWriterLockSlim RWLS = new ReaderWriterLockSlim();
		//public static object sync = new object();
		public static BlockingCollection<List<Columns>> BC;
		public static BlockingCollection<List<ColumnsBrief>> BCBrief;
		public static DataSet DSWF;
		public static List<ColumnsC> C;
		public static List<DateTime> WF;
		public static DataSet DSCI;
		public static DataSet DSC;
		public static List<ColumnsCI> CI;

		public static void Consumer()
		{
			using (var dbConnect = new System.Data.SQLite.SQLiteConnection("Data Source=" + GlobalConstant.DBNAME + ";Version=3;")) {
				dbConnect.Open();
				while (!BC.IsCompleted) {
					var columns = new List<Columns>();
					if (BC.TryTake(out columns))
					{
						foreach (var column in columns)
						{
							if (column.category == "Workflow")
							{
								var firstTransaction = dbConnect.BeginTransaction();
								var sqlCmd = new System.Data.SQLite.SQLiteCommand(@"INSERT INTO Workflow (date,value) VALUES (?,?)",dbConnect);
								sqlCmd.Parameters.Add("@date", DbType.String);
								sqlCmd.Parameters.Add("@value", DbType.String);

								foreach (var column2 in columns)
								{
									sqlCmd.Parameters["@date"].Value = column2.datetime;
									sqlCmd.Parameters["@value"].Value = column2.value;
									sqlCmd.ExecuteNonQuery();
								}
								firstTransaction.Commit();
							}
							else
							{
								var firstTransaction = dbConnect.BeginTransaction();
								var sqlCmd = new System.Data.SQLite.SQLiteCommand(dbConnect);
								foreach (var column3 in columns)
								{
									var DS = new DataSet();
									sqlCmd.Parameters.Clear();
									sqlCmd.CommandText = @"SELECT * FROM CounterIdentifier WHERE server='"+column3.server+"' AND provider='"+column3.category+"' AND counterName='"+
									column3.counterName+"' AND instanceName='"+column3.instanceName+"'";
									sqlCmd.ExecuteNonQuery();
									var adapter = new SQLiteDataAdapter(sqlCmd.CommandText, dbConnect);
									adapter.Fill(DS);
									UInt32 identifier = 0;

									if (DS.Tables[0].Rows.Count != 0)//если запись в таблице Correl уже существует, то её удаляем
									{
										identifier = Convert.ToUInt32(DS.Tables[0].Rows[0].ItemArray[0].ToString());
									}
									else
									{
										sqlCmd.Parameters.Clear();
										sqlCmd.CommandText = @"INSERT INTO CounterIdentifier (server,provider,counterName,instanceName) VALUES (?,?,?,?)";
										sqlCmd.Parameters.Add("@server", DbType.String);
										sqlCmd.Parameters.Add("@provider", DbType.String);
										sqlCmd.Parameters.Add("@counterName", DbType.String);
										sqlCmd.Parameters.Add("@instanceName", DbType.String);

										sqlCmd.Parameters["@server"].Value = column3.server;
										sqlCmd.Parameters["@provider"].Value = column3.category;
										sqlCmd.Parameters["@counterName"].Value = column3.counterName;
										sqlCmd.Parameters["@instanceName"].Value = column3.instanceName;

										sqlCmd.ExecuteNonQuery();

										DS = new DataSet();
										sqlCmd.Parameters.Clear();
										sqlCmd.CommandText = @"SELECT * FROM CounterIdentifier WHERE server='"+column3.server+"' AND provider='"+column3.category+"' AND counterName='"+
										column3.counterName+"' AND instanceName='"+column3.instanceName+"'";
										sqlCmd.ExecuteNonQuery();
										adapter = new SQLiteDataAdapter(sqlCmd.CommandText, dbConnect);
										adapter.Fill(DS);
										try
										{
											identifier = Convert.ToUInt32(DS.Tables[0].Rows[0].ItemArray[0].ToString());
										}
										catch (Exception e)
										{
											Console.WriteLine("Exc "+e);
											Console.WriteLine(column3.server+" "+column3.category+" "+column3.counterName+" "+column3.instanceName);
											foreach (System.Data.DataTable table in DS.Tables)
											{
												Console.WriteLine(table.TableName);
												foreach (DataColumn column in table.Columns)
													Console.WriteLine(column.ColumnName);
												foreach (DataRow row in table.Rows)
												{
													var cells = row.ItemArray;
													foreach (object cell in cells)
													{
														Console.WriteLine("" + cell + " ");
													}
													Console.WriteLine();
												}
											}
											Console.ReadKey();
										}
									}

									sqlCmd.Parameters.Clear();
									sqlCmd.CommandText = @"INSERT INTO Counters (datetime,identifier,value) VALUES (?,?,?)";
									sqlCmd.Parameters.Add("@datetime", DbType.String);
									sqlCmd.Parameters.Add("@identifier", DbType.UInt32);
									sqlCmd.Parameters.Add("@value", DbType.String);

									sqlCmd.Parameters["@datetime"].Value = column3.datetime;
									sqlCmd.Parameters["@identifier"].Value = identifier;
									sqlCmd.Parameters["@value"].Value = column3.value;
									sqlCmd.ExecuteNonQuery();
								}
								firstTransaction.Commit();
							}
							break;
						}
					}
				}
			}
		}
		public static string GetCellValueFromDS(DataSet DS, string counterName, string instanceName)
		{
			foreach (var row in DS.Tables[0].Rows) {
				var cells = row.ItemArray;
				//Console.WriteLine("Значение в DS: "+" "+cells[0].ToString().ToLower().Trim()+" ");
				if (cells[0].ToString().ToLower().Trim() == counterName && cells[1].ToString().ToLower().Trim() == instanceName) {
					//Console.WriteLine("Нашел");
					return cells[2].ToString();
				}
			}
			//Console.WriteLine("Не нашел "+counterName+ " "+instanceName);
			return null;
		}
		public static void CorrelProcessing()
		{
			#region Выборка всех записей Workflow и помещение их в DataSet (DS id, date, value)
			DataSet DS = SqliteQuery(@"SELECT * FROM Workflow");
			#endregion

			var ListForCorrel = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Values>>>>();

			foreach (var row in DS.Tables[0].Rows)
			{
				var cells = row.ItemArray;
				var WFValue = Convert.ToDouble(cells[2]);
				var startDateTime = Convert.ToDateTime(cells[1]);

				#region Выборка всех счетчиков из SQLite, входящий в допустимое время startDateTime - endDateTime, и помещение их в DataSet (DS2 ID, datetime, server, counterName, instanceName, value, category)
				DataSet DS2 = SqliteQuery(@"SELECT * FROM Counters WHERE datetime>='" + startDateTime.AddSeconds(-30) + "' AND datetime<='" + startDateTime.AddSeconds(30) + "'");
				#endregion

				#region Запись листа ListForCorrel из DS2 для последующей корреляции и записи в SQLite
				foreach (var row2 in DS2.Tables[0].Rows) {
					var cells2 = row2.ItemArray;
					var ServerName = cells2[2].ToString().ToLower();
					if (!ListForCorrel.ContainsKey(ServerName)) {
						ListForCorrel[ServerName] = new Dictionary<string, Dictionary<string, Dictionary<string, Values>>>();
					}

					string Provider = cells2[6].ToString().ToLower();
					if (!ListForCorrel[ServerName].ContainsKey(Provider)) {
						ListForCorrel[ServerName][Provider] = new Dictionary<string, Dictionary<string, Values>>();
					}

					string counterName = cells2[3].ToString().ToLower();
					if (!ListForCorrel[ServerName][Provider].ContainsKey(counterName)) {
						ListForCorrel[ServerName][Provider][counterName] = new Dictionary<string, Values>();
					}

					string instanceName = cells2[4].ToString().ToLower();
					if (!ListForCorrel[ServerName][Provider][counterName].ContainsKey(instanceName)) {
						ListForCorrel[ServerName][Provider][counterName][instanceName] = new Values();
						ListForCorrel[ServerName][Provider][counterName][instanceName].CounterValues = new List<double>();
						ListForCorrel[ServerName][Provider][counterName][instanceName].WFValues = new List<double>();
					}

					double CounterValue = Convert.ToDouble(cells2[5]);

					ListForCorrel[ServerName][Provider][counterName][instanceName].CounterValues.Add(CounterValue);
					ListForCorrel[ServerName][Provider][counterName][instanceName].WFValues.Add(WFValue);
				}
				#endregion
			}

			#region расчет корреляции с помощью листа ListForCorrel и (пере)запись коэффициентов в SQLite.Correl
			dbConnect.Open();

			var application = new Application();
			var worksheetFunction = application.WorksheetFunction;

			foreach (var item in ListForCorrel.Keys) //имена серверов
				foreach (var item2 in ListForCorrel[item].Keys) //провайдеров
					foreach (var item3 in ListForCorrel[item][item2].Keys) 		//имена счетчиков
						foreach (var item4 in ListForCorrel[item][item2][item3])
						{//инстанции
							DataSet DS3 = new DataSet();
							sqlCmd.CommandText = @"SELECT * FROM Correl WHERE server='" + item + "' AND provider='" + item2 + "' AND counterName='" + item3 + "' AND instanceName='" + item4.Key.Replace("'", "") + "'";
							sqlCmd.ExecuteNonQuery();
							SQLiteDataAdapter adapter3 = new SQLiteDataAdapter(sqlCmd.CommandText, dbConnect);
							adapter3.Fill(DS3);
							if (DS3.Tables[0].Rows.Count != 0)
							{	//если запись в таблице Correl уже существует, то её удаляем
								string RowIDForDeleting = DS3.Tables[0].Rows[0].ItemArray[0].ToString();
								sqlCmd.CommandText = @"DELETE FROM Correl WHERE ID='" + RowIDForDeleting + "'";
								sqlCmd.ExecuteNonQuery();
							}
							sqlCmd = new SQLiteCommand(@"INSERT INTO Correl (server, provider, counterName, instanceName, correl) VALUES (?,?,?,?,?)", dbConnect);//добавляем новую строку в таблицу Correl (server, counterName, instanceName,correl)
							sqlCmd.Parameters.Add("@server", DbType.String);
							sqlCmd.Parameters.Add("@provider", DbType.String);
							sqlCmd.Parameters.Add("@counterName", DbType.String);
							sqlCmd.Parameters.Add("@instanceName", DbType.String);
							sqlCmd.Parameters.Add("@correl", DbType.String);

							SQLiteTransaction firstTransaction = dbConnect.BeginTransaction();
							sqlCmd.Parameters["@server"].Value = item;
							sqlCmd.Parameters["@provider"].Value = item2;
							sqlCmd.Parameters["@counterName"].Value = item3;
							sqlCmd.Parameters["@instanceName"].Value = item4.Key;
							try {
								sqlCmd.Parameters["@correl"].Value = worksheetFunction.Correl(item4.Value.WFValues.ToArray(), item4.Value.CounterValues.ToArray());
								sqlCmd.ExecuteNonQuery();
							} catch {
								firstTransaction.Commit();
								continue;
							}
							firstTransaction.Commit();
						}
			#endregion

		}
		public static void StatAnalitRowProcessing(int min, int max, SQLiteConnection dbConnect)
		{
			//List<ColumnsBriefKey> ListToBC = new List<ColumnsBriefKey>();
			SQLiteCommand Cmd = new SQLiteCommand(dbConnect);
			var StatAnalit = new Dictionary<ColumnsBriefKey,int>();//дата,сервер,провайдер,diff
			DataSet DS = new DataSet();
			SQLiteDataAdapter adapter;

			for (int i=min; i<max; i++)
			{
				/*Stopwatch swTime = Stopwatch.StartNew();
				DataSet DS = new DataSet();
				Cmd.CommandText = @"SELECT * FROM CounterIdentifier WHERE Id='"+C[i].Name+"'";
				Cmd.ExecuteNonQuery();
				SQLiteDataAdapter adapter = new SQLiteDataAdapter(Cmd.CommandText, dbConnect);
				adapter.Fill(DS);
				var time = swTime.ElapsedMilliseconds;
				Console.WriteLine("SELECT: "+time);
				Stopwatch swTime2 = Stopwatch.StartNew();
				var responce2 = CI.Where(x=>x.Id == C[i].Name);
				var time2 = swTime2.ElapsedMilliseconds;
				Console.WriteLine("Where: "+time2);*/

				DS.Clear();
				Cmd.CommandText = @"SELECT server,provider FROM CounterIdentifier WHERE Id='"+C[i].Name+"'";
				Cmd.ExecuteNonQuery();
				adapter = new SQLiteDataAdapter(Cmd.CommandText, dbConnect);
				adapter.Fill(DS);

				var cells = DS.Tables[0].Rows[0].ItemArray;
				var CB = new ColumnsBriefKey(C[i].datetime,cells[0].ToString(),cells[1].ToString());
				if (!StatAnalit.ContainsKey(CB))
                {
					foreach (var item3 in WF)
					{
						var diff = C[i].datetime - item3;
						if (Math.Abs(diff.TotalSeconds) <=30)
						{
							StatAnalit[CB] = (int)diff.TotalSeconds;
							Console.WriteLine(0);
							break;
						}

					}

                }
			}
			//BCBrief.Add(ListToBC);
			Console.WriteLine("Готово");
		}
		public static void StatAnalit()
		{
			List<Thread> LT = new List<Thread>();
			BCBrief = new BlockingCollection<List<ColumnsBrief>>();

			DSWF = SqliteQuery(@"SELECT * FROM Workflow");
			DSC = SqliteQuery(@"SELECT * FROM Counters");
			DSCI = SqliteQuery(@"SELECT * FROM CounterIdentifier");

			C =new List<ColumnsC>();
			WF = new List<DateTime>();
			CI = new List<ColumnsCI>();

			foreach (DataRow item in DSC.Tables[0].Rows)
			{
				var cells = item.ItemArray;
				C.Add(new ColumnsC(DateTime.Parse(cells[1].ToString()),Convert.ToInt32(cells[2])));
			}

			foreach (DataRow item in DSWF.Tables[0].Rows)
			{
				var cells = item.ItemArray;
				WF.Add(Convert.ToDateTime(cells[1]));
			}

			foreach (DataRow item in DSCI.Tables[0].Rows)
			{
				var cells = item.ItemArray;
				CI.Add(new ColumnsCI(Convert.ToInt32(cells[0]),cells[1].ToString(),cells[2].ToString()));
			}

			int sum = C.Count/200000;

			for (int i=0;i<sum;i++)
			{
				int j = i;
				LT.Add(new Thread(delegate() {StatAnalitRowProcessing((sum-(sum-j))*200000, (sum-(sum-(j+1)))*200000, dbConnect);}));
			}
			LT.Add(new Thread(delegate() {StatAnalitRowProcessing(sum*200000, C.Count, dbConnect);}));
			foreach (var it in LT)
			{
				it.Start();
			}

			bool inner_flag = true;

			while (true)
			{
				inner_flag = true;
				foreach (var item in LT)
				{
					if (item.IsAlive)
					{
						inner_flag = false;
						//Thread.Sleep(600000);
						break;
					}
				}
				if (inner_flag)
				{
					break;
				}
			}
			dbConnect.Close();

		}
		public static void InsertWorkflow(string server)
		{
			System.DateTime startDateTime = DateTime.Now;
			var DS = MSSqlQuery(@"SELECT COUNT(TaskID) FROM [DIRECTUM].[dbo].[SBWorkflowProcessing] with(nolock)");
			var columns = new List<Columns>();
			columns.Add(new Columns(startDateTime, "", "Workflow", "", Convert.ToSingle(DS.Tables[0].Rows[0].ItemArray[0]), ""));
			BC.Add(columns);
		}
		public static void InsertSqlCounters(string server)
		{
			var formattedDS = new List<List<string>>();

			System.DateTime startDateTime = DateTime.Now;
			var firstDS = MSSqlQuery(@"SELECT [counterName],[instanceName],[cntr_value],[cntr_type] FROM [sqldiag].[sys].[dm_os_performance_counters] WHERE (object_name NOT LIKE 'SQLServer:Deprecated Features%')");
			System.Threading.Thread.Sleep(1000);
			System.DateTime startDateTimeSecond = DateTime.Now;
			var secondDS = MSSqlQuery(@"SELECT [counterName],[instanceName],[cntr_value],[cntr_type] FROM [sqldiag].[sys].[dm_os_performance_counters] WHERE (object_name NOT LIKE 'SQLServer:Deprecated Features%')");

			#region Расчет счетчиков SQL и помещение в лист formattedDS(дата,сервер,имя,инст,значение
			foreach (DataRow row in firstDS.Tables[0].Rows)
			{
				var cells = row.ItemArray;
				switch (cells[3].ToString())
				{
					case "65792":
						formattedDS.Add(new List<string>
						{
							startDateTime.ToString(),
							server,
							cells[0].ToString(),
							cells[1].ToString(),
							cells[2].ToString()
						});
						break;
					case "272696320":
						formattedDS.Add(new List<string>
						{
							startDateTime.ToString(),
							server,
							cells[0].ToString(),
							cells[1].ToString(),
							cells[2].ToString()
						});
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
			#endregion

			#region Помещение счетчиков в Pipeline
			var columns = new List<Columns>();

			foreach (var list in formattedDS)
			{
				columns.Add(new Columns(Convert.ToDateTime(list[0]), list[1], "SQL", list[2], Convert.ToSingle(list[4]), list[3]));
			}
			BC.Add(columns);
			#endregion
		}

		public static System.Data.DataSet MSSqlQuery (string query)
		{
			using (var sqlConnect = new SqlConnection(@"Data Source=sng-drmdb-sql;Initial Catalog=" + GlobalConstant.DBNAME + ";Integrated Security=SSPI;"))
			{
				try
				{
					dbConnect.Open();
				}
				catch (SQLiteException ex)
				{
					Console.WriteLine("Error: "+ex.Message);
					return false;
				}
				var sqlCmd = new SqlCommand(query,sqlConnect);
				sqlCmd.ExecuteNonQuery();
				var adapter = new System.Data.SqlClient.SqlDataAdapter(sqlCmd.CommandText, sqlConnect);
				var DS = new DataSet();
				adapter.Fill(DS);
				return DS;
			}
		}
		static void WriteServerCounters(string server, string category)
		{
			System.DateTime startDateTime;

			#region Получение значений счетчиков p1
			var pcc = new PerformanceCounterCategory(categoryName: category, machineName: server);
			startDateTime = DateTime.Now;
			var p1 = pcc.ReadCategory();
			#endregion

			#region Помещение счетчиков в Pipeline
			var columns = new List<Columns>();

			foreach (var counter in p1.Keys)
			{
				string counterName = counter.ToString();
				foreach (var instance in p1[counterName].Keys)
				{
					string instanceName = instance.ToString();
					//if (instanceName.Contains("_total") || instanceName.Contains("systemdiagnosticsperfcounterlibsingleinstance"))
					try
					{
						if (p1[counterName].Contains(instanceName))
						{
							//Получение значений счетчиков p2
							Thread.Sleep(1000);
							var p2 = pcc.ReadCategory();
							var value = CounterSample.Calculate(p1[counterName][instanceName].Sample, p2[counterName][instanceName].Sample);
							columns.Add(new Columns(startDateTime, server, category, counterName, value, instanceName));
						}
						else
						{
							var value = CounterSample.Calculate(p1[counterName][instanceName].Sample);
							columns.Add(new Columns(startDateTime, server, category, counterName, value, instanceName));
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine("Exception occured: " + ex);
						Console.WriteLine("On " + server + ", " + category + ", " + counterName + ", " + instanceName);
					}
				}
			}
			BC.Add(columns);
			#endregion
		}
		public static void Main4(string[] args)
		{
			BC = new BlockingCollection<List<Columns>>();

			/*int WorkerThreads = 1000;
			int CompletionThreads = 1000;
			ThreadPool.SetMinThreads(WorkerThreads, CompletionThreads);*/


			//PrintResponce("Sample.sqlite", "DROP TABLE Correl");
			//PrintResponce("Sample.sqlite", "SELECT * FROM Workflow");
			//DBCheckAndCreate("Sample.sqlite");

			//PrintResponce("Sample.sqlite", "SELECT DISTINCT * FROM Counters WHERE counterName='table opens/sec'");

			Stopwatch swTime = Stopwatch.StartNew();
			//CorrelProcessing("Sample.sqlite");
			//StatAnalit("Sample.sqlite");
			var time = swTime.ElapsedMilliseconds;
			Console.WriteLine("Time: "+time / 1000);
			//PrintResponce("Sample.sqlite", "SELECT * FROM Correl");
			//InsertSqlCounters("sng-drmdb-sql","directum","Sample.sqlite");
			//CorrelProcessing("Sample.sqlite");
			//PrintResponce("Sample.sqlite", "SELECT server,provider,counterName,instanceName,correl FROM Correl WHERE server='sng-drmweb-01' ORDER BY correl");
			PrintResponce("Sample.sqlite", "SELECT   count(id) FROM statanalit");
			/*ThreadPool.QueueUserWorkItem(delegate(object state) {Consumer();});
			for (int i=0; i<60; i++)
			{
				Dictionary<string,PerformanceCounterCategory[]> DictPPC = new Dictionary<string,PerformanceCounterCategory[]>();
				StreamReader SR = new StreamReader("computerlist.txt");

				while (!SR.EndOfStream)
				{
					string server = SR.ReadLine();
					DictPPC.Add(server,PerformanceCounterCategory.GetCategories(server));
				}
				SR.Close();
				Stopwatch swTime =  Stopwatch.StartNew();

				using (var finished = new CountdownEvent(1))
				{
					finished.AddCount();
					ThreadPool.QueueUserWorkItem(delegate(object state) { InsertWorkflow("sng-drmdb-sql", "directum"); finished.Signal();});
	             	finished.AddCount();
	             	ThreadPool.QueueUserWorkItem(delegate(object state) { InsertSqlCounters("sng-drmdb-sql", "directum"); finished.Signal();});

					Console.WriteLine("Start: " + DateTime.Now);
					foreach (var server in DictPPC.Keys)
					{
						PerformanceCounterCategory[] cats= DictPPC[server];
						foreach (var name in cats)
				        {
							if (name.CategoryName == "Process" || name.CategoryName == "Thread") continue;
							finished.AddCount();
							ThreadPool.QueueUserWorkItem(delegate(object state) { WriteServerCounters(server, name.CategoryName, "Sample.sqlite"); finished.Signal();}, null);

						}
					}
					finished.Signal();
					finished.Wait();
					Console.WriteLine("Stop: " + DateTime.Now);
				}
				var time = swTime.ElapsedMilliseconds;

				Console.WriteLine(time/1000);
				Thread.Sleep(60000);
			}*/
			Console.ReadKey();

		}//main
	}//class program
}//namespace
