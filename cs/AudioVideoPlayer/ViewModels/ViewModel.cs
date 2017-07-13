using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioVideoPlayer.ViewModels
{
    public class ViewModel
    {
        private SettingsViewModel _settings;

        public  SettingsViewModel Settings
        {
            get
            {
                if (_settings == null)
                    _settings = new SettingsViewModel();
                return _settings;
            }
        }
    }
}
