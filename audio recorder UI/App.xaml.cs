using Interprocomm;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace audio_recorder_UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Private Fields

        private Mutex mutex;
        private Mutex uiMutex;

        #endregion Private Fields

        #region Public Constructors

        public App()
        {
            bool created;
            uiMutex = new Mutex(true, "audioRecorderUI", out created);
            if (created)
            {
                UIServer = new Server("audioRecorderUI", 1);
                UIServer.RequestRecieved += r =>
                {
                    var code = BitConverter.ToInt32(r.Data, 0);
                    switch (code)
                    {
                        case 1:
                            //wake up app
                            break;
                    }
                };
                mutex = new Mutex(true, "audioRecorder", out created);
                if (created)
                {
                    mutex.ReleaseMutex();
                    mutex.Close();
                    Process.Start(new ProcessStartInfo("audiorec.exe"));
                }
                Client = new Client("audioRecorder");
                Client.Start();
                _ = UIServer.Start();
            }
            else
            {
                Client uiClient = new Client("audioRecorderUI");
                uiClient.Start();
                uiClient.SendRequest(BitConverter.GetBytes(1));
                Environment.Exit(0);
            }
        }

        #endregion Public Constructors

        #region Public Properties

        public Client Client { get; private set; }
        public Server UIServer { get; private set; }

        #endregion Public Properties
    }
}