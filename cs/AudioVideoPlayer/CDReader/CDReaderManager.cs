using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Custom;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using Windows.Web.Http;

namespace AudioVideoPlayer.CDReader
{
    class CDReaderManager
    {
        DeviceWatcher deviceWatcher;
        List<DeviceInformation> ListDeviceInformation;
        // CD Reader constant
        const uint CD_RAW_SECTOR_SIZE = 2352;
        const uint CD_SECTOR_SIZE = 2048;

        // Summary:
        //     Raised when CD Reader is detected.
        public event TypedEventHandler<CDReaderManager, CDReaderDevice> CDReaderDeviceAdded;
        // Summary:
        //     Raised when CD Reader is removed.
        public event TypedEventHandler<CDReaderManager, CDReaderDevice> CDReaderDeviceRemoved;
        protected virtual void OnDeviceAdded(CDReaderManager m, CDReaderDevice d)
        {
            if (CDReaderDeviceAdded != null)
                CDReaderDeviceAdded(m, d);
        }
        protected virtual void OnDeviceRemoved(CDReaderManager m, CDReaderDevice d)
        {
            if (CDReaderDeviceRemoved != null)
                CDReaderDeviceRemoved(m, d);
        }
        public CDReaderManager()
        {
            deviceWatcher = null;
            ListDeviceInformation = new List<DeviceInformation>();
            return;
        }
        public bool StartDiscovery()
        {
            bool result = false;
            string selector = CustomDevice.GetDeviceSelector(new Guid("53f56308-b6bf-11d0-94f2-00a0c91efb8b"));
            IEnumerable<string> additionalProperties = new string[] { "System.Devices.DeviceInstanceId" };
            if (deviceWatcher!=null)
            {
                StopDiscovery();
            }
            ListDeviceInformation.Clear();
            deviceWatcher = DeviceInformation.CreateWatcher(selector, additionalProperties);
            if (deviceWatcher != null)
            {
                deviceWatcher.Added += deviceWatcher_Added;
                deviceWatcher.Removed += deviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted += deviceWatcher_EnumerationCompleted;
                deviceWatcher.Start();
            }
            return result;
        }
        public bool StopDiscovery()
        {
            bool result = false;
            if (deviceWatcher != null)
            {
                deviceWatcher.Stop();
                deviceWatcher.Added -= deviceWatcher_Added;
                deviceWatcher.Removed -= deviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted -= deviceWatcher_EnumerationCompleted;
                deviceWatcher = null;
                result = true;
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> EjectMedia(string Id)
        {
            bool result = false;
            var customDevice = await Windows.Devices.Custom.CustomDevice.FromIdAsync(Id, 
                DeviceAccessMode.ReadWrite,
                DeviceSharingMode.Exclusive);
            if (customDevice != null)
            {
                try
                {
                    uint r = await customDevice.SendIOControlAsync(
                           ejectMedia,
                        null, null);
                    result = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while ejecting Media: " + ex.Message);

                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<CDMetadata> ReadCDTOC(string Id)
        {
            CDMetadata result = null;
            var customDevice = await Windows.Devices.Custom.CustomDevice.FromIdAsync(Id,
                DeviceAccessMode.ReadWrite,
                DeviceSharingMode.Exclusive);
            if (customDevice != null)
            {
                try
                {
                    int[] SectorArray = await GetCDSectorArray(customDevice);
                    if ((SectorArray != null) && (SectorArray.Length > 1))
                    {
                        result = new CDMetadata();
                        for (int i = 0; i < (SectorArray.Length - 1); i++)
                        {
                            CDTrackMetadata t = new CDTrackMetadata() { Number = i + 1, Title = string.Empty, ISrc = string.Empty, FirstSector = SectorArray[i], LastSector = SectorArray[i + 1], Duration = TimeSpan.FromSeconds((SectorArray[i + 1] - SectorArray[i]) * CD_RAW_SECTOR_SIZE / (44100 * 4)) };
                            if (i < result.Tracks.Count)
                                result.Tracks[i] = t;
                            else
                                result.Tracks.Add(t);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while reading Media Metadata: " + ex.Message);
                    result = null;

                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<string> ReadCDDiscid(string Id)
        {
            string result = string.Empty;
            var customDevice = await Windows.Devices.Custom.CustomDevice.FromIdAsync(Id,
                DeviceAccessMode.ReadWrite,
                DeviceSharingMode.Exclusive);
            if (customDevice != null)
            {
                try
                {
                    int[] SectorArray = await GetCDSectorArray(customDevice);
                    if ((SectorArray != null) && (SectorArray.Length > 1))
                    {
                        // SHA-1 
                        // first track
                        Windows.Security.Cryptography.Core.HashAlgorithmProvider sha1 = Windows.Security.Cryptography.Core.HashAlgorithmProvider.OpenAlgorithm(Windows.Security.Cryptography.Core.HashAlgorithmNames.Sha1);
                        if (sha1 != null)
                        {
                            // SHA-1 
                            // first track
                            int track = 1;
                            string stringToEncod = string.Format("{0:X2}", track);
                            track = SectorArray.Length - 1;
                            stringToEncod += string.Format("{0:X2}", track);
                            stringToEncod += string.Format("{0:X8}", SectorArray[SectorArray.Length - 1] + 150);
                            for (int i = 0; i < 99; i++)
                            {
                                if (i < (SectorArray.Length - 1))
                                    stringToEncod += string.Format("{0:X8}", SectorArray[i] + 150);
                                else
                                    stringToEncod += "00000000";
                            }
                            Windows.Storage.Streams.IBuffer buffer = Windows.Security.Cryptography.CryptographicBuffer.ConvertStringToBinary(stringToEncod, Windows.Security.Cryptography.BinaryStringEncoding.Utf8);
                            Windows.Storage.Streams.IBuffer hashBuffer = sha1.HashData(buffer);
                            result = Windows.Security.Cryptography.CryptographicBuffer.EncodeToBase64String(hashBuffer);
                            // Update to be compliant with MusicBrainz
                            result = result.Replace('+', '.');
                            result = result.Replace('/', '_');
                            result = result.Replace('=', '-');
                            result += "?inc=artists+recordings&toc=1+" + (SectorArray.Length - 1).ToString() + "+" + (SectorArray[SectorArray.Length - 1] + 150).ToString();
                            for (int i = 0; i < (SectorArray.Length - 1); i++)
                                result += "+" + (SectorArray[i] + 150).ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while reading Media Metadata: " + ex.Message);
                    result = string.Empty;

                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<byte[]> ReadCDText(string Id)
        {
            byte[] result = null;
            var customDevice = await Windows.Devices.Custom.CustomDevice.FromIdAsync(Id,
                DeviceAccessMode.ReadWrite,
                DeviceSharingMode.Exclusive);
            if (customDevice != null)
            {
                try
                {
                    result = await GetCDTextArray(customDevice);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while reading Media Metadata: " + ex.Message);
                    result = null;

                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<CDMetadata> ReadMediaMetadata(string Id)
        {
            CDMetadata result = null;
            var customDevice = await Windows.Devices.Custom.CustomDevice.FromIdAsync(Id,
                DeviceAccessMode.ReadWrite,
                DeviceSharingMode.Exclusive);
            if (customDevice != null)
            {
                try
                {
                    int[] SectorArray = await GetCDSectorArray(customDevice);
                    if ((SectorArray != null) && (SectorArray.Length > 1))
                    {
                        result = new CDMetadata();
                        for (int i = 0; i < (SectorArray.Length - 1); i++)
                        {
                            CDTrackMetadata t = new CDTrackMetadata() { Number = i + 1, Title = string.Empty, ISrc = string.Empty, FirstSector = SectorArray[i], LastSector = SectorArray[i + 1], Duration = TimeSpan.FromSeconds((SectorArray[i + 1] - SectorArray[i]) * CD_RAW_SECTOR_SIZE / (44100 * 4))};
                            if (i < result.Tracks.Count)
                                result.Tracks[i] = t;
                            else
                                result.Tracks.Add(t);
                        }
                        byte[] TextArray = await GetCDTextArray(customDevice);
                        if (TextArray != null)
                        {
                            var r = FillCDWithLocalMetadata(result, TextArray);
                            if (r != null)
                                result = r;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while reading Media Metadata: " + ex.Message);
                    result = null;

                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<CDMetadata> CheckMedia(string Id)
        {
            CDMetadata result = null;
            var customDevice = await Windows.Devices.Custom.CustomDevice.FromIdAsync(Id,
                DeviceAccessMode.ReadWrite,
                DeviceSharingMode.Exclusive);
            if (customDevice != null)
            {
                try
                {
                    int[] SectorArray = await GetCDSectorArray(customDevice);
                    if ((SectorArray != null) && (SectorArray.Length > 1))
                    {
                        result = new CDMetadata();
                        for (int i = 0; i < (SectorArray.Length - 1); i++)
                        {
                            CDTrackMetadata t = new CDTrackMetadata() { Number = i + 1, Title = string.Empty, ISrc = string.Empty, FirstSector = SectorArray[i], LastSector = SectorArray[i + 1], Duration = TimeSpan.FromSeconds((SectorArray[i + 1] - SectorArray[i]) * CD_RAW_SECTOR_SIZE / (44100 * 4)) };
                            if (i < result.Tracks.Count)
                                result.Tracks[i] = t;
                            else
                                result.Tracks.Add(t);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while reading Media Metadata: " + ex.Message);
                    result = null;

                }
            }
            return result;
        }
        static int MSF_TO_LBA2(byte min, byte sec, byte frame)
        {
            return (int)(frame + 75 * (sec - 2 + 60 * min));
        }
        private async System.Threading.Tasks.Task<int[]> GetCDSectorArray(Windows.Devices.Custom.CustomDevice device)
        {
            int[] Array = null;
            if (device != null)
            {
                var outputBuffer = new byte[MAXIMUM_NUMBER_TRACKS * 8 + 4];

                try
                {
                    uint r = await device.SendIOControlAsync(
                           readTable,
                        null, outputBuffer.AsBuffer());

                    if (r > 0)
                    {
                        int i_tracks = outputBuffer[3] - outputBuffer[2] + 1;

                        Array = new int[i_tracks + 1];
                        if (Array != null)
                        {
                            for (int i = 0; (i <= i_tracks) && (4 + i * 8 + 4 + 3 < r); i++)
                            {
                                int sectors = MSF_TO_LBA2(
                                    outputBuffer[4 + i * 8 + 4 + 1],
                                    outputBuffer[4 + i * 8 + 4 + 2],
                                    outputBuffer[4 + i * 8 + 4 + 3]);
                                Array[i] = sectors;
                                System.Diagnostics.Debug.WriteLine("track number: " + i.ToString() + " sectors: " + sectors.ToString());
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while reading CD Sector Array : " + ex.Message);
                }
            }
            return Array;
        }
        async System.Threading.Tasks.Task<byte[]> GetCDTextArray(Windows.Devices.Custom.CustomDevice device)
        {
            byte[] Array = null;
            if (device != null)
            {
                var inputBuffer = new byte[4];
                inputBuffer[0] = 0x05;
                var outputBuffer = new byte[4];

                try
                {

                    uint r = await device.SendIOControlAsync(
                           readTableEx,
                        inputBuffer.AsBuffer(), outputBuffer.AsBuffer());

                    if (r > 0)
                    {
                        int i_text = 2 + (outputBuffer[0] << 8) + outputBuffer[1];
                        if (i_text > 4)
                        {

                            Array = new byte[i_text];

                            r = await device.SendIOControlAsync(
                                       readTableEx,
                                    inputBuffer.AsBuffer(), Array.AsBuffer());
                        }

                    }
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while reading CD Text Array : " + ex.Message);
                }
            }
            return Array;
        }
        public async System.Threading.Tasks.Task<string> GetAlbumArtUrl(string MBID)
        {
            string result = string.Empty;
            try
            {
                AlbumArtObject albumArt = await GetAlbumArtObjects(MBID);
                if (albumArt != null)
                {
                    if ((albumArt.images != null) &&
                        (albumArt.images.Count >= 1))
                    {
                        if (albumArt.images[0].approved == true)
                        {
                            if (albumArt.images[0].thumbnails != null)
                            {
                                if (!string.IsNullOrEmpty(albumArt.images[0].thumbnails.large))
                                    result = albumArt.images[0].thumbnails.large;
                                else if (!string.IsNullOrEmpty(albumArt.images[0].thumbnails.small))
                                    result = albumArt.images[0].thumbnails.small;
                                else if (!string.IsNullOrEmpty(albumArt.images[0].image))
                                    result = albumArt.images[0].image;
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while getting album art url: " + ex.Message);
                result = string.Empty;
            }
            return result;
        }

        public async System.Threading.Tasks.Task<CDMetadata> FillCDWithOnlineMetadata(CDMetadata currentCD, string discid)
        {
            try
            {
                //Clear CD and Track metadata info:
                currentCD.ISrc = string.Empty;
                currentCD.Message = string.Empty;
                currentCD.Genre = string.Empty;
                currentCD.AlbumTitle = string.Empty;
                currentCD.Artist = string.Empty;
                currentCD.albumArtUrl = string.Empty;
                currentCD.DiscID = string.Empty;
                if (!string.IsNullOrEmpty(discid))
                {
                    char[] sep = { '?' };
                    string[] array = discid.Split(sep);
                    if (array.Count() >= 1)
                    {
                        currentCD.DiscID = array[0];
                        DiscIDObject v = await GetCDObjects(discid);
                        if(v!=null)
                        { 
                            if((v.releases!=null)&&(v.releases.Count>0))
                            {
                                for(int i = 0; i < v.releases.Count;i++)
                                {
                                    if ((v.releases[i].media != null) && (v.releases[i].media.Count > 0))
                                    {
                                        for (int j = 0; j < v.releases[i].media.Count; j++)
                                        {
                                            if (string.Equals(v.releases[i].media[j].format, "CD", StringComparison.OrdinalIgnoreCase))
                                            {
                                                if ((v.releases[i].media[j].discs != null) && (v.releases[i].media[j].discs.Count > 0))
                                                {
                                                    for (int k = 0; k < v.releases[i].media[j].discs.Count; k++)
                                                    {
                                                        if (string.Equals(v.releases[i].media[j].discs[k].id, currentCD.DiscID))
                                                        {
                                                            // Found 
                                                            currentCD.AlbumTitle = v.releases[i].title;
                                                            if((v.releases[i].artistcredit!=null)&&
                                                                (v.releases[i].artistcredit.Count>0))
                                                            {
                                                                currentCD.Artist = v.releases[i].artistcredit[0].name;
                                                            }
                                                            currentCD.albumArtUrl = await GetAlbumArtUrl(v.releases[i].id);
                                                            if ((v.releases[i].media[j].tracks!=null)&&(v.releases[i].media[j].tracks.Count == currentCD.Tracks.Count))
                                                            {
                                                                for (int m = 0; m < v.releases[i].media[j].tracks.Count; m++)
                                                                {
                                                                    if (currentCD.Tracks[m].Number == v.releases[i].media[j].tracks[m].position)
                                                                    {
                                                                        currentCD.Tracks[m].Artist = currentCD.Artist;
                                                                        currentCD.Tracks[m].Album = currentCD.AlbumTitle;
                                                                        currentCD.Tracks[m].Poster = currentCD.albumArtUrl;
                                                                        currentCD.Tracks[m].Title = v.releases[i].media[j].tracks[m].title;
                                                                    }
                                                                }
                                                            }
                                                            return currentCD;
                                                        }
                                                    }
                                                }
                                            }
                                        } 
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while getting online Medtadata: " + ex.Message);
                currentCD = null;
            }
            return currentCD;
        }
        public CDMetadata FillCDWithLocalMetadata(CDMetadata currentCD, byte[] TextArray)
        {
            try
            {
                int i_track_last = 0;
                string[] ArrayTrackInfo = new string[99];
                int Count = (TextArray.Length - 4) / 18;
                //Clear CD and Track metadata info:
                currentCD.ISrc = string.Empty;
                currentCD.Message = string.Empty;
                currentCD.Genre = string.Empty;
                currentCD.AlbumTitle = string.Empty;
                currentCD.Artist = string.Empty;
                currentCD.DiscID = string.Empty;


                for (int i = 0; i < currentCD.Tracks.Count; i++)
                {
                    currentCD.Tracks[i].ISrc = string.Empty;
                    currentCD.Tracks[i].Title = string.Empty;
                }
                for (int i = 0; i < Count; i++)
                {


                    int i_pack_type = TextArray[4 + 18 * i];
                    if (i_pack_type < 0x80 || i_pack_type > 0x8f)
                        continue;

                    int i_track_number = (TextArray[4 + 18 * i + 1] >> 0) & 0x7f;
                    int i_extension_flag = (TextArray[4 + 18 * i + 1] >> 7) & 0x01;
                    if (i_extension_flag != 0)
                        continue;

                    int i_track = i_track_number;

                    int indexTrack = 4 + 18 * i + 4;
                    int indexTrackMax = indexTrack + 12;
                    while (i_track <= 127 && indexTrack < indexTrackMax)
                    {
                        byte[] resArray = new byte[13];
                        int k = 0;
                        int l = 0;
                        for (k = 0; k < 12; k++, l++)
                        {
                            resArray[l] = TextArray[4 + 18 * i + 4 + k];
                            if ((resArray[l] == 0x00) || (k == 11))
                            {
                                string str;
//                                str = System.Text.Encoding.UTF8.GetString(resArray, 0, (resArray[l] == 0x00) ? l : l + 1);
                                str = System.Text.Encoding.UTF7.GetString(resArray, 0, (resArray[l] == 0x00) ? l : l + 1);
                                if (!string.IsNullOrEmpty(str))
                                {
                                    switch (i_pack_type - 0x80)
                                    {
                                        // Title
                                        case 0x00:
                                            if (i_track == 0)
                                                currentCD.AlbumTitle += str;
                                            else
                                            {
                                                if (i_track == currentCD.Tracks.Count + 1)
                                                {
                                                    CDTrackMetadata t = new CDTrackMetadata() { Number = i_track, Title = string.Empty, ISrc = string.Empty, FirstSector = 0, LastSector = 0, Duration = TimeSpan.FromSeconds(0) };
                                                    if ((i_track - 1) < currentCD.Tracks.Count)
                                                        currentCD.Tracks[i_track - 1] = t;
                                                    else
                                                        currentCD.Tracks.Add(t);
                                                }
                                                if (i_track <= currentCD.Tracks.Count)
                                                    currentCD.Tracks[i_track - 1].Title += str;
                                            }
                                            break;
                                        // DiscID
                                        case 0x06:
                                            if (i_track == 0)
                                                currentCD.DiscID += str;
                                            break;
                                        // Artist
                                        case 0x01:
                                            if (i_track == 0)
                                                currentCD.Artist += str;
                                            break;
                                        // Message
                                        case 0x05:
                                            if (i_track == 0)
                                                currentCD.Message += str;
                                            break;
                                        // Genre
                                        case 0x07:
                                            if (i_track == 0)
                                                currentCD.Genre += str;
                                            break;
                                        // ISRC
                                        case 0x0E:
                                            if (i_track == 0)
                                                currentCD.ISrc += str;
                                            else
                                            {
                                                if (i_track == currentCD.Tracks.Count + 1)
                                                {
                                                    CDTrackMetadata t = new CDTrackMetadata() { Number = i_track, Title = string.Empty, ISrc = string.Empty, FirstSector = 0, LastSector = 0, Duration = TimeSpan.FromSeconds(0) };
                                                    if ((i_track - 1) < currentCD.Tracks.Count)
                                                        currentCD.Tracks[i_track - 1] = t;
                                                    else
                                                        currentCD.Tracks.Add(t);
                                                }
                                                if (i_track <= currentCD.Tracks.Count)
                                                    currentCD.Tracks[i_track - 1].ISrc += str;
                                            }
                                            break;
                                        default:
                                            break;

                                    }
                                }
                                // System.Diagnostics.Debug.WriteLine("Track: " + i_track.ToString() + " Type: " + (i_pack_type - 0x80).ToString() + " Text: " + str);
                                i_track++;
                                l = -1;
                            }
                        }
                        indexTrack += k;
                        i_track_last = (i_track_last > i_track ? i_track_last : i_track);

                        i_track++;
                        indexTrack += 1 + 12;
                    }
                }
                System.Diagnostics.Debug.WriteLine("Title: " + currentCD.AlbumTitle + " Artist: " + currentCD.Artist + " DiscID: " + currentCD.DiscID + " ISrc: " + currentCD.ISrc);
                for (int l = 0; l < currentCD.Tracks.Count; l++)
                {
                    if (!string.IsNullOrEmpty(currentCD.Artist))
                        currentCD.Tracks[l].Artist = currentCD.Artist;
                    if (!string.IsNullOrEmpty(currentCD.AlbumTitle))
                        currentCD.Tracks[l].Album = currentCD.AlbumTitle;
                    if (!string.IsNullOrEmpty(currentCD.DiscID))
                        currentCD.Tracks[l].DiscID = currentCD.DiscID;

                    System.Diagnostics.Debug.WriteLine("Track : " + currentCD.Tracks[l].Number.ToString() + " Title: " + currentCD.Tracks[l].Title + "Duration: " + currentCD.Tracks[l].Duration.ToString() + " ISRC: " + currentCD.Tracks[l].ISrc);
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while parsing CD Text Array : " + ex.Message);
                currentCD = null;
            }
            return currentCD;
        }
        private void deviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
        }

        private void deviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if ((ListDeviceInformation != null) && (ListDeviceInformation.Count > 0))
            {
                foreach (var d in ListDeviceInformation)
                {
                    if (d.Id == args.Id)
                    {
                        ListDeviceInformation.Remove(d);
                        OnDeviceRemoved(this, new CDReaderDevice(d.Name, d.Id));
                        break;
                    }
                }
            }
        }

        private void deviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (ListDeviceInformation != null)
            {
                ListDeviceInformation.Add(args);
                OnDeviceAdded(this, new CDReaderDevice(args.Name, args.Id));
            }
        }
        // CD attributes
        const ushort FILE_DEVICE_CD_ROM = 0x00000002;
        const ushort FILE_DEVICE_MASS_STORAGE = 0x0000002d;
        const int MAXIMUM_NUMBER_TRACKS = 100;
        IOControlCode ejectMedia = new IOControlCode(FILE_DEVICE_CD_ROM, 0x0202, IOControlAccessMode.Read, IOControlBufferingMethod.Buffered);
        IOControlCode loadMedia = new IOControlCode(FILE_DEVICE_CD_ROM, 0x0203, IOControlAccessMode.Read, IOControlBufferingMethod.Buffered);
        IOControlCode reserveMedia = new IOControlCode(FILE_DEVICE_CD_ROM, 0x0204, IOControlAccessMode.Read, IOControlBufferingMethod.Buffered);
        IOControlCode releaseMedia = new IOControlCode(FILE_DEVICE_CD_ROM, 0x0205, IOControlAccessMode.Read, IOControlBufferingMethod.Buffered);
        IOControlCode readTable = new IOControlCode(FILE_DEVICE_CD_ROM, 0x0000, IOControlAccessMode.Read, IOControlBufferingMethod.Buffered);
        IOControlCode readTableEx = new IOControlCode(FILE_DEVICE_CD_ROM, 0x0015, IOControlAccessMode.Read, IOControlBufferingMethod.Buffered);
        IOControlCode readRaw = new IOControlCode(FILE_DEVICE_CD_ROM, 0x000F, IOControlAccessMode.Read, IOControlBufferingMethod.DirectOutput);


        #region OnlineMetadata


        public static T ReadToObject<T>(string json)
        {
            T deserializedObject;
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            deserializedObject = (T)ser.ReadObject(ms);
            return deserializedObject;
        }
        public static async System.Threading.Tasks.Task<AlbumArtObject> GetAlbumArtObjects(string MBID)
        {
            AlbumArtObject Result = null;
            Uri restAPIUri = null;
            restAPIUri = new Uri("http://coverartarchive.org/release/" + MBID);
            string url = string.Empty;
            try
            {
                HttpClient hc = new HttpClient();
                hc.DefaultRequestHeaders.TryAppendWithoutValidation("Accept", "application/json");
                hc.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", Information.SystemInformation.ApplicationName + "/" +Information.SystemInformation.ApplicationVersion);
                hc.DefaultRequestHeaders.Remove("Accept-Encoding");

                HttpResponseMessage rep = await hc.GetAsync(restAPIUri);
                if ((rep != null) && (rep.StatusCode == HttpStatusCode.Ok) && (rep.Content != null))
                {
                    string s = rep.Content.ReadAsStringAsync().GetResults();
                    if (!string.IsNullOrEmpty(s))
                    {
                        Result = ReadToObject<AlbumArtObject>(s);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception " + e.Message);
            }

            return Result;
        }
        public static async System.Threading.Tasks.Task<DiscIDObject> GetCDObjects(string discID)
        {
            DiscIDObject Result = null;
            Uri restAPIUri = null;
            restAPIUri = new Uri("https://musicbrainz.org/ws/2/discid/" + discID);
            string url = string.Empty;
            try
            {
                HttpClient hc = new HttpClient();
                hc.DefaultRequestHeaders.TryAppendWithoutValidation("Accept", "application/json");
                hc.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", Information.SystemInformation.ApplicationName + "/" + Information.SystemInformation.ApplicationVersion);
                hc.DefaultRequestHeaders.Remove("Accept-Encoding");

                HttpResponseMessage rep = await hc.GetAsync(restAPIUri);
                if ((rep != null) && (rep.StatusCode == HttpStatusCode.Ok) && (rep.Content != null))
                {
                    string s = rep.Content.ReadAsStringAsync().GetResults();
                    if (!string.IsNullOrEmpty(s))
                    {
                        Result = ReadToObject<DiscIDObject>(s);
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception " + e.Message);
            }

            return Result;
        }
        #endregion
    }
}
