using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Networking.Connectivity;
using System.Threading.Tasks;

namespace AudioVideoPlayer.Helpers
{
    public class NetworkHelper
    {
        public NetworkHelper()
        {
            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
        }
        public event EventHandler<bool> InternetConnectionChanged;

        public static bool IsInternetAvailable()
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            return (profile != null && profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
        }
        void NetworkInformation_NetworkStatusChanged(object sender)
        {
            if (InternetConnectionChanged != null)
                InternetConnectionChanged(this, IsInternetAvailable());
        }
    }
}
