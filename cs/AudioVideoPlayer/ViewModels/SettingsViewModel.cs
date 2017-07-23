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
            set
            {
                StaticSettingsViewModel.BackgroundColor = value;
                NotifyPropertyChanged();
            }
        }
        public Color ForegroundColor
        {
            get
            {
                return StaticSettingsViewModel.ForegroundColor;
            }
            set
            {
                StaticSettingsViewModel.ForegroundColor = value;
                NotifyPropertyChanged();
            }
        }
        public bool DarkTheme
        {
            get
            {
                return StaticSettingsViewModel.DarkTheme;
            }
            set
            {
                StaticSettingsViewModel.DarkTheme = value;
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
        public bool Loop
        {
            get
            {
                return StaticSettingsViewModel.Loop;
            }
            set
            {
                StaticSettingsViewModel.Loop = value;
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
        // Remote Settings
        public ObservableCollection<Models.Device> DeviceList
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
    }


}
