using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioVideoPlayer.CDReader
{
    public class CDTrackMetadata
    {
        public int Number { get; set; }
        // Type = 0
        public string Title { get; set; }
        // Type = 14
        public string Poster { get; set; }
        public string ISrc { get; set; }
        public TimeSpan Duration { get; set; }
        public string Album { get; set; }
        public string Artist { get; set; }
        public string DiscID { get; set; }
        public int FirstSector { get; set; }
        public int LastSector { get; set; }

        public override string ToString()
        {
            string trackname = Number.ToString("00") + "_track";
            string prefix = string.Format("Track: {0} Title: {1} Duration: {2}", Number, string.IsNullOrEmpty(Title) ? trackname : Title, Duration);
            if (!string.IsNullOrEmpty(Album) &&
                !string.IsNullOrEmpty(Artist) &&
                !string.IsNullOrEmpty(DiscID))
                prefix += string.Format(" Album: {0} Artist: {1} ID: {2}", Album, Artist, DiscID);
            return prefix;
        }

    }
}
