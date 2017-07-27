//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using AudioVideoPlayer.DataModel;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using System.Reflection;
using AudioVideoPlayer.Companion;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Text.RegularExpressions;
using Windows.Networking.Connectivity;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AudioVideoPlayer.Pages.Settings
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {

        /// <summary>
        /// PlaylistPage constructor 
        /// </summary>
        public SettingsPage()
        {
            this.InitializeComponent();
            // Show FullWindow on phone
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                // Show fullWindow button
                WindowModeFull.Visibility = Visibility.Collapsed;
            }
            string s = CompanionClient.GetNetworkAdapterIPAddress();
            if (!string.IsNullOrEmpty(s))
                IPAddress.Text = s; 
        }
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected  override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }

        private void ApplyColor_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Color c = (Windows.UI.Color) ColorCombo.SelectedItem;
            // Save Theme
            ViewModelLocator.Settings.DarkTheme = AppThemeSwitch.IsOn;
            // Save the new Color
            ViewModelLocator.Settings.MenuBackgroundColor = c;
            // Refresh the pages with the new Color                         
            AudioVideoPlayer.Shell.Current.UpdateTitleBarAndColor(true);
        }
        /// <summary>
        /// This method is called when the UDPPort fields are changed
        /// </summary>
        void UDPPortChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                uint n;
                if (!uint.TryParse(tb.Text, out n))
                {
                    if (tb == MulticastUDPPort)
                        tb.Text = ViewModels.StaticSettingsViewModel.MulticastUDPPort.ToString();
                    if (tb == UnicastUDPPort)
                        tb.Text = ViewModels.StaticSettingsViewModel.UnicastUDPPort.ToString();
                }
                else
                {

                    if ((n<=0)||(n>65535)) 
                    {
                        if (tb == MulticastUDPPort)
                            tb.Text = ViewModels.StaticSettingsViewModel.MulticastUDPPort.ToString();
                        if (tb == UnicastUDPPort)
                            tb.Text = ViewModels.StaticSettingsViewModel.UnicastUDPPort.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// This method is called when the IPAddress fields are changed
        /// </summary>
        void IPAddressChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                if (!CompanionClient.IsIPv4Address(tb.Text))
                {
                        tb.Text = ViewModels.StaticSettingsViewModel.MulticastIPAddress.ToString();
                }
            }
        }
    }


}
