using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioVideoPlayer.DLNA
{
    public class DLNAMediaPosition
    {
        public int  Track { get; set; }
        public TimeSpan TrackDuration { get; set; }
        public string TrackUri { get; set; }
        public TimeSpan RelTime { get; set; }
        public TimeSpan AbsTime { get; set; }
    }
}
