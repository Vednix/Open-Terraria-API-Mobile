using System.Collections.Generic;
using OTA.Misc;
using System;
using System.Threading;
using System.Diagnostics;
using System.IO;
using Terraria;

namespace OTA.Logging
{
    /// <summary>
    /// ProgramLog is a very slimmed down version of a logging system and is similar to other logging libraries such as log4net.
    /// The implementation uses a single despatch thread to pass any information onto multiple registered LogTargets, known as 
    /// LogTargets. Some of the default LogTargets include:
    ///     - outputting to file using FileOutputTarget
    ///     - outputting to console
    ///     - outputting to custom LogTargets, registered in external assemblies (plugins)
    /// </summary>
    public static class ProgramLog
    {
        static readonly Queue<LogEntry> entries = new Queue<LogEntry>(1024);
        static ProgramThread dispatchThread = new ProgramThread("LogD", LogDispatchThread);
        static ProducerConsumerSignal logSignal = new ProducerConsumerSignal(false);
        static LogTarget console = new StandardOutputTarget();
        static LogTarget logFile = null;

        public static bool LogRotation { get; set; }

        static volatile bool exit = false;

        /// <summary>
        /// Indicates whether the default application log file is open
        /// </summary>
        /// <value><c>true</c> if is file open; otherwise, <c>false</c>.</value>
        public static bool IsFileOpen
        {
            get
            { return logFile != null; }
        }

        static ProgramLog()
        {
            dispatchThread.IsBackground = false;
            dispatchThread.Start();

            lock (logTargets)
                logTargets.Add(console);

            LogRotation = true;
        }

        /// <summary>
        /// Holds the default OTAPI logging facilities.
        /// </summary>
        public static class Categories
        {
            public const String User = "User";
            public const String Admin = "Admin";
            public const String Error = "Error";
            public const String Debug = "Debug";
            public const String Plugin = "Plugin";
            public const String Web = "Web";
        }

        /// <summary>
        /// Log an action that is specific to players/users. The message will be displayed in Magenta.
        /// </summary>
        public static readonly LogChannel Users = new LogChannel(Categories.User, ConsoleColor.Magenta, System.Diagnostics.TraceLevel.Info);

        /// <summary>
        /// Log an action that is specific to admins. The message will be displayed in Yellow.
        /// </summary>
        public static readonly LogChannel Admin = new LogChannel(Categories.Admin, ConsoleColor.Yellow, System.Diagnostics.TraceLevel.Info);

        /// <summary>
        /// Log an exception or error that will be displayed in Red.
        /// </summary>
        public static readonly LogChannel Error = new LogChannel(Categories.Error, ConsoleColor.Red, System.Diagnostics.TraceLevel.Error);

        /// <summary>
        /// Log a debug message in the console.
        /// </summary>
        public static readonly LogChannel Debug = new LogChannel(Categories.Debug, ConsoleColor.Cyan, System.Diagnostics.TraceLevel.Verbose)
        {
            EnableConsoleOutput = false
        };

        /// <summary>
        /// Log an action that is specific to all plugins. The message will be displayed in Blue.
        /// </summary>
        public static readonly LogChannel Plugin = new LogChannel(Categories.Plugin, ConsoleColor.Blue, System.Diagnostics.TraceLevel.Info);

        /// <summary>
        /// Log an action that is specific to web requests. The message will be displayed in Dark Blue.
        /// </summary>
        public static readonly LogChannel Web = new LogChannel(Categories.Web, ConsoleColor.DarkBlue, System.Diagnostics.TraceLevel.Info);

        struct LogEntry
        {
            public Thread thread;
            public DateTime time;
            public object message;
            public object args;
            public LogTarget target;
            public LogChannel channel;
            public ConsoleColor? color;
            public TraceLevel traceLevel;

            public LogEntry(object message, object args, TraceLevel traceLevel, ConsoleColor? color = null)
            {
                this.target = null;
                this.thread = Thread.CurrentThread;
                this.time = DateTime.Now;
                this.message = message;
                this.args = args;
                this.channel = null;
                this.color = color;
                this.traceLevel = traceLevel;
            }
        }

        static List<LogTarget> logTargets = new List<LogTarget>();

        public static void OpenLogFile(string path)
        {
            var lf = new FileOutputTarget(path, LogRotation);

            lock (logTargets)
                logTargets.Add(lf);

            Log("Logging started to file \"{0}\".", lf.FilePath);
            logFile = lf;
        }

        /// <summary>
        /// Add a custom log target receiver
        /// </summary>
        public static void AddTarget(LogTarget target)
        {
            lock (logTargets) logTargets.Add(target);
        }

