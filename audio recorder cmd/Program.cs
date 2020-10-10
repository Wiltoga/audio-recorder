using Interprocomm;
using System;
using System.Diagnostics;
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

        #endregion Public Properties

        #region Private Methods

        private static void Main(string[] args)
        {
            bool created;
            mutex = new Mutex(true, "audioRecorder", out created);
            if (created)
            {
                mutex.ReleaseMutex();
                mutex.Close();
                Process.Start(new ProcessStartInfo("audiorecserv.exe"));
            }
            if (args.Length == 0)
            {
                Console.WriteLine(
@"Usage : audiorec [command]

commands:
stop, -s        : stop the recording server");
            }
            else
            {
                Client = new Client("audioRecorder");
                Client.Start();
                string req = "";
                for (int i = 0; i < args.Length; ++i)
                {
                    if (i > 0)
                        req += ' ';
                    req += '"' + args[i] + '"';
                }
                var response = Client.SendRequest(req);
                if (response != null)
                    Console.WriteLine(response.StringData);
            }
        }

        #endregion Private Methods
    }
}