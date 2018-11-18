using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioVideoPlayer.DLNA
{
    public class DLNAMediaInfo
    {
        public int  NrTrack { get; set; }
        public TimeSpan MediaDuration { get; set; }
        public string CurrentUri { get; set; }
        public string CurrentUriMetaData { get; set; }
        public string NextUri { get; set; }
        public string NextUriMetaData { get; set; }
    }
}
