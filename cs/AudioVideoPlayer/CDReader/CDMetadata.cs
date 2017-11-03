using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace AudioVideoPlayer.CDReader
{
    public class CDMetadata
    {
        // Type = 0
        public string AlbumTitle { get; set; }
        // Type = 1
        public string Artist { get; set; }
        // Type = 5
        public string Message { get; set; }
        // Type = 7
        public string Genre { get; set; }
        // Type = 6
        public string DiscID { get; set; }
        // Type = 14
        public string ISrc { get; set; }

        public string albumArtUrl { get; set; }

        public List<CDTrackMetadata> Tracks { get; set; }

        public CDMetadata()
        {
            Tracks = new List<CDTrackMetadata>();
        }
    }

    #region DiscID

    [DataContract]
    public class TextRepresentation
    {
        [DataMember]
        public string script { get; set; }
        [DataMember]
        public string language { get; set; }
    }
    [DataContract]
    public class Recording
    {
        [DataMember]
        public bool video { get; set; }
        [DataMember]
        public int length { get; set; }
        [DataMember]
        public string title { get; set; }
        [DataMember]
        public string disambiguation { get; set; }
        [DataMember]
        public string id { get; set; }
    }
    [DataContract]
    public class Track
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public int position { get; set; }
        [DataMember]
        public int length { get; set; }
        [DataMember]
        public string title { get; set; }
        [DataMember]
        public Recording recording { get; set; }
        [DataMember]
        public string number { get; set; }
    }
    public class Disc
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public List<int> offsets { get; set; }
        [DataMember]
        public int sectors { get; set; }
        [DataMember(Name = "offset-count")]
        public int offsetcount { get; set; }
    }
    [DataContract]
    public class Medium
    {
        [DataMember]
        public List<Track> tracks { get; set; }
        [DataMember(Name = "track-count")]
        public int trackcount { get; set; }
        [DataMember]
        public int position { get; set; }
        [DataMember]
        public string title { get; set; }
        [DataMember]
        public string format { get; set; }
        [DataMember(Name = "track-offset")]
        public int trackoffset { get; set; }
        [DataMember(Name = "format-id")]
        public string formatid { get; set; }
        [DataMember]
        public List<Disc> discs { get; set; }
    }


    [DataContract]

    public class Area
    {
        [DataMember]
        public string name { get; set; }
        [DataMember(Name = "sort-name")]
        public string sortname { get; set; }
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string disambiguation { get; set; }
        [DataMember(Name = "iso-3166-1-codes")]
        public List<string> iso31661codes { get; set; }
    }

    [DataContract]
    public class ReleaseEvent
    {
        [DataMember]
        public Area area { get; set; }
        [DataMember]
        public string date { get; set; }
    }
    [DataContract]
    public class Artist
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string disambiguation { get; set; }
        [DataMember(Name = "sort-name")]
        public string sortname { get; set; }
        [DataMember]
        public string name { get; set; }
    }
    [DataContract]
    public class ArtistCredit
{
        [DataMember]
        public Artist artist { get; set; }
        [DataMember]
        public string joinphrase { get; set; }
        [DataMember]
        public string name { get; set; }
    }
    [DataContract]
    public class CoverArtArchive
    {
        [DataMember]
        public bool darkened { get; set; }
        [DataMember]
        public bool artwork { get; set; }
        [DataMember]
        public bool front { get; set; }
        [DataMember]
        public bool back { get; set; }
        [DataMember]
        public int count { get; set; }
    }
    [DataContract]
    public class Release
    {
        [DataMember]
        public string quality { get; set; }
        [DataMember(Name = "packaging-id")]
        public string packagingid { get; set; }
        [DataMember]
        public string country { get; set; }
        [DataMember]
        public string status { get; set; }
        [DataMember]
        public string date { get; set; }
        [DataMember]
        public string title { get; set; }
        [DataMember]
        public string barcode { get; set; }
        [DataMember]
        public string asin { get; set; }
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string packaging { get; set; }
        [DataMember(Name = "text-representation")]
        public TextRepresentation textrepresentation { get; set; }
        [DataMember]
        public List<Medium> media { get; set; }
        [DataMember(Name = "release-events")]
        public List<ReleaseEvent> releaseevents { get; set; }
        [DataMember]
        public string disambiguation { get; set; }
        [DataMember(Name = "status-id")]
        public string statusid { get; set; }
        [DataMember(Name = "artist-credit")]
        public List<ArtistCredit> artistcredit { get; set; }
        [DataMember(Name = "coverart-archive")]
        public CoverArtArchive coverartarchive { get; set; }
    }
    [DataContract]
    public class DiscIDObject
    {
        
        [DataMember(Name = "offset-count")]
        public int offsetcount { get; set; }
        [DataMember]
        public int sectors { get; set; }
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public List<int> offsets { get; set; }
        [DataMember]
        public List<Release> releases { get; set; }
    }
    #endregion


    #region AlbumArt
    [DataContract]
    public class Thumbnails
    {
        [DataMember]
        public string large { get; set; }
        [DataMember]
        public string small { get; set; }
    }
    [DataContract]
    public class Image
    {
        [DataMember]
        public List<string> types { get; set; }
        [DataMember]
        public bool front { get; set; }
        [DataMember]
        public bool back { get; set; }
        [DataMember]
        public int edit { get; set; }
        [DataMember]
        public string image { get; set; }
        [DataMember]
        public string comment { get; set; }
        [DataMember]
        public bool approved { get; set; }
        [DataMember]
        public Thumbnails thumbnails { get; set; }
        [DataMember]
        public string id { get; set; }
    }
    [DataContract]
    public class AlbumArtObject
    {
        [DataMember]
        public List<Image> images { get; set; }
        [DataMember]
        public string release { get; set; }
    }
    #endregion
}
