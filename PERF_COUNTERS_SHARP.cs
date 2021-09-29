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


namespace PERF_COUNTERS_SHARP
{
	
	class Values
	{
		public List<double> CounterValues { get; set; }
		public List<double> WFValues { get; set; }
		
	}
	
	struct Args
	{
		public string Server;
		public string Category;
		public string dbFileName;
		
		public Args(string Server, string Category, string dbFileName)
		{
			this.Server = Server;
			this.Category = Category;
			this.dbFileName = dbFileName;
		}
	}
	
	struct Columns
	{
		public DateTime datetime;
		public string server;
		public string counter_name;
		public string instance_name;
		public float value;
		public string category;
		
		public Columns(DateTime datetime, string server, string category, string counter_name, float value, string instance_name = null)
		{
			this.datetime = datetime;
			this.server = server;
			this.counter_name = counter_name;
			this.instance_name = instance_name;
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
	
	class Program
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
			using (var SQLiteConn = new System.Data.SQLite.SQLiteConnection("Data Source=Sample.sqlite;Version=3;")) {
				SQLiteConn.Open();
				while (!BC.IsCompleted) {
					var Columns = new List<Columns>();
					if (BC.TryTake(out Columns)) {
						foreach (var col in Columns)
						{
							if (col.category == "Workflow")
							{
								using (var TransactionFirst = SQLiteConn.BeginTransaction())
								{
									
									using (var SQLiteCmd = new System.Data.SQLite.SQLiteCommand(@"INSERT INTO Workflow (date,value) VALUES (?,?)",SQLiteConn))
									{
										SQLiteCmd.Parameters.Add("@date", DbType.String);
										SQLiteCmd.Parameters.Add("@value", DbType.String);
										
										foreach (var col2 in Columns)
										{
											SQLiteCmd.Parameters["@date"].Value = col2.datetime;
											SQLiteCmd.Parameters["@value"].Value = col2.value;
											SQLiteCmd.ExecuteNonQuery();
										}
									}
									TransactionFirst.Commit();
								}
							}
							else
							{
								using (var TransactionFirst = SQLiteConn.BeginTransaction())
								{
									using (var SQLiteCmd = new System.Data.SQLite.SQLiteCommand(SQLiteConn))
									{
										
										foreach (var col3 in Columns)
										{
											var DS = new DataSet();
											SQLiteCmd.Parameters.Clear();
											SQLiteCmd.CommandText = @"SELECT * FROM CounterIdentifier WHERE server='"+col3.server+"' AND provider='"+col3.category+"' AND counter_name='"+
											col3.counter_name+"' AND instance_name='"+col3.instance_name+"'";
											SQLiteCmd.ExecuteNonQuery();
											var adapter = new SQLiteDataAdapter(SQLiteCmd.CommandText, SQLiteConn);
											adapter.Fill(DS);
											UInt32 identifier = 0;
											
											if (DS.Tables[0].Rows.Count != 0)//если запись в таблице Correl уже существует, то её удаляем
											{
												identifier = Convert.ToUInt32(DS.Tables[0].Rows[0].ItemArray[0].ToString());
											}
											else
											{
												SQLiteCmd.Parameters.Clear();
												SQLiteCmd.CommandText = @"INSERT INTO CounterIdentifier (server,provider,counter_name,instance_name) VALUES (?,?,?,?)";
												SQLiteCmd.Parameters.Add("@server", DbType.String);
												SQLiteCmd.Parameters.Add("@provider", DbType.String);
												SQLiteCmd.Parameters.Add("@counter_name", DbType.String);
												SQLiteCmd.Parameters.Add("@instance_name", DbType.String);
												
												SQLiteCmd.Parameters["@server"].Value = col3.server;
												SQLiteCmd.Parameters["@provider"].Value = col3.category;
												SQLiteCmd.Parameters["@counter_name"].Value = col3.counter_name;
												SQLiteCmd.Parameters["@instance_name"].Value = col3.instance_name;
												
												SQLiteCmd.ExecuteNonQuery();
												
												DS = new DataSet();
												SQLiteCmd.Parameters.Clear();
												SQLiteCmd.CommandText = @"SELECT * FROM CounterIdentifier WHERE server='"+col3.server+"' AND provider='"+col3.category+"' AND counter_name='"+
												col3.counter_name+"' AND instance_name='"+col3.instance_name+"'";
												SQLiteCmd.ExecuteNonQuery();
												adapter = new SQLiteDataAdapter(SQLiteCmd.CommandText, SQLiteConn);
												adapter.Fill(DS);
												try
												{
													identifier = Convert.ToUInt32(DS.Tables[0].Rows[0].ItemArray[0].ToString());
												}
												catch (Exception e)
												{
													Console.WriteLine("Exc "+e);
													Console.WriteLine(col3.server+" "+col3.category+" "+col3.counter_name+" "+col3.instance_name);
													foreach (System.Data.DataTable table in DS.Tables) {
														Console.WriteLine(table.TableName);
														foreach (DataColumn column in table.Columns)
															Console.WriteLine(column.ColumnName);
														foreach (DataRow row in table.Rows) {
															var cells = row.ItemArray;
															foreach (object cell in cells) {
																Console.WriteLine("" + cell + " ");
															}
															Console.WriteLine();
														}
													}
													Console.ReadKey();
												}
											}
											
											SQLiteCmd.Parameters.Clear();
											SQLiteCmd.CommandText = @"INSERT INTO Counters (datetime,identifier,value) VALUES (?,?,?)";
											SQLiteCmd.Parameters.Add("@datetime", DbType.String);
											SQLiteCmd.Parameters.Add("@identifier", DbType.UInt32);
											SQLiteCmd.Parameters.Add("@value", DbType.String);
										
											SQLiteCmd.Parameters["@datetime"].Value = col3.datetime;
											SQLiteCmd.Parameters["@identifier"].Value = identifier;
											SQLiteCmd.Parameters["@value"].Value = col3.value;
											SQLiteCmd.ExecuteNonQuery();
										}
									}
									TransactionFirst.Commit();
								}
							}
							break;
						}
					}
				}
			}
		}
		
