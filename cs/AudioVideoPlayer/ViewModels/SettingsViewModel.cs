using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Windows.UI;

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
    }

    public static class StaticSettingsViewModel
    {
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
