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
stop, -s                    : stop the server
view, -v <element> [mode]   : see an element
                        elements :
                            - devicesid, modes :
                                - all : see all devices
                                - input : only inputs
                                - output : only outputs
                            - devicesname, modes :
                                - all : see all devices
                                - input : only inputs
                                - output : only outputs
                            - mxsize : max size of the samples buffer in kilobytes
                            - record : display the currently recording devices
record, -r <devices name>   : start recording the given devices name or id
stoprecord, -sr             : stop recording
mxsize, -xs <size in bytes> : max size of the sample to record in kilobytes
out, -o [auto] <path>              : saves the record as .wav file to the specified output. If 'auto' is added,
                                        the path is a folder and the name is auto-generated
state                       : request the state of the server. Result :
                                - recording
                                - stopped");
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