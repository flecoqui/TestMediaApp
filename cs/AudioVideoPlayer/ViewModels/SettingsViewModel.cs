using System;
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
    public class SettingsViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // UI Settings

        public List<Color> MenuBackgroundColors
        {
            get
            {
                return StaticSettingsViewModel.MenuBackgroundColors;
            }
        }
        public Color MenuBackgroundColor
        {
            get
            {
                return StaticSettingsViewModel.MenuBackgroundColor;
            }
            set
            {
                StaticSettingsViewModel.MenuBackgroundColor = value;
                NotifyPropertyChanged();
            }
        }
        public Color MenuForegroundColor
        {
            get
            {
                return StaticSettingsViewModel.MenuForegroundColor;
            }
            set
            {
                StaticSettingsViewModel.MenuForegroundColor = value;
                NotifyPropertyChanged();
            }
        }
        public Color BackgroundColor
        {
            get
            {
                return StaticSettingsViewModel.BackgroundColor;
            }
        }
        public Color ForegroundColor
        {
            get
            {
                return StaticSettingsViewModel.ForegroundColor;
            }
        }
        public Color BorderColor
        {
            get
            {
                return StaticSettingsViewModel.BorderColor;
            }
        }

        public Color BackgroundPointerOverColor
        {
            get
            {
                return StaticSettingsViewModel.BackgroundPointerOverColor;
            }
        }
        public Color ForegroundPointerOverColor
        {
            get
            {
                return StaticSettingsViewModel.ForegroundPointerOverColor;
            }
        }
        public Color BorderPointerOverColor
        {
            get
            {
                return StaticSettingsViewModel.BorderPointerOverColor;
            }
        }
        public Color BackgroundPressedColor
        {
            get
            {
                return StaticSettingsViewModel.BackgroundPressedColor;
            }
        }
        public Color ForegroundPressedColor
        {
            get
            {
                return StaticSettingsViewModel.ForegroundPressedColor;
            }
        }
        public Color BorderPressedColor
        {
            get
            {
                return StaticSettingsViewModel.BorderPressedColor;
            }
        }
        public Color BackgroundDisabledColor
        {
            get
            {
                return StaticSettingsViewModel.BackgroundDisabledColor;
            }
        }
        public Color ForegroundDisabledColor
        {
            get
            {
                return StaticSettingsViewModel.ForegroundDisabledColor;
            }
        }
        public Color BorderDisabledColor
        {
            get
            {
                return StaticSettingsViewModel.BorderDisabledColor;
            }
        }


        public bool LightTheme
        {
            get
            {
                return StaticSettingsViewModel.LightTheme;
            }
            set
            {
                StaticSettingsViewModel.LightTheme = value;
                NotifyPropertyChanged();
            }
        }
        // Application Settings
        public bool ApplicationStart
        {
            get
            {
                return StaticSettingsViewModel.ApplicationStart;
            }
            set
            {
                StaticSettingsViewModel.ApplicationStart = value;
                NotifyPropertyChanged();
            }
        }

        // Player Settings
        public bool AutoStart
        {
            get
            {
                return StaticSettingsViewModel.AutoStart;
            }
            set
            {
                StaticSettingsViewModel.AutoStart = value;
                NotifyPropertyChanged();
            }
        }
        public bool ContentLoop
        {
            get
            {
                return StaticSettingsViewModel.ContentLoop;
            }
            set
            {
                StaticSettingsViewModel.ContentLoop = value;
                NotifyPropertyChanged();
            }
        }
        public bool PlaylistLoop
        {
            get
            {
                return StaticSettingsViewModel.PlaylistLoop;
            }
            set
            {
                StaticSettingsViewModel.PlaylistLoop = value;
                NotifyPropertyChanged();
            }
        }
        public StaticSettingsViewModel.PlayerWindowState WindowState
        {
            get
            {
                return StaticSettingsViewModel.WindowState;
            }
            set
            {
                StaticSettingsViewModel.WindowState = value;
                NotifyPropertyChanged();
            }
        }
        public int MaxBitrate
        {
            get
            {
                return StaticSettingsViewModel.MaxBitrate;
            }
            set
            {
                StaticSettingsViewModel.MaxBitrate = value;
                NotifyPropertyChanged();
            }
        }
        public int MinBitrate
        {
            get
            {
                return StaticSettingsViewModel.MinBitrate;
            }
            set
            {
                StaticSettingsViewModel.MinBitrate = value;
                NotifyPropertyChanged();
            }
        }
        public int LiveOffset
        {
            get
            {
                return StaticSettingsViewModel.LiveOffset;
            }
            set
            {
                StaticSettingsViewModel.LiveOffset = value;
                NotifyPropertyChanged();
            }
        }
        // Remote Settings
        public ObservableCollection<Companion.CompanionDevice> DeviceList
        {
            get
            {
                return StaticSettingsViewModel.DeviceList;
            }
            set
            {
                StaticSettingsViewModel.DeviceList = value;
                NotifyPropertyChanged();
            }
        }
        public int IndexRemoteContent
        {
            get
            {
                return StaticSettingsViewModel.IndexRemoteContent;
            }
            set
            {
                StaticSettingsViewModel.IndexRemoteContent = value;
                NotifyPropertyChanged();
            }
        }
        public string UriRemoteContent
        {
            get
            {
                return StaticSettingsViewModel.UriRemoteContent;
            }
            set
            {
                StaticSettingsViewModel.UriRemoteContent = value;
                NotifyPropertyChanged();
            }
        }
        public string UriRemotePlaylist
        {
            get
            {
                return StaticSettingsViewModel.UriRemotePlaylist;
            }
            set
            {
                StaticSettingsViewModel.UriRemotePlaylist = value;
                NotifyPropertyChanged();
            }
        }
        // PlayList settings
        public ObservableCollection<Models.PlayList> PlayListList
        {
            get
            {
                return StaticSettingsViewModel.PlayListList;
            }
            set
            {
                StaticSettingsViewModel.PlayListList = value;
                NotifyPropertyChanged();
            }
        }
        public string CurrentPlayListPath
        {
            get
            {
                return StaticSettingsViewModel.CurrentPlayListPath;
            }
            set
            {
                StaticSettingsViewModel.CurrentPlayListPath = value;
                NotifyPropertyChanged();
            }
        }
        public int CurrentPlayListIndex
        {
            get
            {
                return StaticSettingsViewModel.CurrentPlayListIndex;
            }
            set
            {
                StaticSettingsViewModel.CurrentPlayListIndex = value;
                NotifyPropertyChanged();
            }
        }
        public string CurrentMediaPath
        {
            get
            {
                return StaticSettingsViewModel.CurrentMediaPath;
            }
            set
            {
                StaticSettingsViewModel.CurrentMediaPath = value;
                NotifyPropertyChanged();
            }
        }
        public int CurrentMediaIndex
        {
            get
            {
                return StaticSettingsViewModel.CurrentMediaIndex;
            }
            set
            {
                StaticSettingsViewModel.CurrentMediaIndex = value;
                NotifyPropertyChanged();
            }
        }
        public int MulticastUDPPort
        {
            get
            {
                return StaticSettingsViewModel.MulticastUDPPort;
            }
            set
            {
                StaticSettingsViewModel.MulticastUDPPort = value;
                NotifyPropertyChanged();
            }
        }
        public int UnicastUDPPort
        {
            get
            {
                return StaticSettingsViewModel.UnicastUDPPort;
            }
            set
            {
                StaticSettingsViewModel.UnicastUDPPort = value;
                NotifyPropertyChanged();
            }
        }
        public string MulticastIPAddress
        {
            get
            {
                return StaticSettingsViewModel.MulticastIPAddress;
            }
            set
            {
                StaticSettingsViewModel.MulticastIPAddress = value;
                NotifyPropertyChanged();
            }
        }
        public bool MulticastDiscovery
        {
            get
            {
                return StaticSettingsViewModel.MulticastDiscovery;
            }
            set
            {
                StaticSettingsViewModel.MulticastDiscovery = value;
                NotifyPropertyChanged();
            }
        }
        public bool UdpTransport
        {
            get
            {
                return StaticSettingsViewModel.UdpTransport;
            }
            set
            {
                StaticSettingsViewModel.UdpTransport = value;
                NotifyPropertyChanged();
            }
        }


        // Playlist

        private string playlistFolder;
        public string PlaylistFolder
        {
            get
            {
                return playlistFolder;
            }
            set
            {
                playlistFolder = value;
                NotifyPropertyChanged();
            }
        }

        private string playlistName;
        public string PlaylistName
        {
            get
            {
                return playlistName;
            }
            set
            {
                playlistName = value;
                NotifyPropertyChanged();
            }
        }
        private string playlistFilters;
        public string PlaylistFilters
        {
            get
            {
                return playlistFilters;
            }
            set
            {
                playlistFilters = value;
                NotifyPropertyChanged();
            }
        }
        private string playlistPath;
        public string PlaylistPath
        {
            get
            {
                return playlistPath;
            }
            set
            {
                playlistPath = value;
                NotifyPropertyChanged();
            }
        }
        public int SlideShowPeriod
        {
            get
            {
                return StaticSettingsViewModel.SlideShowPeriod;
            }
            set
            {
                StaticSettingsViewModel.SlideShowPeriod = value;
                NotifyPropertyChanged();
            }
        }

        public bool CreateThumbnails
        {
            get
            {
                return StaticSettingsViewModel.CreateThumbnails;
            }
            set
            {
                StaticSettingsViewModel.CreateThumbnails = value;
                NotifyPropertyChanged();
            }
        }

        public int CloudSlideShowPeriod
        {
            get
            {
                return StaticSettingsViewModel.CloudSlideShowPeriod;
            }
            set
            {
                StaticSettingsViewModel.CloudSlideShowPeriod = value;
                NotifyPropertyChanged();
            }
        }

        public bool CloudCreateThumbnails
        {
            get
            {
                return StaticSettingsViewModel.CloudCreateThumbnails;
            }
            set
            {
                StaticSettingsViewModel.CloudCreateThumbnails = value;
                NotifyPropertyChanged();
            }
        }
        public string AzureAccountName
        {
            get
            {
                return StaticSettingsViewModel.AzureAccountName;
            }
            set
            {
                StaticSettingsViewModel.AzureAccountName = value;
                NotifyPropertyChanged();
            }
        }
        public string AzureAccountKey
        {
            get
            {
                return StaticSettingsViewModel.AzureAccountKey;
            }
            set
            {
                StaticSettingsViewModel.AzureAccountKey = value;
                NotifyPropertyChanged();
            }
        }
        public string AzureContainer
        {
            get
            {
                return StaticSettingsViewModel.AzureContainer;
            }
            set
            {
                StaticSettingsViewModel.AzureContainer = value;
                NotifyPropertyChanged();
            }
        }
        private string cloudPlaylistName;
        public string CloudPlaylistName
        {
            get
            {
                return cloudPlaylistName;
            }
            set
            {
                cloudPlaylistName = value;
               
                NotifyPropertyChanged();
            }
        }
        private string cloudPlaylistFilters;
        public string CloudPlaylistFilters
        {
            get
            {
                return cloudPlaylistFilters;
            }
            set
            {
                cloudPlaylistFilters = value;
                NotifyPropertyChanged();
            }
        }
        private string cloudPlaylistPath;
        public string CloudPlaylistPath
        {
            get
            {
                return cloudPlaylistPath;
            }
            set
            {
                cloudPlaylistPath = value;
                NotifyPropertyChanged();
            }
        }
    }


}
