using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace audio_recorder_UI.Models
{
    public class Settings
    {
        //TODO: Change type of OutputDevice
        public object OutputDevice { get; set; }

        public TimeSpan TimeToRecord { get; set; }
        public string SavePath { get; set; }
    }
}