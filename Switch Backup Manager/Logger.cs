using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

//Original article: https://heiswayi.nrird.com/2016/creating-super-simple-logger-class-in-csharp

namespace Switch_Backup_Manager
{
    public class Logger
    {
        private readonly string datetimeFormat;
        private readonly string logFilename;
        private RichTextBox outputTextBox;

        /// <summary>
        /// Initiate an instance of SimpleLogger class constructor.
        /// If log file does not exist, it will be created automatically.
        /// </summary>
        public Logger()
        {
            datetimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
            logFilename = Util.LOG_FILE;

            // Log file header line
            string logHeader = logFilename + " is created.";
            if (!System.IO.File.Exists(logFilename))
            {
                WriteLine(System.DateTime.Now.ToString(datetimeFormat) + " " + logHeader, false);
            }
        }

        public Logger(ref RichTextBox output)
            :this()
        {
            this.outputTextBox = output;
        }

        /// <summary>
        /// Log a DEBUG message
        /// </summary>
        /// <param name="text">Message</param>
        public void Debug(string text)
        {
            if (Util.log_Level == "debug") {
                WriteFormattedLog(LogLevel.DEBUG, text);
            }            
        }

        /// <summary>
        /// Log an ERROR message
        /// </summary>
        /// <param name="text">Message</param>
        public void Error(string text)
        {
            WriteFormattedLog(LogLevel.ERROR, text);
        }

        /// <summary>
        /// Log a FATAL ERROR message
        /// </summary>
        /// <param name="text">Message</param>
        public void Fatal(string text)
        {
            WriteFormattedLog(LogLevel.FATAL, text);
        }

        /// <summary>
        /// Log an INFO message
        /// </summary>
        /// <param name="text">Message</param>
        public void Info(string text)
        {
            WriteFormattedLog(LogLevel.INFO, text);
        }

        /// <summary>
        /// Log a TRACE message
        /// </summary>
        /// <param name="text">Message</param>
        public void Trace(string text)
        {
            WriteFormattedLog(LogLevel.TRACE, text);
        }

        /// <summary>
        /// Log a WARNING message
        /// </summary>
        /// <param name="text">Message</param>
        public void Warning(string text)
        {
            WriteFormattedLog(LogLevel.WARNING, text);
        }

        private void WriteFormattedLog(LogLevel level, string text)
        {
            string pretext;
            switch (level)
            {
                case LogLevel.TRACE:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [TRACE]   ";
                    break;
                case LogLevel.INFO:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [INFO]    ";
                    break;
                case LogLevel.DEBUG:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [DEBUG]   ";
                    break;
                case LogLevel.WARNING:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [WARNING] ";
                    break;
                case LogLevel.ERROR:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [ERROR]   ";
                    break;
                case LogLevel.FATAL:
                    pretext = System.DateTime.Now.ToString(datetimeFormat) + " [FATAL]   ";
                    break;
                default:
                    pretext = "";
                    break;
            }

            WriteLine(pretext + text);
        }

        delegate void OutputMethodDelegate(string text, bool append = true);
        private void WriteLine(string text, bool append = true)
        {
            try
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(logFilename, append, System.Text.Encoding.UTF8))
                {
                    if (text != "")
                    {
                        writer.WriteLine(text);
                        Color color = this.outputTextBox.ForeColor;

                        if (text.Contains("[DEBUG]"))
                        {
                            color = Color.DarkGreen;
                        } else if (text.Contains("[ERROR]"))
                        {
                            color = Color.DarkRed;
                        } else if (text.Contains("[WARNING]"))
                        {
                            color = Color.IndianRed;
                        }
                        
                        this.outputTextBox.AppendText("\n" + text, color);
                    }
                }
            }
            catch (Exception e)
            {
                string ex = e.StackTrace;
            }
        }

        [System.Flags]
        private enum LogLevel
        {
            TRACE,
            INFO,
            DEBUG,
            WARNING,
            ERROR,
            FATAL
        }
    }
}
