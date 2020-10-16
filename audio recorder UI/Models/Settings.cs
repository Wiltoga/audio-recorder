using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace audio_recorder_UI.Models
{
    public class Settings
    {
        public List<string> RecordDevices { get; set; }

        public TimeSpan TimeToRecord { get; set; }

        [JsonProperty("SavePath")]
        private string _savepath;

        [JsonIgnore]
        public string SavePath
        {
            get
            {
                if (Directory.Exists(App.dataPath))
                    return App.dataPath;
                return _savepath;
            }
            set
            {
                if (Directory.Exists(App.dataPath))
                    _savepath = "";
                else
                    _savepath = value;
            }
        }

        public Settings()
        {
            RecordDevices = new List<string>();
            // TimeToRecord = 32Mo;
        }
    }
}