		/*public static void ConsumerForStatAnalit()
		{
			#region Запись строк в БД SQLite
			using (SQLiteConnection SQLiteConn = new SQLiteConnection("Data Source=" + "Sample.sqlite" + ";Version=3;")) {
				SQLiteConn.Open();
				while (!BCBrief.IsCompleted) {
					List<ColumnsBrief> Columns = new List<ColumnsBrief>();
					if (BCBrief.TryTake(out Columns)) {
						using (SQLiteTransaction TransactionFirst = SQLiteConn.BeginTransaction()) {
							using (SQLiteCommand SQLiteCmd = new SQLiteCommand(SQLiteConn)) {
								SQLiteCmd.CommandText = @"INSERT INTO StatAnalit (datetime, server, counter_type, diff) VALUES (?,?,?,?)";//добавляем новую строку в таблицу Correl (server, counter_name, instance_name,correl)
								SQLiteCmd.Parameters.Add("@datetime", DbType.String);
								SQLiteCmd.Parameters.Add("@server", DbType.String);
								SQLiteCmd.Parameters.Add("@counter_type", DbType.String);
								SQLiteCmd.Parameters.Add("@diff", DbType.String);
								
								foreach (var col in Columns) {//типы счетчиков
									SQLiteCmd.Parameters["@datetime"].Value = col.datetime;
									SQLiteCmd.Parameters["@server"].Value = col.server;
									SQLiteCmd.Parameters["@counter_type"].Value = col.category;
									SQLiteCmd.Parameters["@diff"].Value = col.value;
									SQLiteCmd.ExecuteNonQuery();
								}
							}
							TransactionFirst.Commit();
						}
					}
					#endregion
				}
			}
		}*/
		
		public static string GetCellValueFromDS(DataSet DS, string counter_name, string instance_name)
		{
			foreach (var row in DS.Tables[0].Rows) {
				var cells = row.ItemArray;
				//Console.WriteLine("Значение в DS: "+" "+cells[0].ToString().ToLower().Trim()+" ");
				if (cells[0].ToString().ToLower().Trim() == counter_name && cells[1].ToString().ToLower().Trim() == instance_name) {
					//Console.WriteLine("Нашел");
					return cells[2].ToString();
				}
			}
			//Console.WriteLine("Не нашел "+counter_name+ " "+instance_name);
			return null;
		}
		
