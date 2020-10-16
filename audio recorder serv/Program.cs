using Interprocomm;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
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

        public static List<(WasapiCapture, MMDevice, TemporaryStream)> Recorders { get; private set; }
        public static bool Recording { get; set; }
        public static Server Server { get; private set; }

        #endregion Public Properties

        #region Private Methods

        private static async System.Threading.Tasks.Task Main(string[] args)
        {
            bool created;
            mutex = new Mutex(true, "audioRecorder", out created);
            if (created)
            {
                Recording = false;
                Server = new Server("audioRecorder", 2);
                int maxBuff = 32000;
                Recorders = new List<(WasapiCapture, MMDevice, TemporaryStream)>();
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
                                                    result += item.ID + '|' + item.FriendlyName + '|' + item.AudioClient.MixFormat.AverageBytesPerSecond;
                                                }
                                                r.Respond(result);
                                                break;

                                            case "input":
                                                result = "";
                                                foreach (var item in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                                                {
                                                    if (result != "")
                                                        result += "\n";
                                                    result += item.ID + '|' + item.FriendlyName + '|' + item.AudioClient.MixFormat.AverageBytesPerSecond;
                                                }
                                                r.Respond(result);
                                                break;

                                            case "output":
                                                result = "";
                                                foreach (var item in new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                                                {
                                                    if (result != "")
                                                        result += "\n";
                                                    result += item.ID + '|' + item.FriendlyName + '|' + item.AudioClient.MixFormat.AverageBytesPerSecond;
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
                                if (!Recording)
                                {
                                    var devices = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
                                    foreach (var item in arguments)
                                    {
                                        var device = devices.FirstOrDefault(d => d.ID == item || d.FriendlyName == item);
                                        if (device == null)
                                        {
                                            r.Respond("No device named '" + item + "' found.");
                                            return;
                                        }
                                        else
                                        {
                                            var stream = new TemporaryStream(maxBuff);
                                            WasapiCapture recorder;
                                            if (device.DataFlow == DataFlow.Capture)
                                                recorder = new WasapiCapture(device);
                                            else
                                                recorder = new WasapiLoopbackCapture(device);
                                            recorder.DataAvailable += (sender, e) => stream.Write(e.Buffer, 0, e.BytesRecorded);
                                            Recorders.Add((recorder, device, stream));
                                        }
                                    }
                                    foreach (var item in Recorders)
                                        try
                                        {
                                            item.Item1.StartRecording();
                                        }
                                        catch (Exception)
                                        {
                                            Recorders.Clear();
                                            r.Respond("Unable to record the device named '" + item.Item2.FriendlyName + "'");
                                            return;
                                        }
                                    Recording = true;
                                }
                                else
                                    r.Respond("The server is already recording");
                                break;

                            case "stoprecord":
                            case "-sr":
                                if (Recording)
                                {
                                    foreach (var item in Recorders)
                                    {
                                        item.Item1.RecordingStopped += (sender, e) =>
                                        {
                                            item.Item1.Dispose();
                                            item.Item3.Dispose();
                                            item.Item3.Close();
                                        };
                                        item.Item1.StopRecording();
                                    }
                                    Recorders.Clear();
                                    Recording = false;
                                }
                                break;

                            case "out":
                            case "-o":
                                if (Recording)
                                {
                                    if (arguments.Length == 0)
                                    {
                                        r.Respond("Invalid command");
                                        return;
                                    }
                                    int n = Recorders.Count;
                                    foreach (var item in Recorders)
                                    {
                                        item.Item1.RecordingStopped += (sender, e) => n--;
                                        item.Item1.StopRecording();
                                    }
                                    while (n > 0)
                                        Thread.Sleep(50);
                                    var mixer = new MixingSampleProvider(new WasapiLoopbackCapture().WaveFormat);
                                    foreach (var rec in Recorders)
                                    {
                                        rec.Item3.Seek(0, SeekOrigin.Begin);
                                        mixer.AddMixerInput(new RawSourceWaveStream(rec.Item3, rec.Item1.WaveFormat));
                                    }
                                    WaveFileWriter.CreateWaveFile16(arguments[0], mixer);
                                    var tmpList = new List<(WasapiCapture, MMDevice, TemporaryStream)>();
                                    foreach (var item in Recorders)
                                    {
                                        item.Item1.Dispose();
                                        var device = item.Item2;
                                        var stream = new TemporaryStream(maxBuff);
                                        WasapiCapture recorder;
                                        if (device.DataFlow == DataFlow.Capture)
                                            recorder = new WasapiCapture(device);
                                        else
                                            recorder = new WasapiLoopbackCapture(device);
                                        recorder.DataAvailable += (sender, e) => stream.Write(e.Buffer, 0, e.BytesRecorded);
                                        tmpList.Add((recorder, device, stream));
                                    }
                                    Recorders.Clear();
                                    Recorders.AddRange(tmpList);
                                    foreach (var item in Recorders)
                                        item.Item1.StartRecording();
                                }
                                else
                                    r.Respond("The server is not recording");
                                break;

                            case "state":
                                r.Respond(Recording ? "recording" : "stopped");
                                break;

                            case "mxsize":
                            case "-xs":
                                if (Recording)
                                    r.Respond("Unable to change the size when recording");
                                else if (arguments.Length > 0)
                                    maxBuff = int.Parse(arguments[0]);
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