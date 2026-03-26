#region using
using System;
using System.Text;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.IO;

using Serilog;
using Serilog.Events;
#endregion

namespace SharpHsql
{
	/// <summary>
	/// Provides static methods that supply helper utilities for logging data with the ILogger object. 
	/// This class cannot be inherited. 
	/// </summary>
	/// <author>Andrés G Vettori</author>
	sealed class LogHelper
	{
		#region Constants

		private static readonly string newLine = Environment.NewLine;

		#endregion Constants

		#region Enums
		/// <summary>
		/// Specifies the event type of an log entry.
		/// </summary>
		/// <remarks>
		/// The type of an log entry is used to indicate the severity of a log entry.
		/// Each log must be of a single type, which the application indicates when it reports the log.
		/// </remarks>
		public enum LogEntryType
		{
			/// <summary>
			/// An audit log. This indicates a successful audit.
			/// </summary>
			Audit,
			/// <summary>
			/// A debug log. This is for testing and debugging operations.
			/// </summary>
			Debug,
			/// <summary>
			/// An information log. This indicates a significant, successful operation.
			/// </summary>
			Information,
			/// <summary>
			/// A warning log. 
			/// This indicates a problem that is not immediately significant, 
			/// but that may signify conditions that could cause future problems.
			/// </summary>
			Warning,
			/// <summary>
			/// An error log. 
			/// This indicates a significant problem the user should know about; 
			/// usually a loss of functionality or data.
			/// </summary>
			Error,
			/// <summary>
			/// An fatal log. 
			/// This indicates a fatal problem the user should know about; 
			/// always a loss of functionality or data.
			/// </summary>
			Fatal
		}
		#endregion

		#region Private utility methods & constructors

		//Since this class provides only static methods, make the default constructor private to prevent 
		//instances from being created with "new LogHelper()".
		private LogHelper() {}