		public static string Processing_537003264(string counter_name, string instance_name, DataSet DSFirst)
		{
			long A1 = 0;//long представлен системным типом System.Int64
			long B1 = 0;
			
			var Formatted_counter_name = counter_name.ToLower().Trim();
			var Formatted_instance_name = instance_name.ToLower().Trim();
			var Formatted_counter_name_original = Formatted_counter_name;
			
			#region A1
			string A1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
			if (A1_string != null) {
				A1 = Convert.ToInt64(A1_string);
			} else {
				Console.WriteLine("нету A1 у 537003264");
				return null;
			}
			#endregion
			
			Formatted_counter_name = Regex.Replace(Formatted_counter_name, @"^*(\(ms\))$", "");
			Formatted_counter_name = Regex.Replace(Formatted_counter_name, @"^*(/fetch|/sec)$", " ");
			if (Formatted_counter_name[Formatted_counter_name.Length - 1] != ' ') {
				Formatted_counter_name += " base";
			} else {
				Formatted_counter_name += "base";
			}
			
			#region B1
			string B1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
			if (B1_string != null) {
				B1 = Convert.ToInt64(B1_string);
			} else {
				Formatted_counter_name = Regex.Replace(Formatted_counter_name, @"^(avg |avg. )*", "");
				B1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
				if (B1_string != null) {
					B1 = Convert.ToInt64(B1_string);
				} else {
					Formatted_counter_name = Formatted_counter_name_original + " base";
					B1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
					if (B1_string != null) {
						B1 = Convert.ToInt64(B1_string);
					} else {
						Formatted_counter_name = Formatted_counter_name_original + " bs";
						B1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
						if (B1_string != null) {
							B1 = Convert.ToInt64(B1_string);
						} else {
							Formatted_counter_name = Regex.Replace(Formatted_counter_name_original, @"^*ratio$", "") + "base";
							B1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
							if (B1_string != null) {
								B1 = Convert.ToInt64(B1_string);
							} else {
								Console.WriteLine(Formatted_counter_name + " нету. Конец");
								return null;
							}
						}
					}
				}
			}
			#endregion
			
			//Console.Write(Formatted_counter_name+" "+Formatted_instance_name+" ");
			long Divisible = A1;
			long Divider = B1;
			if (Divider != 0) {
				//Console.WriteLine(100*Divisible/Divider);
				return (100 * Divisible / Divider).ToString();
			} else {
				//Console.WriteLine("Infinity");
				return null;
			}
		}
		
		public static string Processing_1073874176(string counter_name, string instance_name, DataSet DSFirst, DataSet DSSecond)
		{
			long A1 = 0;
			long A2 = 0;
			long B1 = 0;
			long B2 = 0;
			
			var Formatted_counter_name = counter_name.ToLower().Trim();
			var Formatted_instance_name = instance_name.ToLower().Trim();
			var Formatted_counter_name_original = Formatted_counter_name;
			
			#region A1
			string A1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
			if (A1_string != null) {
				A1 = Convert.ToInt64(A1_string);
			} else {
				Console.WriteLine("нет A1 у 1073874176");
				return null;
			}
			#endregion
			
			#region A2
			string A2_string = GetCellValueFromDS(DSSecond, Formatted_counter_name, Formatted_instance_name);
			if (A2_string != null) {
				A2 = Convert.ToInt64(A2_string);
			} else {
				Console.WriteLine("нет A2 у 1073874176");
				return null;
			}
			#endregion
			
			Formatted_counter_name = Regex.Replace(Formatted_counter_name, @"^*(\(ms\))$", "");
			Formatted_counter_name = Regex.Replace(Formatted_counter_name, @"^*(/fetch|/sec)$", " ");
			if (Formatted_counter_name[Formatted_counter_name.Length - 1] != ' ') {
				Formatted_counter_name += " base";
			} else {
				Formatted_counter_name += "base";
			}
			
			#region B1
			string B1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
			if (B1_string != null) {
				B1 = Convert.ToInt64(B1_string);
			} else {
				Formatted_counter_name = Regex.Replace(Formatted_counter_name, @"^(avg |avg. )*", "");
				B1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
				if (B1_string != null) {
					B1 = Convert.ToInt64(B1_string);
				} else {
					Formatted_counter_name = Formatted_counter_name_original + " base";
					B1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
					if (B1_string != null) {
						B1 = Convert.ToInt64(B1_string);
					} else {
						Formatted_counter_name = Formatted_counter_name_original + " bs";
						B1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
						if (B1_string != null) {
							B1 = Convert.ToInt64(B1_string);
						} else {
							Formatted_counter_name = Regex.Replace(Formatted_counter_name_original, @"^*ratio$", "") + "base";
							B1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
							if (B1_string != null) {
								B1 = Convert.ToInt64(B1_string);
							} else {
								Console.WriteLine(Formatted_counter_name + " нету. Конец");
								return null;
							}
						}
					}
				}
			}
			#endregion
			
			#region B2
			string B2_string = GetCellValueFromDS(DSSecond, Formatted_counter_name, Formatted_instance_name);
			if (B2_string != null) {
				B2 = Convert.ToInt64(B2_string);
			} else {
				Console.WriteLine("нету В2 у 1073874176");
				return null;
			}
			#endregion
			
			//Console.Write(Formatted_counter_name+" "+Formatted_instance_name+" ");
			long Divisible = A2 - A1;
			long Divider = B2 - B1;
			if (Divider != 0) {
				//Console.WriteLine(Divisible/Divider);
				return (Divisible / Divider).ToString();
			} else {
				//Console.WriteLine("Infinity");
				return null;
			}
		}
		
