using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioVideoPlayer.DLNA
{
    public class DLNATransportInfo
    {
        public string CurrentTransportState { get; set; }
        public string CurrentTransportStatus { get; set; }
        public string CurrentSpeed { get; set; }
    }
}
