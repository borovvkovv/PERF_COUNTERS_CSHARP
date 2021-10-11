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

	public static class GlobalConstant
	{
		public const string DBNAME = "sqlt.sqlite";
	}
	class double_Point
	{
		public float value {get; set;}//System.Single = float в C#
		public DateTime time {get; set;}
	}
}

