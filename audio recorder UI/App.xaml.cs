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
        #region Public Fields

        public static LogStream logstream;

        #endregion Public Fields

        #region Private Fields

        private Mutex mutex;
        private Mutex uiMutex;

        #endregion Private Fields

        #region Public Constructors

        public App()
        {
            try
            {
                bool created;
                uiMutex = new Mutex(true, "audioRecorderUI", out created);
                if (created)
                {
                    logstream = new LogStream(Path.Combine(AppPath, "logs.log"));
                    logstream.Log("LogStream initialized");
                    UIServer = new Server("audioRecorderUI", 1);
                    UIServer.RequestRecieved += r =>
                    {
                        var code = BitConverter.ToInt32(r.Data, 0);
                        switch (code)
                        {
                            case 1:
                                Current.Dispatcher.Invoke(() =>
                                {
                                    Current.MainWindow.Visibility = Visibility.Visible;
                                    Current.MainWindow.Activate();
                                    Current.MainWindow.Topmost = true;
                                    Current.MainWindow.Topmost = false;
                                    Current.MainWindow.Focus();
                                });
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
                if (File.Exists(Path.Combine(ConfigPath)))
                    Config = JSONSerializer.Deserialize<Settings>(ConfigPath);
                else
                    Config = new Settings();

                JSONSerializer.Serialize(ConfigPath, Config);
                logstream.Log("Settings initialized");

                logstream.Log("Ready");
            }
            catch (Exception e) { logstream.Error(e); }
        }

        #endregion Public Constructors

        #region Public Properties

        public static string AppPath => Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        public static Client Client { get; private set; }
        public static Settings Config { get; set; }
        public static string ConfigPath => Path.Combine(AppPath, "config.json");
        public static string DataPath => Path.Combine(AppPath, "data");
        public static Server UIServer { get; private set; }

        #endregion Public Properties
    }
}