		public static string Processing_272696576(string counter_name, string instance_name, DataSet DSFirst, DataSet DSSecond, DateTime StartDateTimeFirst, DateTime StartDateTimeSecond)
		{
			long A1 = 0;
			long A2 = 0;
			DateTime T1 = StartDateTimeFirst;
			DateTime T2 = StartDateTimeSecond;
			
			var Formatted_counter_name = counter_name.ToLower().Trim();
			var Formatted_instance_name = instance_name.ToLower().Trim();
			
			string A1_string = GetCellValueFromDS(DSFirst, Formatted_counter_name, Formatted_instance_name);
			if (A1_string != null) {
				A1 = Convert.ToInt64(A1_string);
			} else {
				Console.WriteLine("нет A1 у 272696576");
				return null;
			}
			
			string A2_string = GetCellValueFromDS(DSSecond, Formatted_counter_name, Formatted_instance_name);
			if (A2_string != null) {
				A2 = Convert.ToInt64(A2_string);
			} else {
				Console.WriteLine("нет A2 у 272696576");
				return null;
			}
			
			//Console.Write(Formatted_counter_name+" "+Formatted_instance_name+" ");
			long Divisible = A2 - A1;
			double Divider = (T2 - T1).TotalSeconds;
			if (Divider != 0) {
				//Console.WriteLine(Divisible/Divider);
				return (Divisible / Divider).ToString();
			} else {
				//Console.WriteLine("Infinity");
				return null;
			}
		}

		public static void DeleteRows(string dbFileName)
		{
			
			using (var SQLiteConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;"))
			{
				SQLiteConn.Open();
				var SQLiteCmd = new SQLiteCommand(@"DELETE FROM Counters",SQLiteConn);
				SQLiteCmd.ExecuteNonQuery();
				SQLiteCmd.CommandText = @"DELETE FROM Workflow";
				SQLiteCmd.ExecuteNonQuery();
			}
		}
		
		public static void CorrelProcessing(string dbFileName)
		{
			#region Выборка всех записей Workflow и помещение их в DataSet (DS id, date, value)
			var SQLiteConn = new SQLiteConnection();
			SQLiteConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
				SQLiteConn.Open();
				var SQLiteCmd = new System.Data.SQLite.SQLiteCommand(@"SELECT * FROM Workflow",SQLiteConn);
				SQLiteCmd.ExecuteNonQuery();
				DataSet DS = new DataSet();
				SQLiteDataAdapter adapter = new SQLiteDataAdapter(SQLiteCmd.CommandText, SQLiteConn);
				adapter.Fill(DS);
				SQLiteConn.Close();
			#endregion
			
			var ListForCorrel = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Values>>>>();
			
			foreach (var row in DS.Tables[0].Rows)
			{
				var cells = row.ItemArray;
				var WFValue = Convert.ToDouble(cells[2]);
				var StartDateTime = Convert.ToDateTime(cells[1]);
				
				#region Выборка всех счетчиков из SQLite, входящий в допустимое время StartDateTime - EndDateTime, и помещение их в DataSet (DS2 ID, datetime, server, counter_name, instance_name, value, category)
				SQLiteConn.Open();
				var DS2 = new DataSet();
				SQLiteCmd.CommandText = @"SELECT * FROM Counters WHERE datetime>='" + StartDateTime.AddSeconds(-30) + "' AND datetime<='" + StartDateTime.AddSeconds(30) + "'";
				SQLiteCmd.ExecuteNonQuery();
				SQLiteDataAdapter adapter2 = new SQLiteDataAdapter(SQLiteCmd.CommandText, SQLiteConn);
				adapter2.Fill(DS2);
				SQLiteConn.Close();
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
					
					string CounterName = cells2[3].ToString().ToLower();
					if (!ListForCorrel[ServerName][Provider].ContainsKey(CounterName)) {
						ListForCorrel[ServerName][Provider][CounterName] = new Dictionary<string, Values>();
					}
					
					string InstanceName = cells2[4].ToString().ToLower();
					if (!ListForCorrel[ServerName][Provider][CounterName].ContainsKey(InstanceName)) {
						ListForCorrel[ServerName][Provider][CounterName][InstanceName] = new Values();
						ListForCorrel[ServerName][Provider][CounterName][InstanceName].CounterValues = new List<double>();
						ListForCorrel[ServerName][Provider][CounterName][InstanceName].WFValues = new List<double>();
					}
					
					double CounterValue = Convert.ToDouble(cells2[5]);
					
					ListForCorrel[ServerName][Provider][CounterName][InstanceName].CounterValues.Add(CounterValue);
					ListForCorrel[ServerName][Provider][CounterName][InstanceName].WFValues.Add(WFValue);
				}
				#endregion
			}
			
			#region расчет корреляции с помощью листа ListForCorrel и (пере)запись коэффициентов в SQLite.Correl
			SQLiteConn.Open();
			
			var application = new Application();
			var worksheetFunction = application.WorksheetFunction;

			foreach (var item in ListForCorrel.Keys) //имена серверов
				foreach (var item2 in ListForCorrel[item].Keys) //провайдеров
					foreach (var item3 in ListForCorrel[item][item2].Keys) 		//имена счетчиков
						foreach (var item4 in ListForCorrel[item][item2][item3])
						{//инстанции
							DataSet DS3 = new DataSet();
							SQLiteCmd.CommandText = @"SELECT * FROM Correl WHERE server='" + item + "' AND provider='" + item2 + "' AND counter_name='" + item3 + "' AND instance_name='" + item4.Key.Replace("'", "") + "'";
							SQLiteCmd.ExecuteNonQuery();
							SQLiteDataAdapter adapter3 = new SQLiteDataAdapter(SQLiteCmd.CommandText, SQLiteConn);
							adapter3.Fill(DS3);
							if (DS3.Tables[0].Rows.Count != 0)
							{	//если запись в таблице Correl уже существует, то её удаляем
								string RowIDForDeleting = DS3.Tables[0].Rows[0].ItemArray[0].ToString();
								SQLiteCmd.CommandText = @"DELETE FROM Correl WHERE ID='" + RowIDForDeleting + "'";
								SQLiteCmd.ExecuteNonQuery();
							}
							SQLiteCmd = new SQLiteCommand(@"INSERT INTO Correl (server, provider, counter_name, instance_name, correl) VALUES (?,?,?,?,?)", SQLiteConn);//добавляем новую строку в таблицу Correl (server, counter_name, instance_name,correl)
							SQLiteCmd.Parameters.Add("@server", DbType.String);
							SQLiteCmd.Parameters.Add("@provider", DbType.String);
							SQLiteCmd.Parameters.Add("@counter_name", DbType.String);
							SQLiteCmd.Parameters.Add("@instance_name", DbType.String);
							SQLiteCmd.Parameters.Add("@correl", DbType.String);
							
							SQLiteTransaction TransactionFirst = SQLiteConn.BeginTransaction();
							SQLiteCmd.Parameters["@server"].Value = item;
							SQLiteCmd.Parameters["@provider"].Value = item2;
							SQLiteCmd.Parameters["@counter_name"].Value = item3;
							SQLiteCmd.Parameters["@instance_name"].Value = item4.Key;
							try {
								SQLiteCmd.Parameters["@correl"].Value = worksheetFunction.Correl(item4.Value.WFValues.ToArray(), item4.Value.CounterValues.ToArray());
								SQLiteCmd.ExecuteNonQuery();
							} catch {
								TransactionFirst.Commit();
								continue;
							}
							TransactionFirst.Commit();
						}
			#endregion

		}
		
