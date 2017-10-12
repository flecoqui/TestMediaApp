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
        int GetBuildNumber(string version)
        {
            int result = 0;
            char[] sep = { '.' };
            string[] res = version.Split(sep);
            if((res!=null)&&(res.Count()==4))
            {
                int.TryParse(res[2], out result);
            }
            return result;
        }
        /// <summary>
        /// PlaylistPage constructor 
        /// </summary>
        public SettingsPage()
        {
            this.InitializeComponent();
            ShowPointer();
            // Show FullWindow on phone
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                // Show fullWindow button
                WindowModeFull.Visibility = Visibility.Collapsed;
            }
            if (string.Equals(Information.SystemInformation.SystemFamily, "Windows.Desktop", StringComparison.OrdinalIgnoreCase) &&
                (GetBuildNumber(Information.SystemInformation.SystemVersion)>16299))
            {
                ApplicationStartHeaderPanel.Visibility = Visibility.Visible;
                ApplicationStartContentPanel.Visibility = Visibility.Visible;
            }
            else
            {
                ApplicationStartHeaderPanel.Visibility = Visibility.Collapsed;
                ApplicationStartContentPanel.Visibility = Visibility.Collapsed;
            }
            string s = MulticastCompanionConnectionManager.GetNetworkAdapterIPAddress();
            if (!string.IsNullOrEmpty(s))
                IPAddress.Text = s; 
        }
        // Display pointer as a mouse (XBOX Only)
        public void ShowPointer()
        {
            if (string.Equals(Information.SystemInformation.SystemFamily, "Windows.Xbox", StringComparison.OrdinalIgnoreCase))
                RequiresPointer = RequiresPointer.WhenFocused;
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

        public bool Reload()
        {
            if (!this.Frame.BackStack.Any())
                return false;
            int backStackCount = this.Frame.BackStack.Count;
            if (backStackCount > 0)
            {
                var current = this.Frame.BackStack[backStackCount - 1];
                if(current.SourcePageType == this.GetType())
                    this.Frame.BackStack.RemoveAt(backStackCount - 1);
                return this.Frame.Navigate(this.GetType(), null);
            }
            return false;
        }
        private void ApplyColor_Click(object sender, RoutedEventArgs e)
        {
            if (ColorCombo.SelectedItem != null)
            {
                Windows.UI.Color c = (Windows.UI.Color)ColorCombo.SelectedItem;
                if (c != ViewModelLocator.Settings.MenuBackgroundColor)
                {
                    // Save the new Color
                    ViewModelLocator.Settings.MenuBackgroundColor = c;
                    // Refresh the pages with the new Color                         
                    AudioVideoPlayer.Shell.Current.UpdateTitleBarAndColor(true);
                    Reload();
                }
            }
        }
        private void ApplyTheme_Click(object sender, RoutedEventArgs e)
        {
            if (ColorCombo.SelectedItem != null)
            {
                // Save Theme
                ViewModelLocator.Settings.LightTheme = AppThemeSwitch.IsOn;
                // Refresh the pages with the new Color                         
                AudioVideoPlayer.Shell.Current.UpdateTitleBarAndColor(true);
            }
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
                if (!MulticastCompanionConnectionManager.IsIPv4Address(tb.Text))
                {
                        tb.Text = ViewModels.StaticSettingsViewModel.MulticastIPAddress.ToString();
                }
            }
        }

        private void ColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyColor_Click(null,null);
        }

        private async  void ApplicationStart_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    Windows.ApplicationModel.StartupTask startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("TestMediaAppStartupId");
                    switch (startupTask.State)
                    {
                        case Windows.ApplicationModel.StartupTaskState.Disabled:
                            // Task is disabled but can be enabled.
                            Windows.ApplicationModel.StartupTaskState newState = await startupTask.RequestEnableAsync();
                            System.Diagnostics.Debug.WriteLine("Request to enable startup, result = {0}", newState);
                            break;
                        case Windows.ApplicationModel.StartupTaskState.DisabledByUser:
                            // Task is disabled and user must enable it manually.
                            Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(
                "I know you don't want this app to run " +
                "as soon as you sign in, but if you change your mind, " +
                "you can enable this in the Startup tab in Task Manager.",
                "TestStartup");
                            await dialog.ShowAsync();
                            toggleSwitch.IsOn = false;
                            break;
                        case Windows.ApplicationModel.StartupTaskState.DisabledByPolicy:
                            System.Diagnostics.Debug.WriteLine(
                            "Startup disabled by group policy, or not supported on this device");
                            toggleSwitch.IsOn = false;
                            break;
                        case Windows.ApplicationModel.StartupTaskState.Enabled:
                            System.Diagnostics.Debug.WriteLine("Startup is enabled.");
                            break;
                    }
                }
                else
                {

                }
            }
        }
    }


}
