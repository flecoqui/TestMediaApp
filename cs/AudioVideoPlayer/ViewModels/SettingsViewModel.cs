using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Windows.UI;
using System.Collections.ObjectModel;
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
    }

    public static class StaticSettingsViewModel
    {


        public enum PlayerWindowState
        {
            WindowMode = 0,
            FullWindow,
            FullScreen
        };
        // Player Settings
        private static bool autoStart;
        private static PlayerWindowState windowState;

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
                    WindowState = (PlayerWindowState)value;
                }
                return windowState;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(WindowState), value.ToString());
                windowState = value;
            }
        }

        // Remote Settings
        public const string cALL = "All(Multicast)";
        private static ObservableCollection<Models.Device> deviceList;
        private static int indexRemoteContent;
        private static string uriRemoteContent;
        private static string uriRemotePlaylist;

        public static ObservableCollection<Models.Device> DeviceList
        {
            get
            {
                var auto = Helpers.SettingsHelper.ReadSettingsValue(nameof(DeviceList));
                if ((auto == null) || (string.IsNullOrEmpty(auto.ToString())))
                {
                    deviceList = new ObservableCollection<Models.Device>();
                    deviceList.Add(new Models.Device(cALL,Companion.CompanionClient.cMulticastAddress) );
                    deviceList.Add(new Models.Device("MyDevice", "192.168.0.1"));
                }
                else
                    deviceList = (ObservableCollection<Models.Device>)auto;
                return deviceList;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(DeviceList), value);
                deviceList = value;
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
        private static Color backgroundColor;
        private static Color foregroundColor;
        private static bool darkTheme;
        public static List<Color> MenuBackgroundColors
        {
            get
            {
                if (menuBackgroundColors == null)
                {
                    menuBackgroundColors = new List<Color>();
                    if (menuBackgroundColors != null)
                    {
                        menuBackgroundColors.Add(Windows.UI.Colors.DeepSkyBlue);
                        menuBackgroundColors.Add(Windows.UI.Colors.Purple);
                        menuBackgroundColors.Add(Windows.UI.Colors.Orange);
                        menuBackgroundColors.Add(Windows.UI.Colors.Green);
                        menuBackgroundColors.Add(Windows.UI.Colors.Gray);
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
                var color = Helpers.SettingsHelper.ReadSettingsValue(nameof(ForegroundColor));
                if ((color == null) || (string.IsNullOrEmpty(color.ToString())))
                {

                    foregroundColor = Windows.UI.Colors.Black;
                }
                else
                {
                    var s = color.ToString();
                    var c = new Color();
                    c.A = byte.Parse(s.Substring(1, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.R = byte.Parse(s.Substring(3, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.G = byte.Parse(s.Substring(5, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.B = byte.Parse(s.Substring(7, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    foregroundColor = c;
                }
                return foregroundColor;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(ForegroundColor), value.ToString());
                foregroundColor = value;
            }
        }
        public static Color BackgroundColor
        {
            get
            {
                var color = Helpers.SettingsHelper.ReadSettingsValue(nameof(BackgroundColor));
                if ((color == null) || (string.IsNullOrEmpty(color.ToString())))
                {

                    backgroundColor = Windows.UI.Colors.White;
                }
                else
                {
                    var s = color.ToString();
                    var c = new Color();
                    c.A = byte.Parse(s.Substring(1, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.R = byte.Parse(s.Substring(3, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.G = byte.Parse(s.Substring(5, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    c.B = byte.Parse(s.Substring(7, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                    backgroundColor = c;
                }
                return backgroundColor;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(BackgroundColor), value.ToString());
                backgroundColor = value;
            }
        }
        public static bool DarkTheme
        {
            get
            {
                var theme = Helpers.SettingsHelper.ReadSettingsValue(nameof(DarkTheme));
                if ((theme == null) || (string.IsNullOrEmpty(theme.ToString())))
                {

                    darkTheme = true;
                }
                else
                {
                    var s = theme.ToString();
                    bool c = true;
                    if (!string.IsNullOrEmpty(s))
                        bool.TryParse(s, out c);
                    darkTheme = c;
                }
                return darkTheme;
            }
            set
            {
                Helpers.SettingsHelper.SaveSettingsValue(nameof(DarkTheme), value.ToString());
                darkTheme = value;
            }
        }
    }
}