		public static void StatAnalitRowProcessing(int min, int max, SQLiteConnection SQLiteConn)
		{
			//List<ColumnsBriefKey> ListToBC = new List<ColumnsBriefKey>();
			SQLiteCommand Cmd = new SQLiteCommand(SQLiteConn);
			var StatAnalit = new Dictionary<ColumnsBriefKey,int>();//дата,сервер,провайдер,diff
			DataSet DS = new DataSet();
			SQLiteDataAdapter adapter;
			
			for (int i=min; i<max; i++)
			{
				/*Stopwatch swTime = Stopwatch.StartNew();
				DataSet DS = new DataSet();
				Cmd.CommandText = @"SELECT * FROM CounterIdentifier WHERE Id='"+C[i].Name+"'";
				Cmd.ExecuteNonQuery();
				SQLiteDataAdapter adapter = new SQLiteDataAdapter(Cmd.CommandText, SQLiteConn);
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
				adapter = new SQLiteDataAdapter(Cmd.CommandText, SQLiteConn);
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
				
		public static void StatAnalit(string dbFileName)
		{
			List<Thread> LT = new List<Thread>();
			BCBrief = new BlockingCollection<List<ColumnsBrief>>();
			
			#region Выборка всех записей Workflow,Counters и помещение их в List
			SQLiteConnection SQLiteConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
			SQLiteConn.Open();
			var SQLiteCmd = new SQLiteCommand(SQLiteConn);
			
			DSWF = new DataSet();
			SQLiteCmd.CommandText = @"SELECT * FROM Workflow";
			SQLiteCmd.ExecuteNonQuery();
			var adapterWF = new SQLiteDataAdapter(SQLiteCmd.CommandText, SQLiteConn);
			adapterWF.Fill(DSWF);
			#endregion
			
			DSC = new DataSet();
			SQLiteCmd.CommandText = @"SELECT * FROM Counters";
			SQLiteCmd.ExecuteNonQuery();
			SQLiteDataAdapter adapterC = new SQLiteDataAdapter(SQLiteCmd.CommandText, SQLiteConn);
			adapterC.Fill(DSC);
			
			DSCI = new DataSet();
			SQLiteCmd.CommandText = @"SELECT * FROM CounterIdentifier";
			SQLiteCmd.ExecuteNonQuery();
			SQLiteDataAdapter adapterCI = new SQLiteDataAdapter(SQLiteCmd.CommandText, SQLiteConn);
			adapterCI.Fill(DSCI);
			
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
				LT.Add(new Thread(delegate() {StatAnalitRowProcessing((sum-(sum-j))*200000, (sum-(sum-(j+1)))*200000, SQLiteConn);}));
			}
			LT.Add(new Thread(delegate() {StatAnalitRowProcessing(sum*200000, C.Count, SQLiteConn);}));
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
			SQLiteConn.Close();
			
		}
		
		public static void InsertWorkflow(string Server, string DB)
		{
			System.DateTime StartDateTime;
			
			#region Выборка WF из SQL и помещение в DataSet (DS)
			
			using (var SQLConn = new System.Data.SqlClient.SqlConnection(@"Data Source=" + Server + ";Initial Catalog=" + DB + ";Integrated Security=SSPI;"))
			{
				SQLConn.Open();
				var SQLCmd = new System.Data.SqlClient.SqlCommand(@"SELECT COUNT(TaskID) FROM [DIRECTUM].[dbo].[SBWorkflowProcessing] with(nolock)",SQLConn);
				
				StartDateTime = DateTime.Now;
				SQLCmd.ExecuteNonQuery();
				
				var DS = new DataSet();
				var Adapter = new System.Data.SqlClient.SqlDataAdapter(SQLCmd.CommandText, SQLConn);
				Adapter.Fill(DS);
			}
			#endregion
			
			#region Помещение счетчиков в Pipeline
			var Columns = new List<Columns>();
			Columns.Add(new Columns(StartDateTime, "", "Workflow", "", Convert.ToSingle(DS.Tables[0].Rows[0].ItemArray[0]), ""));
			BC.Add(Columns);
			#endregion
			
		}
		
		public static void CreateAndCheckSQLiteDB(string dbFileName)
		{
			if (!System.IO.File.Exists(dbFileName)) {
				SQLiteConnection.CreateFile(dbFileName);
			}
			
			using (var SQLiteConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;"))
			{
				SQLiteConn.Open();
				var SQLiteCmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS StatAnalit(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, counter_type TEXT, diff TEXT);",SQLiteConn);
				SQLiteCmd.ExecuteNonQuery();
				
				SQLiteCmd.CommandText = "CREATE TABLE IF NOT EXISTS Counters(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, identifier INTEGER, value TEXT);";
				SQLiteCmd.ExecuteNonQuery();
				
				SQLiteCmd.CommandText = "CREATE TABLE IF NOT EXISTS Workflow(id INTEGER PRIMARY KEY AUTOINCREMENT, date TEXT, value TEXT);";
				SQLiteCmd.ExecuteNonQuery();
				
				SQLiteCmd.CommandText = "CREATE TABLE IF NOT EXISTS Correl(id INTEGER PRIMARY KEY AUTOINCREMENT, datetime TEXT, server TEXT, provider TEXT, counter_name TEXT, instance_name TEXT, Correl TEXT);";
				SQLiteCmd.ExecuteNonQuery();
				
				SQLiteCmd.CommandText = "CREATE TABLE IF NOT EXISTS CounterIdentifier(id INTEGER PRIMARY KEY AUTOINCREMENT, server TEXT, provider TEXT, counter_name TEXT, instance_name TEXT);";
				SQLiteCmd.ExecuteNonQuery();
				
				/*SQLiteCmd.CommandText = "CREATE INDEX IF NOT EXISTS index1 ON CounterIdentifier(server, provider, counter_name, instance_name);";
				SQLiteCmd.ExecuteNonQuery();
				
				SQLiteCmd.CommandText = "CREATE INDEX IF NOT EXISTS index2 ON Counters(datetime, identifier, value);";
				SQLiteCmd.ExecuteNonQuery();*/
				
			}
		}
		
