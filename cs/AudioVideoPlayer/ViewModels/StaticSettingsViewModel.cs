﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Windows.UI;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace AudioVideoPlayer.ViewModels
{

    public static class StaticSettingsViewModel
    {


        public enum PlayerWindowState
        {
            WindowMode = 0,
            FullWindow,
            FullScreen
        };
        // Player Settings
        private static bool applicationStart;
        private static bool autoStart;
        private static bool contentLoop;
        private static bool playlistLoop;
        private static bool onlineMetadata;
        private static bool liveBufferLogs;
        private static bool downloadLogs;
        private static bool subtitleLogs;
        private static bool nativeSmoothLibrary;
        private static PlayerWindowState windowState;
        private static int maxBitrate;
        private static int minBitrate;
        private static int liveOffset;

        public static bool ApplicationStart
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(ApplicationStart));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    autoStart = false;
                else
                    autoStart = bool.Parse(auto);
                return applicationStart;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(ApplicationStart), value.ToString());
                applicationStart = value;
            }
        }

        public static bool AutoStart
        {
            get
            {
                string auto = (string) Helpers.SettingsHelper.ReadSettingsValue(nameof(AutoStart));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    autoStart = false;
                else
                    autoStart = bool.Parse(auto);
                return autoStart;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(AutoStart), value.ToString());
                autoStart = value;
            }
        }
        public static bool ContentLoop
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(ContentLoop));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    contentLoop = false;
                else
                    contentLoop = bool.Parse(auto);
                return contentLoop;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(ContentLoop), value.ToString());
                contentLoop = value;
            }
        }
        static long GetVersionNumber(int Index, string version)
        {
            long result = 0;
            char[] sep = { '.' };
            string[] res = version.Split(sep);
            if ((res != null) && (res.Count() == 4))
            {
                long.TryParse(res[Index], out result);
            }
            return result;
        }
        /// <summary>
        /// This method indicates whether the OS is at least RS4 which support natively Smooth Streaming
        /// </summary>
        static bool IsRS4()
        {
            if ((GetVersionNumber(0, Information.SystemInformation.SystemVersion) * 281474976710656 +
                GetVersionNumber(1, Information.SystemInformation.SystemVersion) * 4294967296 +
                GetVersionNumber(2, Information.SystemInformation.SystemVersion) * 65536 +
                GetVersionNumber(3, Information.SystemInformation.SystemVersion)) >= (10 * 281474976710656 + 17134 * 65536))
                return true;
            return false;
        }
        public static bool NativeSmoothLibrary
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(NativeSmoothLibrary));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    nativeSmoothLibrary = false;
                else
                    nativeSmoothLibrary = bool.Parse(auto);
                if ((nativeSmoothLibrary == true) && (!IsRS4()))
                    nativeSmoothLibrary = false;
                return nativeSmoothLibrary;
            }
            set
            {
                if ((value == true) && (!IsRS4()))
                    value = false;
                Helpers.SettingsHelper.SaveSettingsValue(nameof(NativeSmoothLibrary), value.ToString());
                nativeSmoothLibrary = value;
            }
        }
        public static bool OnlineMetadata
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(OnlineMetadata));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    onlineMetadata = true;
                else
                    onlineMetadata = bool.Parse(auto);
                return onlineMetadata;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(OnlineMetadata), value.ToString());
                onlineMetadata = value;
            }
        }
        public static bool LiveBufferLogs
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(LiveBufferLogs));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    liveBufferLogs = false;
                else
                    liveBufferLogs = bool.Parse(auto);
                return liveBufferLogs;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(LiveBufferLogs), value.ToString());
                liveBufferLogs = value;
            }
        }
        public static bool DownloadLogs
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(DownloadLogs));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    downloadLogs = false;
                else
                    downloadLogs = bool.Parse(auto);
                return downloadLogs;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(DownloadLogs), value.ToString());
                downloadLogs = value;
            }
        }
        public static bool SubtitleLogs
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(SubtitleLogs));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    subtitleLogs = false;
                else
                    subtitleLogs = bool.Parse(auto);
                return subtitleLogs;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(SubtitleLogs), value.ToString());
                subtitleLogs = value;
            }
        }
        public static bool PlaylistLoop
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(PlaylistLoop));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    playlistLoop = false;
                else
                    playlistLoop = bool.Parse(auto);
                return playlistLoop;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(PlaylistLoop), value.ToString());
                playlistLoop = value;
            }
        }

        public static PlayerWindowState WindowState
        {
            get
            {
                string auto = (string) Helpers.SettingsHelper.ReadSettingsValue(nameof(WindowState));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    windowState = PlayerWindowState.WindowMode;
                else
                {
                    int value = int.Parse(auto);

                    if(value>=0 && value < 3)
                        windowState = (PlayerWindowState)value;
                }
                return windowState;
            }
            set
            {
                int intValue = (int) value;
                Helpers.SettingsHelper.SaveSettingsValue(nameof(WindowState), intValue.ToString());
                windowState = value;
            }
        }
        public static int MaxBitrate
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(MaxBitrate));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    maxBitrate = 0;
                else
                    maxBitrate = int.Parse(auto);
                return maxBitrate;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(MaxBitrate), value.ToString());
                maxBitrate = value;
            }
        }
        public static int MinBitrate
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(MinBitrate));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    minBitrate = 0;
                else
                    minBitrate = int.Parse(auto);
                return minBitrate;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(MinBitrate), value.ToString());
                minBitrate = value;
            }
        }
        public static int LiveOffset
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(LiveOffset));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    liveOffset = 30;
                else
                    liveOffset = int.Parse(auto);
                return liveOffset;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(LiveOffset), value.ToString());
                liveOffset = value;
            }
        }
        // Remote Settings
        private static ObservableCollection<Companion.CompanionDevice> deviceList;
        private static int indexRemoteContent;
        private static bool multicastDiscovery;
        private static bool udpTransport;
        private static string uriRemoteContent;
        private static string uriRemotePlaylist;

        public static bool UdpTransport
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(UdpTransport));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    udpTransport = false;
                else
                    udpTransport = bool.Parse(auto);
                return udpTransport;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(UdpTransport), value.ToString());
                udpTransport = value;
            }
        }
        public static bool MulticastDiscovery
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(MulticastDiscovery));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    multicastDiscovery = false;
                else
                    multicastDiscovery = bool.Parse(auto);
                return multicastDiscovery;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(MulticastDiscovery), value.ToString());
                multicastDiscovery = value;
            }
        }

        public  static  ObservableCollection<Companion.CompanionDevice> DeviceList
        {
            get
            {
                var auto = Helpers.StorageHelper.RestoreStringFromFile(nameof(DeviceList));
                //var auto = Helpers.SettingsHelper.ReadSettingsValue(nameof(DeviceList));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                {
                    deviceList = new ObservableCollection<Companion.CompanionDevice>();
                   // deviceList.Add(new Companion.CompanionDevice("0",cALL,Companion.CompanionClient.cMulticastAddress,cALL) );
                    // deviceList.Add(new Companion.CompanionDevice("1","MyHomeDevice", "192.168.1.65","Desktop"));
                  //  deviceList.Add(new Companion.CompanionDevice("2", "MyDeviceWork", "10.85.185.182","PC"));
                  //  deviceList.Add(new Companion.CompanionDevice("3", "MyDevicePhone", "10.85.197.252","Phone"));
                  //  deviceList.Add(new Companion.CompanionDevice("4", "MyDevicePhone1", "172.16.0.3","Phone"));
                  //  deviceList.Add(new Companion.CompanionDevice("5", "MyDevicePC1", "172.16.0.2","PC"));
                }
                else
                    deviceList = ObjectSerializer <ObservableCollection<Companion.CompanionDevice>>.FromXml((string)auto);
                return deviceList;
            }
            set
            {
                string serializeString = ObjectSerializer<ObservableCollection<Companion.CompanionDevice>>.ToXml(value);
                //Helpers.SettingsHelper.SaveSettingsValue(nameof(DeviceList), serializeString);
                bool result = Helpers.StorageHelper.SaveStringIntoFile(nameof(DeviceList), serializeString);
                deviceList = value;
            }

        }
        private static ObservableCollection<DLNA.DLNADevice> dLNADeviceList;

        public static ObservableCollection<DLNA.DLNADevice> DLNADeviceList
        {
            get
            {
                try
                {
                    var auto = Helpers.StorageHelper.RestoreStringFromFile(nameof(DLNADeviceList));
                    //var auto = Helpers.SettingsHelper.ReadSettingsValue(nameof(DeviceList));
                    if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    {
                        dLNADeviceList = new ObservableCollection<DLNA.DLNADevice>();
                    }
                    else
                    {
                        dLNADeviceList = ObjectSerializer<ObservableCollection<DLNA.DLNADevice>>.FromXml((string)auto);
                    }
                }
                catch(Exception)
                {

                }
                if(dLNADeviceList == null)
                    dLNADeviceList = new ObservableCollection<DLNA.DLNADevice>();

                return dLNADeviceList;
            }
            set
            {
                string serializeString = ObjectSerializer<ObservableCollection<DLNA.DLNADevice>>.ToXml(value);
                //Helpers.SettingsHelper.SaveSettingsValue(nameof(DeviceList), serializeString);
                bool result = Helpers.StorageHelper.SaveStringIntoFile(nameof(DLNADeviceList), serializeString);
                dLNADeviceList = value;
            }

        }

        public static int IndexRemoteContent
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(IndexRemoteContent));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    indexRemoteContent = 0;
                else
                    indexRemoteContent = int.Parse(auto);
                return indexRemoteContent;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(IndexRemoteContent), value.ToString());
                indexRemoteContent = value;
            }
        }

        public static string UriRemoteContent
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(UriRemoteContent));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    uriRemoteContent = "http://amssamples.streaming.mediaservices.windows.net/91492735-c523-432b-ba01-faba6c2206a2/AzureMediaServicesPromo.ism/manifest";
                else
                    uriRemoteContent = auto;
                return uriRemoteContent;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(UriRemoteContent), value.ToString());
                uriRemoteContent = value;
            }
        }
        public static string UriRemotePlaylist
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(UriRemotePlaylist));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    uriRemotePlaylist = "https://raw.githubusercontent.com/flecoqui/Windows10/master/Samples/TestMediaApp/cs/AudioVideoPlayer/DataModel/MediaData.json";
                else
                    uriRemotePlaylist = auto;
                return uriRemotePlaylist;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(UriRemotePlaylist), value.ToString());
                uriRemotePlaylist = value;
            }
        }
        // UI settings
        private static List<Color> menuBackgroundColors;
        private static Color menuBackgroundColor;
        private static Color menuForegroundColor;
        private static bool lightTheme;
        public static List<Color> MenuBackgroundColors
        {
            get
            {
                if (menuBackgroundColors == null)
                {
                    menuBackgroundColors = new List<Color>();
                    if (menuBackgroundColors != null)
                    {
                        menuBackgroundColors.Add(Windows.UI.Colors.Gray);
                        menuBackgroundColors.Add(Windows.UI.Colors.Purple);
                        menuBackgroundColors.Add(Windows.UI.Colors.Red);
                        menuBackgroundColors.Add(Windows.UI.Colors.Orange);
                        menuBackgroundColors.Add(Windows.UI.Colors.Green);
                        menuBackgroundColors.Add(Windows.UI.Colors.LightGreen);
                        menuBackgroundColors.Add(Windows.UI.Colors.DeepSkyBlue);
                        menuBackgroundColors.Add(Windows.UI.Colors.LightSkyBlue);
                    }
                }
                return menuBackgroundColors;
            }
        }
        public static Color MenuBackgroundColor
        {
            get
            {
                var color = Helpers.SettingsHelper.ReadSettingsValue(nameof(MenuBackgroundColor));
                if ((color == null) || (string.IsNullOrEmpty(color.ToString())))
                {

                    menuBackgroundColor = MenuBackgroundColors[0];
                }
                else
                {
                    var s = color.ToString();
                    var c = new Color();
                    c.A = byte.Parse(s.Substring(1, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.R = byte.Parse(s.Substring(3, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.G = byte.Parse(s.Substring(5, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.B = byte.Parse(s.Substring(7, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    menuBackgroundColor = c;
                }
                return menuBackgroundColor;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(MenuBackgroundColor), value.ToString());
                menuBackgroundColor = value;
            }
        }
        public static Color MenuForegroundColor
        {
            get
            {
                var color = Helpers.SettingsHelper.ReadSettingsValue(nameof(MenuForegroundColor));
                if ((color == null) || (string.IsNullOrEmpty(color.ToString())))
                {

                    menuForegroundColor = Windows.UI.Colors.White;
                }
                else
                {
                    var s = color.ToString();
                    var c = new Color();
                    c.A = byte.Parse(s.Substring(1, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.R = byte.Parse(s.Substring(3, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.G = byte.Parse(s.Substring(5, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.B = byte.Parse(s.Substring(7, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    menuForegroundColor = c;
                }
                return menuForegroundColor;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(MenuForegroundColor), value.ToString());
                menuForegroundColor = value;
            }
        }
        public static Color ForegroundColor
        {
            get
            {
                return MenuForegroundColor;
            }
        }
        public static Color BackgroundColor
        {
            get
            {
                return MenuBackgroundColor;
            }
        }
        public static Color BorderColor
        {
            get
            {
                return MenuBackgroundColor;
            }
        }
        public static Color ForegroundPointerOverColor
        {
            get
            {
                return MenuForegroundColor;
            }
        }
        public static Color BackgroundPointerOverColor
        {
            get
            {
                return MenuBackgroundColor;
            }
        }
        public static Color BorderPointerOverColor
        {
            get
            {
                return MenuForegroundColor;
            }
        }
        public static Color ForegroundPressedColor
        {
            get
            {
                return MenuForegroundColor;
            }
        }
        public static Color BackgroundPressedColor
        {
            get
            {
                var s = MenuBackgroundColor.ToString();
                var c = new Color();
                c.A = 0xA0;
                c.R = byte.Parse(s.Substring(3, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                c.G = byte.Parse(s.Substring(5, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                c.B = byte.Parse(s.Substring(7, 2), System.Globalization.NumberStyles.AllowHexSpecifier);

                return c;
            }
        }
        public static Color BorderPressedColor
        {
            get
            {
                var s = MenuBackgroundColor.ToString();
                var c = new Color();
                c.A = 0xA0;
                c.R = byte.Parse(s.Substring(3, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                c.G = byte.Parse(s.Substring(5, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                c.B = byte.Parse(s.Substring(7, 2), System.Globalization.NumberStyles.AllowHexSpecifier);

                return c;
            }
        }
        public static Color ForegroundDisabledColor
        {
            get
            {
                return MenuForegroundColor;
            }
        }
        public static Color BackgroundDisabledColor
        {
            get
            {
                var s = MenuBackgroundColor.ToString();
                var c = new Color();
                c.A = 0x50;
                c.R = byte.Parse(s.Substring(3, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                c.G = byte.Parse(s.Substring(5, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                c.B = byte.Parse(s.Substring(7, 2), System.Globalization.NumberStyles.AllowHexSpecifier);

                return c;
            }
        }
        public static Color BorderDisabledColor
        {
            get
            {
                var s = MenuBackgroundColor.ToString();
                var c = new Color();
                c.A = 0x50;
                c.R = byte.Parse(s.Substring(3, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                c.G = byte.Parse(s.Substring(5, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                c.B = byte.Parse(s.Substring(7, 2), System.Globalization.NumberStyles.AllowHexSpecifier);

                return c;
            }
        }
        public static bool LightTheme
        {
            get
            {
                var theme = Helpers.SettingsHelper.ReadSettingsValue(nameof(LightTheme));
                if ((theme == null) || (string.IsNullOrEmpty(theme.ToString())))
                {

                    lightTheme = true;
                }
                else
                {
                    var s = theme.ToString();
                    bool c = true;
                    if (!string.IsNullOrEmpty(s))
                        bool.TryParse(s, out c);
                    lightTheme = c;
                }
                return lightTheme;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(LightTheme), value.ToString());
                lightTheme = value;
            }
        }

        // DeviceInputList Settings
        private static ObservableCollection<Models.DeviceInput> deviceInputList;

        public static ObservableCollection<Models.DeviceInput> DeviceInputList
        {
            get
            {

                if (deviceInputList == null) 
                {

                    deviceInputList = new ObservableCollection<Models.DeviceInput>();
                    if (deviceInputList != null)
                    {
                        
                        deviceInputList.Add(new Models.DeviceInput("inputs/aux_in_1"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/aux_in_2"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/aux_in_3"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/aux_in_4"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/aux1"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/aux2"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/aux3"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/aux4"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/aux5"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/aux6"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/aux7"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/line_in_1"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/line_in_2"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/line_in_3"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/line_in_4"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/coax_in_1"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/coax_in_2"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/optical_in_1"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/optical_in_2"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/hdmi_in_1"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/hdmi_arc_1"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/cable_sat"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/dvd"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/bluray"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/game"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/mediaplayer"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/cd"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/tuner"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/hdradio"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/tvaudio"));
                        deviceInputList.Add(new Models.DeviceInput("inputs/phono"));
                    }
                }

                return deviceInputList;
            }

        }
        // Playlist Settings
        private static ObservableCollection<Models.PlayList> playListList;
        private static string currentPlayListPath;
        private static int currentPlayListIndex;
        private static string currentMediaPath;
        private static int currentMediaIndex;

        public static ObservableCollection<Models.PlayList> PlayListList
        {
             get
            {
                var auto = Helpers.StorageHelper.RestoreStringFromFile(nameof(PlayListList));
                //var auto = Helpers.SettingsHelper.ReadSettingsValue(nameof(PlayListList));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                {

                    playListList = new ObservableCollection<Models.PlayList>();
                    Models.PlayList p = Task.Run(async () => { return await Models.PlayList.GetNewPlaylist("ms-appx:///DataModel/MediaData.json"); }).Result;
                    if ((p != null)&&(p.Count>0))
                        playListList.Add(p);
                }
                else
                {
                    playListList = ObjectSerializer<ObservableCollection<Models.PlayList>>.FromXml((string)auto);
                }
                return playListList;
            }
            set
            {
                playListList = value;
                string serializeString = ObjectSerializer<ObservableCollection<Models.PlayList>>.ToXml(value);

                //Helpers.SettingsHelper.SaveSettingsValue(nameof(PlayListList), serializeString);
                bool result = Helpers.StorageHelper.SaveStringIntoFile(nameof(PlayListList), serializeString);

                playListList = value;
            }
        }
        public static bool SavePlayListList()
        {

            string serializeString = ObjectSerializer<ObservableCollection<Models.PlayList>>.ToXml(playListList);

            //Helpers.SettingsHelper.SaveSettingsValue(nameof(PlayListList), serializeString);
            return Helpers.StorageHelper.SaveStringIntoFile(nameof(PlayListList), serializeString);

        }
        public static string CurrentPlayListPath
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(CurrentPlayListPath));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    currentPlayListPath = "ms-appx:///DataModel/MediaData.json";
                else
                    currentPlayListPath = auto;
                return currentPlayListPath;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Helpers.SettingsHelper.SaveSettingsValue(nameof(CurrentPlayListPath), value.ToString());
                    currentPlayListPath = value;
                }
            }
        }
        public static int CurrentPlayListIndex
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(CurrentPlayListIndex));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    currentPlayListIndex = 0;
                else
                    currentPlayListIndex = int.Parse(auto);
                return currentPlayListIndex;
            }
            set
            {
                if (value >= 0)
                {
                    Helpers.SettingsHelper.SaveSettingsValue(nameof(CurrentPlayListIndex), value.ToString());
                    currentPlayListIndex = value;
                }
            }
        }
        public static string CurrentMediaPath
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(CurrentMediaPath));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    currentMediaPath = string.Empty;
                else
                    currentMediaPath = auto;
                return currentMediaPath;
            }
            set
            {
                //if (!string.IsNullOrEmpty(value))
                if (value!=null)
                {
                    Helpers.SettingsHelper.SaveSettingsValue(nameof(CurrentMediaPath), value.ToString());
                    currentMediaPath = value;
                }
            }
        }
        public static int CurrentMediaIndex
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(CurrentMediaIndex));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    currentMediaIndex = 0;
                else
                    currentMediaIndex = int.Parse(auto);
                return currentMediaIndex;
            }
            set
            {
                if (value >= 0)
                {
                    Helpers.SettingsHelper.SaveSettingsValue(nameof(CurrentMediaIndex), value.ToString());
                    currentMediaIndex = value;
                    if (StaticSettingsViewModel.PlayListList != null)
                    {

                        if ((StaticSettingsViewModel.CurrentPlayListIndex >= 0) && (StaticSettingsViewModel.CurrentPlayListIndex < StaticSettingsViewModel.PlayListList.Count))
                        {
                            StaticSettingsViewModel.PlayListList[StaticSettingsViewModel.CurrentPlayListIndex].Index = value;
                            SavePlayListList();
                        }
                    }
                }
            }
        }
        // Companion Settings
        private static int unicastUDPPort;
        private static int multicastUDPPort;
        private static string multicastIPAddress;

        public static int MulticastUDPPort
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(MulticastUDPPort));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    multicastUDPPort = 1919;
                else
                    multicastUDPPort = int.Parse(auto);
                return multicastUDPPort;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(MulticastUDPPort), value.ToString());
                multicastUDPPort = value;
            }
        }
        public static int UnicastUDPPort
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(UnicastUDPPort));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    unicastUDPPort = 1918;
                else
                    unicastUDPPort = int.Parse(auto);
                return unicastUDPPort;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(UnicastUDPPort), value.ToString());
                unicastUDPPort = value;
            }
        }
        public static string MulticastIPAddress
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(MulticastIPAddress));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    multicastIPAddress = "239.11.11.11";
                else
                    multicastIPAddress = auto;
                return multicastIPAddress;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(MulticastIPAddress), value.ToString());
                multicastIPAddress = value;
            }
        }
        // Playlist Settings
        private static int slideShowPeriod;

        public static int SlideShowPeriod
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(SlideShowPeriod));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    slideShowPeriod = 10000;
                else
                    slideShowPeriod = int.Parse(auto);
                return slideShowPeriod;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(SlideShowPeriod), value.ToString());
                slideShowPeriod = value;
            }
        }
        static bool createThumbnails;
        public static bool CreateThumbnails
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(CreateThumbnails));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    createThumbnails = false;
                else
                    createThumbnails = bool.Parse(auto);
                return createThumbnails;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(CreateThumbnails), value.ToString());
                createThumbnails = value;
            }
        }


        private static int cloudSlideShowPeriod;

        public static int CloudSlideShowPeriod
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(CloudSlideShowPeriod));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    cloudSlideShowPeriod = 10000;
                else
                    cloudSlideShowPeriod = int.Parse(auto);
                return cloudSlideShowPeriod;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(CloudSlideShowPeriod), value.ToString());
                cloudSlideShowPeriod = value;
            }
        }
        static bool cloudCreateThumbnails;
        public static bool CloudCreateThumbnails
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(CloudCreateThumbnails));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    cloudCreateThumbnails = false;
                else
                    cloudCreateThumbnails = bool.Parse(auto);
                return cloudCreateThumbnails;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(CloudCreateThumbnails), value.ToString());
                cloudCreateThumbnails = value;
            }
        }
        private static string azureAccountName;
        public static string AzureAccountName
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(AzureAccountName));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    azureAccountName = "AzureAccountName";
                else
                    azureAccountName = auto;
                return azureAccountName;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(AzureAccountName), value.ToString());
                azureAccountName = value;
            }
        }
        private static string azureAccountKey;
        public static string AzureAccountKey
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(AzureAccountKey));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    azureAccountKey = "AzureAccountKey";
                else
                    azureAccountKey = auto;
                return azureAccountKey;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(AzureAccountKey), value.ToString());
                azureAccountKey = value;
            }
        }
        private static string azureContainer;
        public static string AzureContainer
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(AzureContainer));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                    azureContainer = "AzureContainer";
                else
                    azureContainer = auto;
                return azureContainer;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(AzureContainer), value.ToString());
                azureContainer = value;
            }
        }
        private static string azureFolder;
        public static string AzureFolder
        {
            get
            {
                string auto = (string)Helpers.SettingsHelper.ReadSettingsValue(nameof(AzureFolder));
                if (auto == null)
                    azureFolder = "AzureFolder";
                else
                    azureFolder = auto;
                return azureFolder;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(AzureFolder), value.ToString());
                azureFolder = value;
            }
        }
    }
}
