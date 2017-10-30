// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using Windows.ApplicationModel;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls;
using System;

namespace AudioVideoPlayer.Pages.About
{
    public sealed partial class AboutPage : Page 
    {
        Windows.UI.Xaml.Controls.Button hamburgerMenuButton
        {
            get
            {
                return Shell.Current.GetHamburgerMenu().GetHamburgerMenuButton();
            }
        }
    

        public string NameOfSystemFamily { get { return nameof(Information.SystemInformation.SystemFamily); } }
        public string SystemFamily { get { return Information.SystemInformation.SystemFamily; } }
        public string SystemVersion { get { return Information.SystemInformation.SystemVersion; } }
        public string SystemArchitecture { get { return Information.SystemInformation.SystemArchitecture; } }
        public string ApplicationName { get { return Information.SystemInformation.ApplicationName; } }
        public string ApplicationVersion { get { return Information.SystemInformation.ApplicationVersion; } }
        public string DeviceName { get { return Information.SystemInformation.DeviceName; } }
        public string DeviceManufacturer { get { return Information.SystemInformation.DeviceManufacturer; } }
        public string DeviceModel { get { return Information.SystemInformation.DeviceModel; } }
        public string AppSpecificHardwareID { get{ return Information.SystemInformation.AppSpecificHardwareID.Replace('-',' '); }}
        public string PackageFamilyName {get { return Information.SystemInformation.PackageFamilyName; } }
        public AboutPage()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.Loaded += AboutPage_Loaded;
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.Loaded -= AboutPage_Loaded;
        }

        private void AboutPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (hamburgerMenuButton != null)
            {
                hamburgerMenuButton.Focus(Windows.UI.Xaml.FocusState.Programmatic);
            }
        }
    }
}