		public static void InsertSQLCounters(string Server, string DB)
		{
			var FormattedDS = new List<List<string>>();
			System.DateTime StartDateTime;
			System.DateTime StartDateTimeSecond;
			
			#region Выборка счетчиков из SQL, помещение их в DataSet'ы DSFirst, DSSecond
			var SQLConn = new System.Data.SqlClient.SqlConnection(@"Data Source=" + Server + ";Initial Catalog=" + DB + ";Integrated Security=SSPI;");
			SQLConn.Open();
			var SQLCmd = new System.Data.SqlClient.SqlCommand();
			SQLCmd.Connection = SQLConn;
			
			//SQLCmd.CommandText = @"SELECT [counter_name],[instance_name],[cntr_value],[cntr_type] FROM [sqldiag].[sys].[dm_os_performance_counters] WHERE (cntr_type='65792' OR cntr_type='272696320') AND (object_name NOT LIKE 'SQLServer:Deprecated Features%')";
			SQLCmd.CommandText = @"SELECT [counter_name],[instance_name],[cntr_value],[cntr_type] FROM [sqldiag].[sys].[dm_os_performance_counters] WHERE (object_name NOT LIKE 'SQLServer:Deprecated Features%')";
			StartDateTime = DateTime.Now;
			SQLCmd.ExecuteNonQuery();
			
			var DSFirst = new DataSet();
			var AdapterFirst = new System.Data.SqlClient.SqlDataAdapter(SQLCmd.CommandText, SQLConn);
			AdapterFirst.Fill(DSFirst);
			
			System.Threading.Thread.Sleep(1000);
			SQLCmd.ExecuteNonQuery();
			StartDateTimeSecond = DateTime.Now;
			
			System.Data.DataSet DSSecond = new DataSet();
			var AdapterSecond = new System.Data.SqlClient.SqlDataAdapter(SQLCmd.CommandText, SQLConn);
			AdapterSecond.Fill(DSSecond);
			SQLConn.Close();
			#endregion
			
			#region Расчет счетчиков SQL и помещение в лист FormattedDS(дата,сервер,имя,инст,значение
			foreach (System.Data.DataRow row in DSFirst.Tables[0].Rows) {
				var cells = row.ItemArray;
				string value;
				switch (cells[3].ToString()) {
					case "65792":
						FormattedDS.Add(new List<string> {
							StartDateTime.ToString(),
							Server,
							cells[0].ToString(),
							cells[1].ToString(),
							cells[2].ToString()
						});
						break;
					case "272696320":
						FormattedDS.Add(new List<string> {
							StartDateTime.ToString(),
							Server,
							cells[0].ToString(),
							cells[1].ToString(),
							cells[2].ToString()
						});
						break;
					case "1073874176":
						value = Processing_1073874176(cells[0].ToString(), cells[1].ToString(), DSFirst, DSSecond);
						if (value != null) {
							FormattedDS.Add(new List<string> {
								StartDateTime.ToString(),
								Server,
								cells[0].ToString(),
								cells[1].ToString(),
								value
							});
						}
						break;
					case "272696576":
						value = Processing_272696576(cells[0].ToString(), cells[1].ToString(), DSFirst, DSSecond, StartDateTime, StartDateTimeSecond);
						if (value != null) {
							FormattedDS.Add(new List<string> {
								StartDateTime.ToString(),
								Server,
								cells[0].ToString(),
								cells[1].ToString(),
								value
							});
						}
						break;
					case "1073939712":
						//base
						break;
					case "537003264":
						value = Processing_537003264(cells[0].ToString(), cells[1].ToString(), DSFirst);
						if (value != null) {
							FormattedDS.Add(new List<string> {
								StartDateTime.ToString(),
								Server,
								cells[0].ToString(),
								cells[1].ToString(),
								value
							});
						}
						break;
					default:
						Console.WriteLine("Error!!! Unknown SQL counter type");
						break;
				}
			}
			#endregion
			
			#region Помещение счетчиков в Pipeline
			List<Columns> Columns = new List<Columns>();
			
			foreach (var list in FormattedDS) {
				Columns.Add(new Columns(Convert.ToDateTime(list[0]), list[1], "SQL", list[2], Convert.ToSingle(list[4]), list[3]));
			}
			BC.Add(Columns);
			#endregion
		}

