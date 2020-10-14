using Interprocomm;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                long maxBuff = 32000000;
                Server.RequestRecieved += r =>
                {
                    try
                    {
                        string command;
                        string[] arguments;
                        {
                            var split = SplitCommand(r.StringData);
                            command = split[0];
                            arguments = split.Where((_, i) => i > 0).ToArray();
                        }
                        switch (command)
                        {
                            case "-s":
                            case "stop":
                                Environment.Exit(0);
                                break;

                            case "view":
                            case "-v":
                                switch (arguments.Length > 0 ? arguments[0] : "")
                                {
                                    case "devices":
                                        switch (arguments.Length > 1 ? arguments[1] : "")
                                        {
                                            case "all":
                                                var result = "";
                                                foreach (var item in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
                                                {
                                                    if (result != "")
                                                        result += "\n";
                                                    result += item.DeviceFriendlyName + " : " + (item.DataFlow == DataFlow.Capture ? "input" : "output");
                                                }
                                                r.Respond(result);
                                                break;

                                            case "input":
                                                result = "";
                                                foreach (var item in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                                                {
                                                    if (result != "")
                                                        result += "\n";
                                                    result += item.DeviceFriendlyName;
                                                }
                                                r.Respond(result);
                                                break;

                                            case "output":
                                                result = "";
                                                foreach (var item in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                                                {
                                                    if (result != "")
                                                        result += "\n";
                                                    result += item.DeviceFriendlyName;
                                                }
                                                r.Respond(result);
                                                break;

                                            default:
                                                r.Respond("Invalid command");
                                                break;
                                        }
                                        break;

                                    case "mxsize":
                                        r.Respond(maxBuff.ToString());
                                        break;

                                    default:
                                        r.Respond("Invalid command");
                                        break;
                                }
                                break;

                            case "record":
                            case "-r":
                                break;

                            case "mxsize":
                            case "-xs":
                                if (arguments.Length > 0)
                                    maxBuff = long.Parse(arguments[0]);
                                break;

                            default:
                                r.Respond("Unknown command '" + command + "'");
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        r.Respond(e.ToString());
                    }
                };
                await Server.Start();
            }
        }

        private static string[] SplitCommand(string cmd)
        {
            var list = new List<string>();
            static int skipBlank(ref int index, string str)
            {
                int skipped = 0;
                while (str.Length > index && char.IsWhiteSpace(str[index]))
                {
                    skipped++;
                    index++;
                }
                return skipped;
            }
            int i = 0;
            bool insideQuotes = false;
            string currStr = "";
            while (i < cmd.Length)
            {
                if (!insideQuotes)
                {
                    if (skipBlank(ref i, cmd) > 0)
                    {
                        if (currStr != "")
                            list.Add(currStr);
                        currStr = "";
                    }
                }
                if (i >= cmd.Length)
                    break;
                var currChar = cmd[i];
                if (currChar == '"')
                    insideQuotes = !insideQuotes;
                else
                    currStr += currChar;
                ++i;
            }
            if (currStr != "")
                list.Add(currStr);
            return list.ToArray();
        }

        #endregion Private Methods
    }
}