        /// <summary>
        /// Remove a log target receiver
        /// </summary>
        /// <param name="target">Target.</param>
        public static void RemoveTarget(LogTarget target)
        {
            lock (logTargets) logTargets.Remove(target);
        }

        public static void Close()
        {
            exit = true;
            logSignal.Signal();
        }

        static void Write(LogEntry entry)
        {
            lock (entries)
            {
                entries.Enqueue(entry);
            }
            logSignal.Signal();
        }

        public static void BareLog(object obj)
        {
            Write(new LogEntry { message = obj.ToString(), thread = Thread.CurrentThread, traceLevel = TraceLevel.Info });
        }

        public static void BareLog(string text)
        {
            Write(new LogEntry { message = text, thread = Thread.CurrentThread, traceLevel = TraceLevel.Info });
        }

        public static void BareLog(string format, params object[] args)
        {
            Write(new LogEntry { message = format, args = args, thread = Thread.CurrentThread, traceLevel = TraceLevel.Info });
        }

        public static void BareLog(LogChannel channel, string text)
        {
            Write(new LogEntry { message = text, thread = Thread.CurrentThread, channel = channel, target = channel.Target, traceLevel = channel.Level });
        }

        public static void BareLog(LogChannel channel, string format, params object[] args)
        {
            Write(new LogEntry { message = format, args = args, thread = Thread.CurrentThread, channel = channel, target = channel.Target, traceLevel = channel.Level });
        }

        public static void Log(string text)
        {
            Write(new LogEntry(text, null, TraceLevel.Info));
        }

        public static void Log(string format, params object[] args)
        {
            Write(new LogEntry(format, args, TraceLevel.Info));
        }

        public static void Log(string format, ConsoleColor colour, params object[] args)
        {
            Write(new LogEntry(format, args, TraceLevel.Info, colour));
        }

        public static void Log(LogChannel channel, string text, bool multipleLines = false)
        {
            if (!multipleLines)
                Write(new LogEntry(text, null, channel.Level) { channel = channel, target = channel.Target });
            else
            {
                var split = text.Split('\n');

                foreach (var line in split)
                    Write(new LogEntry(line, null, channel.Level) { channel = channel, target = channel.Target });
            }
        }

        public static void Log(LogChannel channel, string format, params object[] args)
        {
            Write(new LogEntry(format, args, channel.Level) { channel = channel, target = channel.Target });
        }

        public static void Log(Exception e)
        {
            Write(new LogEntry(e, null, Error.Level) { channel = Error, target = Error.Target });
        }

        public static void Log(LogChannel channel, Exception e)
        {
            Write(new LogEntry(e, null, channel.Level) { channel = channel, target = channel.Target ?? Error.Target });
        }

        public static void Log(Exception e, string text)
        {
            Write(new LogEntry(e, text, Error.Level) { channel = Error, target = Error.Target });
        }

        public static void Log(LogChannel channel, Exception e, string text)
        {
            Write(new LogEntry(e, text, channel.Level) { channel = channel, target = channel.Target ?? Error.Target });
        }

        public static void AddProgressLogger(ProgressLogger prog)
        {
            Write(new LogEntry(prog, null, TraceLevel.Info));
        }

        public static void RemoveProgressLogger(ProgressLogger prog)
        {
            Write(new LogEntry(prog, null, TraceLevel.Info));
        }

        /// <summary>
        /// Allows you to write to the console as if you were using Console.WriteLine
        /// </summary>
        public static class Console
        {
            public static void Print(string text)
            {
                ProgramLog.Write(new LogEntry { target = console, message = text, thread = Thread.CurrentThread, traceLevel = TraceLevel.Info });
            }

            public static void Print(string text, params object[] args)
            {
                ProgramLog.Write(new LogEntry { target = console, message = String.Format(text, args), thread = Thread.CurrentThread, traceLevel = TraceLevel.Info });
            }
        }

        static int EntryCount()
        {
            lock (entries)
            {
                return entries.Count;
            }
        }

        //static Dictionary<Thread, string> poolNames = new Dictionary<Thread, string>();
        //static int nextPoolIndex = 0;

        static string GeneratePrefix(LogEntry entry, OutputEntry output)
        {
            if (entry.time != default(DateTime))
                return $"{entry.time} {output.threadName} {entry.traceLevel}> ";

            return null;
        }