		public static void ReadDataToConsole(string dbFileName, string CommandText)
		{
			
			var dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
			dbConn.Open();
			var sqlCmd = new SQLiteCommand(@CommandText,dbConn);
			sqlCmd.ExecuteNonQuery();
			DataSet ds = new DataSet();
			SQLiteDataAdapter adapter = new SQLiteDataAdapter(sqlCmd.CommandText, dbConn);
			adapter.Fill(ds);
			
			foreach (var table in ds.Tables) {
				Console.WriteLine(table.TableName);
				foreach (var column in table.Columns)
					Console.WriteLine(column.ColumnName);
				foreach (var row in table.Rows) {
					var cells = row.ItemArray;
					foreach (var cell in cells) {
						Console.WriteLine("" + cell + " ");
					}
					Console.WriteLine();
				}
			}
		}
		
		static void InsertServerCounters(string Server, string Category, string dbFileName)
		{
			System.DateTime StartDateTime;

			#region Выборка счетчиков с помощью ReadCategory p1, p2
			var pcc = new PerformanceCounterCategory(categoryName: Category, machineName: Server);
			StartDateTime = DateTime.Now;
			var p1 = pcc.ReadCategory();
			Thread.Sleep(1000);
			var p2 = pcc.ReadCategory();
			#endregion
			
			#region Помещение счетчиков в Pipeline
			var Columns = new List<Columns>();
			
			foreach (var cnt in p1.Keys) {
				string CounterName = cnt.ToString();
				foreach (var inst in p1[CounterName].Keys) {
					string InstanceName = inst.ToString();
					//if (InstanceName.Contains("_total") || InstanceName.Contains("systemdiagnosticsperfcounterlibsingleinstance"))
					try {
						if (p2[CounterName].Contains(InstanceName)) {
							var Value = CounterSample.Calculate(p1[CounterName][InstanceName].Sample, p2[CounterName][InstanceName].Sample);
							Columns.Add(new Columns(StartDateTime, Server, Category, CounterName, Value, InstanceName));
						} else {
							var Value = CounterSample.Calculate(p1[CounterName][InstanceName].Sample);
							Columns.Add(new Columns(StartDateTime, Server, Category, CounterName, Value, InstanceName));
						}
					} catch (Exception e) {
						Console.WriteLine("Exception occured: " + e);
						Console.WriteLine("On " + Server + ", " + Category + ", " + CounterName + ", " + InstanceName);
					} finally {
					}
				}
			}
			BC.Add(Columns);
			#endregion
		}
		
