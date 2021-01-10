using System;
using System.Diagnostics;

namespace Nez
{
	// TODO: add Conditionals for all log levels
	public static partial class Debug
	{
		enum LogType
		{
			Error,
			Warn,
			Log,
			Info,
			Trace
		}


		#region Logging

		[DebuggerHidden]
		static void Log(LogType type, string format)
		{
			//System.Console.WriteLine($"{type}: {string.Format(format, args)}");
			switch (type)
			{
				case LogType.Error:
					System.Diagnostics.Debug.WriteLine(type.ToString() + ": " + format);
					break;
				case LogType.Warn:
					System.Diagnostics.Debug.WriteLine(type.ToString() + ": " + format);
					break;
				case LogType.Log:
					System.Diagnostics.Debug.WriteLine(type.ToString() + ": " + format);
					break;
				case LogType.Info:
					System.Diagnostics.Debug.WriteLine(type.ToString() + ": " + format);
					break;
				case LogType.Trace:
					System.Diagnostics.Debug.WriteLine(type.ToString() + ": " + format);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[DebuggerHidden]
		public static void Error(string format)
		{
			Log(LogType.Error, format);
		}

		[DebuggerHidden]
		public static void ErrorIf(bool condition, string format)
		{
			if (condition)
				Log(LogType.Error, format);
		}

		[DebuggerHidden]
		[Conditional("DEBUG")]
		public static void ErrorIfDebug(bool condition, string format)
		{
			if (condition)
				Log(LogType.Error, format);
		}

		[DebuggerHidden]
		public static void Warn(string format)
		{
			Log(LogType.Warn, format);
		}

		[DebuggerHidden]
		public static void WarnIf(bool condition, string format)
		{
			if (condition)
				Log(LogType.Warn, format);
		}

		[Conditional("DEBUG")]
		[DebuggerHidden]
		public static void Log(object obj)
		{
			Log(LogType.Log, obj?.ToString() ?? "<null>");
		}

		[Conditional("DEBUG")]
		[DebuggerHidden]
		public static void Log(string obj)
		{
			Log(LogType.Log, obj);
		}

		[Conditional("DEBUG")]
		[DebuggerHidden]
		public static void LogIf(bool condition, string format)
		{
			if (condition)
				Log(LogType.Log, format);
		}

		[Conditional("DEBUG")]
		[DebuggerHidden]
		public static void Info(string format)
		{
			Log(LogType.Info, format);
		}

		[Conditional("DEBUG")]
		[DebuggerHidden]
		public static void Trace(string format)
		{
			Log(LogType.Trace, format);
		}

		#endregion


		[Conditional("DEBUG")]
		public static void BreakIf(bool condition)
		{
			if (condition)
				Debugger.Break();
		}

		[Conditional("DEBUG")]
		public static void Break_()
		{
			Debugger.Break();
		}

		/// <summary>
		/// times how long an Action takes to run and returns the TimeSpan
		/// </summary>
		/// <returns>The action.</returns>
		/// <param name="action">Action.</param>
		public static TimeSpan TimeAction(Action action, uint numberOfIterations = 1)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			for (var i = 0; i < numberOfIterations; i++)
				action();
			stopwatch.Stop();

			if (numberOfIterations > 1)
				return TimeSpan.FromTicks(stopwatch.Elapsed.Ticks / numberOfIterations);

			return stopwatch.Elapsed;
		}
	}
}