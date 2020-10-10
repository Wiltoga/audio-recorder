using Interprocomm;
using System;
using System.Threading;

namespace audioRecorderServ
{
    internal class Program
    {
        #region Private Fields

        private static Mutex mutex;

        #endregion Private Fields

        #region Public Properties

        public static Server Server { get; private set; }

        #endregion Public Properties

        #region Private Methods

        private static async System.Threading.Tasks.Task Main(string[] args)
        {
            bool created;
            mutex = new Mutex(true, "audioRecorder", out created);
            if (created)
            {
                Server = new Server("audioRecorder", 2);
                Server.RequestRecieved += r => Console.WriteLine(r.StringData); //debug
                await Server.Start();
            }
        }

        #endregion Private Methods
    }
}