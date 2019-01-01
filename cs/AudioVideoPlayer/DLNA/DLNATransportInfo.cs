using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioVideoPlayer.DLNA
{
    public class DLNAMediaTransportInformation
    {
        public string CurrentTransportState { get; set; }
        public string CurrentTransportStatus { get; set; }
        public int CurrentSpeed { get; set; }
    }
}
