using Interprocomm;
using System;
using System.Collections.Generic;
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
                Server.RequestRecieved += r =>
                {
                    string command;
                    string[] arguments;
                    {
                        var split = splitCommand(r.StringData);
                        command = split[0];
                        arguments = split.Where((_, i) => i > 0).ToArray();
                    }
                    //TODO
                };
                await Server.Start();
            }
        }

        private static string[] splitCommand(string cmd)
        {
            var list = new List<string>();
            int skipBlank(ref int index, string str)
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