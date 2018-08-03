using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioVideoPlayer.Pages.Player;

namespace AudioVideoPlayer.Subtitle
{
    public class SubtitleDescription
    {
        public string Name { get; set; }
        public string Language { get; set; }
        public string SubType { get; set; }
        public string StreamIndexContent { get; set; }
        public string SubtitleUri { get; set; }
        public ulong SubtitleIndex { get; set; }
        public Windows.Media.Core.TimedTextSource SubtitleSource { get; set; }
        //public Windows.Storage.Streams.InMemoryRandomAccessStream SubtitleStream { get; set; }
        public SubtitleTTMLStream SubtitleStream { get; set; }
        public Windows.Media.Core.TimedMetadataTrack SubtitleTrack { get; set; }
    }
}
