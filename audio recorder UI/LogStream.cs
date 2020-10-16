using System;
using System.IO;

namespace audio_recorder_UI
{
    public class LogStream
    {
        #region Private Fields

        private TextWriter writer;

        #endregion Private Fields

        #region Constructor

        public LogStream(string path)
        {
            writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        }

        #endregion Constructor

        #region Public Methods

        public void Close()
        {
            writer.Close();
        }

        /// <summary>
        /// Logs an object as error
        /// </summary>
        /// <param name="value">The object</param>
        public void Error(object value) => Log(value, "ERROR");

        /// <summary>
        /// Logs an object
        /// </summary>
        /// <param name="value">The object</param>
        /// <param name="status">The status, info by default</param>
        public void Log(object value, string status = "INFO")
        {
            writer.WriteLine($"[{status}] {DateTime.Now} - {value.ToString()}");
            Console.Write('[');
            switch (status)
            {
                case "ERROR":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case "INFO":
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;

                case "WARNING":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
            }
            Console.Write(status);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"] {DateTime.Now} - {value.ToString()}");
        }

        /// <summary>
        /// Logs an object as warning
        /// </summary>
        /// <param name="value"></param>
        public void Warning(object value) => Log(value, "WARNING");

        [Obsolete]
        public void WriteLine(object value, string status = "INFO") => Log(value, status);

        #endregion Public Methods
    }
}