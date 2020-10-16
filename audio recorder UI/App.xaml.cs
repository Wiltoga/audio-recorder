using Interprocomm;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using audio_recorder_UI.Models;

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
            try
            {
                logstream.Log("LogStream initialized");
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
                        Process.Start(new ProcessStartInfo("audiorecserv.exe"));
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

                // Reading or creating the settings
                if (File.Exists(Path.Combine(configPath)))
                    Config = JSONSerializer.Deserialize<Settings>(configPath);
                else
                    Config = new Settings();

                JSONSerializer.Serialize(configPath, Config);
                logstream.Log("Settings initialized");

                logstream.Log("Ready");
            }
            catch (Exception e) { logstream.Error(e); }
        }

        #endregion Public Constructors

        #region Public Properties

        public Client Client { get; private set; }
        public Server UIServer { get; private set; }

        public static readonly string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        public static readonly string configPath = Path.Combine(appPath, "config.json");

        public static LogStream logstream = new LogStream(Path.Combine(appPath, "logs.log"));
        public static Settings Config { get; set; }

        #endregion Public Properties
    }
}