		static LogHelper()
		{
			//Configure Serilog with default configuration if not already configured
			try
			{
				if (Serilog.Log.Logger.GetType().Name == "SilentLogger")
				{
					Serilog.Log.Logger = new LoggerConfiguration()
						.MinimumLevel.Debug()
						.WriteTo.Console()
						.WriteTo.File(
							Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", "logs", "sharphsql-.txt"),
							rollingInterval: RollingInterval.Day,
							outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
						.CreateLogger();
				}
			}
			catch
			{
				// If configuration fails, use a basic console logger
				Serilog.Log.Logger = new LoggerConfiguration()
					.WriteTo.Console()
					.CreateLogger();
			}
		}

		private static string InternalFormattedMessage(string? message, Exception? exception, Assembly assembly)
		{
			return InternalFormattedMessage(message, exception, assembly, true);
		}

		private static string InternalFormattedMessage(string? message, Exception? exception, Assembly assembly, bool showStack)
		{
			const string TEXT_SEPARATOR = "*********************************************";			

			// Create StringBuilder to maintain publishing information.
			StringBuilder strInfo = new StringBuilder(String.Concat(newLine, newLine, message ?? string.Empty, newLine, newLine));

			try
			{
				if (exception != null)
				{
					#region Loop through each exception class in the chain of exception objects
					// Loop through each exception class in the chain of exception objects.
				
					if(message == null) message = exception.Message;

					Exception? currentException = exception; // Temp variable to hold Exception object during the loop.

					int intExceptionCount = 1;// Count variable to track the number of exceptions in the chain.
					
					do
					{
						// Write title information for the exception object.
						strInfo.AppendFormat("{1}) Exception Information{0}{2}", newLine, intExceptionCount.ToString(System.Globalization.CultureInfo.InvariantCulture), TEXT_SEPARATOR);
						strInfo.AppendFormat("{0}Exception Type: {1}", newLine, currentException.GetType().FullName);
				
						#region Loop through the public properties of the exception object and record their value
						// Loop through the public properties of the exception object and record their value.
						PropertyInfo[] aryPublicProperties = currentException.GetType().GetProperties();
						foreach (PropertyInfo p in aryPublicProperties)
						{
							// Do not log information for the InnerException or StackTrace. This information is 
							// captured later in the process.
							if (!p.Name.Equals("InnerException") && !p.Name.Equals("StackTrace") && !p.Name.Equals("BaseInnerException"))
							{
								object? prop = null;
								try
								{
									prop = p.GetValue(currentException, null);
								}
								catch(TargetInvocationException) {}

								if (prop == null)
								{
									strInfo.AppendFormat("{0}{1}: NULL", newLine, p.Name);
								}
								else
								{
									strInfo.AppendFormat("{0}{1}: {2}", newLine, p.Name, prop);
								}
							}
						}
						#endregion

						#region Record the Exception StackTrace

						// Record the StackTrace with separate label.
						if (showStack && currentException.StackTrace != null)
						{
							strInfo.AppendFormat("{0}{0}StackTrace Information{0}{1}", newLine, TEXT_SEPARATOR);
							strInfo.AppendFormat("{0}{1}", newLine, currentException.StackTrace);
						}
						#endregion

						strInfo.AppendFormat("{0}{0}", newLine);

						// Reset the temp exception object and iterate the counter.
						currentException = currentException.InnerException;
						intExceptionCount++;
					} while (currentException != null);
					#endregion
				}

				strInfo.AppendFormat("{1}Assembly version: {0}{1}RuntimeVersion: {2}{1}Compilation: {3}{1}Assembly file version: {4}", 
					assembly.GetName().Version?.ToString() ?? "Unknown",
					newLine,
					assembly.ImageRuntimeVersion,
					ReflexHelper.GetAssemblyConfiguration(assembly),
					ReflexHelper.GetAssemblyFileVersion(assembly));
			}
			catch (Exception ex)
			{
				strInfo.AppendFormat("{0}{0}Exception in PublishException:{4}{0}{1}{0}Original message:{0}{2}{0}Original Exception:{0}{3}", 
					newLine, ex.Message, message, exception?.Message ?? "null", TEXT_SEPARATOR);
			}
			return strInfo.ToString();
		}

		private static void PublishInternal(string? message, Exception? exception, LogEntryType exceptionType)
		{
			//Using the StackTrace object may be tricky on release builds because of inlining optimizations.
			StackTrace stackTrace = new StackTrace();
			var methodBase = stackTrace.GetFrame(2)?.GetMethod();
			if (methodBase == null)
			{
				// Fallback if method info is not available
				Serilog.Log.Write(MapLogLevel(exceptionType), message ?? exception?.Message ?? "Unknown error");
				return;
			}

			MemberInfo prevMethodInfo = (MemberInfo)methodBase;
			Type? callertype = prevMethodInfo.ReflectedType;

			if (callertype == null)
			{
				// Fallback if type info is not available
				Serilog.Log.Write(MapLogLevel(exceptionType), InternalFormattedMessage(message, exception, Assembly.GetExecutingAssembly()));
				return;
			}

			var formattedMessage = InternalFormattedMessage(message, exception, callertype.Assembly);
			var logLevel = MapLogLevel(exceptionType);

			// Create a logger with context
			var logger = Serilog.Log.ForContext("SourceContext", callertype.FullName ?? "Unknown");

			switch(exceptionType)
			{
				case LogEntryType.Audit:
					logger.Write(LogEventLevel.Information, "[AUDIT] {Message}", formattedMessage);
					break;					
				case LogEntryType.Error:
					if (exception != null)
						logger.Error(exception, "{Message}", formattedMessage);
					else
						logger.Error("{Message}", formattedMessage);
					break;
				case LogEntryType.Fatal:
					if (exception != null)
						logger.Fatal(exception, "{Message}", formattedMessage);
					else
						logger.Fatal("{Message}", formattedMessage);
					break;
				case LogEntryType.Warning:
					logger.Warning("{Message}", formattedMessage);
					break;
				case LogEntryType.Debug:
					logger.Debug("{Message}", formattedMessage);
					break;
				default:
					logger.Information("{Message}", formattedMessage);
					break;
			}
		}

		private static LogEventLevel MapLogLevel(LogEntryType entryType)
		{
			return entryType switch
			{
				LogEntryType.Audit => LogEventLevel.Information,
				LogEntryType.Debug => LogEventLevel.Debug,
				LogEntryType.Information => LogEventLevel.Information,
				LogEntryType.Warning => LogEventLevel.Warning,
				LogEntryType.Error => LogEventLevel.Error,
				LogEntryType.Fatal => LogEventLevel.Fatal,
				_ => LogEventLevel.Information
			};
		}

		#endregion

		#region Public members

		#region Publish
		/// <summary>
		/// Write Exception Info to the ILogger interface.
		/// </summary>
		/// <remarks>
		/// For Debugging or Information uses, it's faster to use ILogger 
		/// interface directly, instead of this method. 
		/// </remarks>
		/// <param name="message">Additional exception info.</param>
		public static void Publish(string message)
		{
			PublishInternal(message, null, LogEntryType.Information);
		}

		/// <summary>
		/// Write Exception Info to the ILogger interface.
		/// </summary>
		/// <param name="exception">Exception object.</param>
		public static void Publish(Exception exception)
		{
			PublishInternal(null, exception, LogEntryType.Error);
		}

		/// <summary>
		/// Write Exception Info to the ILogger interface.
		/// </summary>
		/// <param name="exception">Exception object.</param>
		/// <param name="exceptionType">See <see cref="LogEntryType"/>.</param>
		public static void Publish(Exception exception, LogEntryType exceptionType)
		{
			PublishInternal(null, exception, exceptionType);
		}

		/// <summary>
		/// Write Exception Info to the ILogger interface.
		/// </summary>
		/// <remarks>
		/// For Debugging or Information uses, it's faster to use ILogger 
		/// interface directly, instead of this method. 
		/// </remarks>
		/// <param name="message">Additional exception info.</param>
		/// <param name="exceptionType">See <see cref="LogEntryType"/>.</param>
		public static void Publish(string message, LogEntryType exceptionType)
		{
			PublishInternal(message, null, exceptionType);
		}

		/// <summary>
		/// Write Exception Info to the ILogger interface.
		/// </summary>
		/// <param name="message">Additional exception info.</param>
		/// <param name="exception">Exception object.</param>
		public static void Publish(string message, Exception exception)
		{
			PublishInternal(message, exception, LogEntryType.Error);
		}

		/// <summary>
		/// Write Exception Info to the ILogger interface.
		/// </summary>
		/// <param name="message">Additional exception info.</param>
		/// <param name="exception">Exception object.</param>
		/// <param name="exceptionType">See <see cref="LogEntryType"/>.</param>
		public static void Publish(string message, Exception exception, LogEntryType exceptionType)
		{ 
			PublishInternal(message, exception, exceptionType);
		}
		
		#endregion

		#region Logger

		/// <summary>
		/// Returns the global Serilog logger instance.
		/// </summary>
		/// <returns>The global logger instance.</returns>
		public static ILogger GetLogger()
		{
			return Serilog.Log.Logger;
		}

		/// <summary>
		/// Returns a Serilog logger with the specified source context.
		/// </summary>
		/// <param name="name">The name of the logger context.</param>
		/// <returns>A logger with the specified context.</returns>
		public static ILogger GetLogger(string name)
		{
			return Serilog.Log.ForContext("SourceContext", name);
		}

		/// <summary>
		/// Returns a Serilog logger with the specified type as context.
		/// </summary>
		/// <param name="type">The type that will be used as the name of the logger context.</param>
		/// <returns>A logger with the specified type context.</returns>
		public static ILogger GetLogger(Type type)
		{
			return Serilog.Log.ForContext(type);
		}

		/// <summary>
		/// Returns a Serilog logger with the specified type as context.
		/// </summary>
		/// <param name="domainAssembly">The assembly (for compatibility - not used in Serilog).</param>
		/// <param name="type">The type that will be used as the name of the logger context.</param>
		/// <returns>A logger with the specified type context.</returns>
		public static ILogger GetLogger(Assembly domainAssembly, Type type)
		{
			// Assembly parameter kept for API compatibility
			return Serilog.Log.ForContext(type);
		}

		/// <summary>
		/// Returns a Serilog logger with the specified source context.
		/// </summary>
		/// <param name="domainAssembly">The assembly (for compatibility - not used in Serilog).</param>
		/// <param name="name">The name of the logger context.</param>
		/// <returns>A logger with the specified context.</returns>
		public static ILogger GetLogger(Assembly domainAssembly, string name)
		{
			// Assembly parameter kept for API compatibility
			return Serilog.Log.ForContext("SourceContext", name);
		}

		#endregion

		#region FormattedMessage
		/// <summary>
		/// Gets the Exception Info to be written to the Log.
		/// </summary>
		/// <param name="exception">Exception object.</param>
		public static string FormattedMessage(Exception exception)
		{
			return InternalFormattedMessage(null, exception, Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Gets the Exception Info to be written to the Log.
		/// </summary>
		/// <param name="exception">Exception object.</param>
		/// <param name="showStack">True, show all the stack trace info.</param>
		public static string FormattedMessage(Exception exception, bool showStack)
		{
			return InternalFormattedMessage(null, exception, Assembly.GetCallingAssembly(), showStack);
		}

		/// <summary>
		/// Gets the Exception Info to be written to the Log.
		/// </summary>
		/// <param name="message">Additional exception info.</param>
		/// <param name="exception">Exception object.</param>
		public static string FormattedMessage(string message, Exception exception)
		{
			return InternalFormattedMessage(message, exception, Assembly.GetCallingAssembly());
		}

		/// <summary>
		/// Gets the Exception Info to be written to the Log.
		/// </summary>
		/// <param name="message">Additional exception info.</param>
		/// <param name="exception">Exception object.</param>
		/// <param name="showStack">True, show all the stack trace info.</param>
		public static string FormattedMessage(string message, Exception exception, bool showStack)
		{
			return InternalFormattedMessage(message, exception, Assembly.GetCallingAssembly(), showStack);
		}

		#endregion

		#endregion
	}
}