        static void Build(LogEntry entry, out OutputEntry output)
        {
            Exception error = null;
            output = default(OutputEntry);

            output.consoleOutput = true;
            if (entry.channel != null)
            {
                output.color = entry.channel.Color;
                output.channelName = entry.channel.Name;
                output.traceLevel = entry.channel.Level;
                output.consoleOutput = entry.channel.EnableConsoleOutput;
            }
            else if (entry.color != null)
                output.color = entry.color.Value;

            try
            {
                if (entry.message is string)
                {
                    var text = (string)entry.message;

                    if (entry.args != null)
                    {
                        var args = (object[])entry.args;
                        try
                        {
                            text = String.Format(text, args);
                        }
                        catch (Exception)
                        {
                            text = String.Format("<Incorrect log message format string or argument list: message=\"{0}\", args=({1})>",
                                text, String.Join(", ", args));
                        }
                    }

                    output.message = text;
                }
                else if (entry.message is Exception)
                {
                    var e = (Exception)entry.message;

                    if (entry.args is string)
                        output.message = String.Format("{0}:{1}{2}", entry.args, Environment.NewLine, e.ToString());
                    else
                        output.message = e.ToString();
                }
                else
                    output.message = entry.message;

                output.threadName = "?";
                if (entry.thread != null)
                {
                    if (entry.thread.IsThreadPoolThread)
                    {
                        output.threadName = "Pool";
                        //                      string name;
                        //                      if (poolNames.TryGetValue (entry.thread, out name))
                        //                      {
                        //                          thread = name;
                        //                      }
                        //                      else
                        //                      {
                        //                          thread = String.Format ("P{0:000}", nextPoolIndex++);
                        //                          poolNames[entry.thread] = thread;
                        //                      }
                    }
                    else if (entry.thread.Name != null)
                        output.threadName = entry.thread.Name;
                }

                output.prefix = GeneratePrefix(entry, output);
            }
            catch (Exception e)
            {
                error = e;
            }

            if (error != null)
            {
                try
                {
                    SafeConsole.WriteLine("Error writing log entry:");
                    SafeConsole.WriteLine(error.ToString());
                }
                catch (Exception)
                {
                }
            }
        }

        static void Send(LogTarget target, OutputEntry output)
        {
//            if (target == null)
//            {
            lock (logTargets)
                foreach (var tar in logTargets)
                {
                    tar.Send(output);
                }
//            }
//            else
            if (target != null)
                target.Send(output);
        }

        public const int LOG_THREAD_BATCH_SIZE = 64;

        static void LogDispatchThread()
        {
            try
            {
                var list = new LogEntry[LOG_THREAD_BATCH_SIZE];
                var progs = new List<ProgressLogger>();
                var last = default(OutputEntry);
                var run = 0;

                while (exit == false || EntryCount() > 0)
                {
                    int items = 0;

                    lock (entries)
                    {
                        while (entries.Count > 0)
                        {
                            list[items++] = entries.Dequeue();
                            if (items == LOG_THREAD_BATCH_SIZE) break;
                        }
                    }

                    if (items == 0)
                    {
                        if (exit)
                            break;
                        else
                            logSignal.WaitForIt();
                    }

                    for (int i = 0; i < items; i++)
                    {
                        var entry = list[i];
                        list[i] = default(LogEntry);
                        OutputEntry output;

                        Build(entry, out output);

                        if (entry.message is ProgressLogger)
                        {
                            var prog = (ProgressLogger)entry.message;

                            if (progs.Remove(prog))
                            {
                                // it's done
                                output.arg = -2;
                            }
                            else
                            {
                                // new one
                                progs.Add(prog);
                                output.arg = -1;
                            }
                        }
                        else
                        {
                            // force updates of progress loggers in the same thread
                            foreach (var prog in progs)
                            {
                                if (prog.Thread == entry.thread)
                                {
                                    var upd = new OutputEntry { prefix = output.prefix, message = prog, arg = prog.Value };
                                    Send(entry.target, upd);
                                    last = upd;
                                    run = 0;
                                }
                            }
                        }

                        if (output.message.Equals(last.message) && output.prefix == last.prefix && output.arg == last.arg)
                        {
                            run += 1;
                            //System.ProgramLog.Log (run);
                        }
                        else if (run > 0)
                        {
                            //System.ProgramLog.Log ("sending");
                            last.message = String.Format("Log message repeated {0} times", run);
                            Send(entry.target, last);
                            last = output;
                            run = 0;
                            Send(entry.target, output);
                        }
                        else
                        {
                            last = output;
                            Send(entry.target, output);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SafeConsole.WriteLine(e.ToString());
            }

            lock (logTargets)
                foreach (var tar in logTargets)
                {
                    tar.Close();
                }

            #if Full_API
            //If the log is closed then we are exiting, or there is an issue.
            Terraria.Netplay.disconnect = true;
            #endif
        }
    }
}
