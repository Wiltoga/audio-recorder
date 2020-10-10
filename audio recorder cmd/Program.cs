using Interprocomm;
using System;
using System.Threading;

namespace audioRecorderCmd
{
    internal class Program
    {
        #region Private Fields

        private static Mutex mutex;

        #endregion Private Fields

        #region Public Properties

        public static Client Client { get; private set; }
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

                await Server.Start();
            }
            else
            {
                Client = new Client("audioRecorder");
                Client.Start();
            }
        }

        #endregion Private Methods
    }
}