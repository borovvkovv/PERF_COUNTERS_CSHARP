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

namespace PerfCounters3
{
	class Program
	{
		public static void Main0(string[] args)
		{
			var pcc = new PerformanceCounterCategory("Client Side Caching");   
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