		public static void Main4(string[] args)
		{
			BC = new BlockingCollection<List<Columns>>();
			
			/*int WorkerThreads = 1000;
			int CompletionThreads = 1000;
			ThreadPool.SetMinThreads(WorkerThreads, CompletionThreads);*/
			
			
			//ReadDataToConsole("Sample.sqlite", "DROP TABLE Correl");
			//ReadDataToConsole("Sample.sqlite", "SELECT * FROM Workflow");
			//CreateAndCheckSQLiteDB("Sample.sqlite");
			
			//ReadDataToConsole("Sample.sqlite", "SELECT DISTINCT * FROM Counters WHERE counter_name='table opens/sec'");
			
			Stopwatch swTime = Stopwatch.StartNew();
			//CorrelProcessing("Sample.sqlite");
			//StatAnalit("Sample.sqlite");
			var time = swTime.ElapsedMilliseconds;
			Console.WriteLine("Time: "+time / 1000);
			//ReadDataToConsole("Sample.sqlite", "SELECT * FROM Correl");
			//InsertSQLCounters("sng-drmdb-sql","directum","Sample.sqlite");
			//CorrelProcessing("Sample.sqlite");
			//ReadDataToConsole("Sample.sqlite", "SELECT server,provider,counter_name,instance_name,correl FROM Correl WHERE Server='sng-drmweb-01' ORDER BY correl");
			ReadDataToConsole("Sample.sqlite", "SELECT   count(id) FROM statanalit");
			/*ThreadPool.QueueUserWorkItem(delegate(object state) {Consumer();});
			for (int i=0; i<60; i++)
			{
				Dictionary<string,PerformanceCounterCategory[]> DictPPC = new Dictionary<string,PerformanceCounterCategory[]>();
				StreamReader SR = new StreamReader("computerlist.txt");
			
				while (!SR.EndOfStream)
				{
					string Server = SR.ReadLine();
					DictPPC.Add(Server,PerformanceCounterCategory.GetCategories(Server));
				}
				SR.Close();
				Stopwatch swTime =  Stopwatch.StartNew();
				
				using (var finished = new CountdownEvent(1))
				{
					finished.AddCount();
					ThreadPool.QueueUserWorkItem(delegate(object state) { InsertWorkflow("sng-drmdb-sql", "directum"); finished.Signal();});
	             	finished.AddCount();
	             	ThreadPool.QueueUserWorkItem(delegate(object state) { InsertSQLCounters("sng-drmdb-sql", "directum"); finished.Signal();});
					
					Console.WriteLine("Start: " + DateTime.Now);
					foreach (var Server in DictPPC.Keys)
					{
						PerformanceCounterCategory[] cats= DictPPC[Server];
						foreach (var name in cats)
				        {
							if (name.CategoryName == "Process" || name.CategoryName == "Thread") continue;
							finished.AddCount();
							ThreadPool.QueueUserWorkItem(delegate(object state) { InsertServerCounters(Server, name.CategoryName, "Sample.sqlite"); finished.Signal();}, null);
							
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
