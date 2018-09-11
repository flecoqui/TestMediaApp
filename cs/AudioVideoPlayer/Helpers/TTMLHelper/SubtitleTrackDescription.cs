using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioVideoPlayer.Pages.Player;

namespace AudioVideoPlayer.Helpers.TTMLHelper
{
    public class SubtitleTrackDescription
    {
        public string Name { get; set; }
        public string Language { get; set; }
        public string SubType { get; set; }
        public string StreamIndexContent { get; set; }
        public string SubtitleUri { get; set; }
        public ulong SubtitleIndex { get; set; }
        public int Bitrate { get; set; }
        public int PeriodMs { get; set; }
        public Windows.Media.Core.TimedMetadataTrack SubtitleTrack { get; set; }

        public SubtitleTrackDescription(string name, string lang, int bitrate, int period)
        {
            Name = name;
            Language = lang;
            Bitrate = bitrate;
            PeriodMs = period;
            SubtitleTrack = new Windows.Media.Core.TimedMetadataTrack(name, lang, Windows.Media.Core.TimedMetadataKind.Caption);

        }
    }
}
