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
using System.ComponentModel;
using System.Runtime.CompilerServices;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AudioVideoPlayer.Pages.Heos
{


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HeosPage : Page
    {

        /// <summary>
        /// HeosPage constructor 
        /// </summary>
        public HeosPage()
        {
            this.InitializeComponent();
        }
        Windows.UI.Xaml.Controls.Button hamburgerMenuButton
        {
            get
            {
                return Shell.Current.GetHamburgerMenu().GetHamburgerMenuButton();
            }
        }
        /// <summary>
        /// UpdateControls Method which update the controls on the page  
        /// </summary>
        async void UpdateControls(bool bDisable = false)
        {

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                 () =>
                 {


                 });
        }
        private void comboPlayList_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (comboPlayList.Items.Count > 0)
            {
                ViewModels.ViewModel vm = this.DataContext as ViewModels.ViewModel;
                if (vm != null)
                {
                    string PlaylistPath = vm.Settings.CurrentPlayListPath;
                    int index = 0;
                    foreach (var item in comboPlayList.Items)
                    {
                        if (item is Models.PlayList)
                        {
                            Models.PlayList p = item as Models.PlayList;
                            if (p != null)
                            {
                                if (string.Equals(p.Path, PlaylistPath))
                                {
                                    comboPlayList.SelectedIndex = index;
                                    break;
                                }
                                if (string.Equals(p.ImportedPath, PlaylistPath))
                                {
                                    comboPlayList.SelectedIndex = index;
                                    break;
                                }
                            }
                        }
                        index++;
                    }
                    if (comboPlayList.SelectedIndex < 0)
                        comboPlayList.SelectedIndex = 0;
                }
            }
        }
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

            // Register Network
            RegisterNetworkHelper();

            // Update playlist controls
            UpdateControls();

            // Logs updated
            logs.TextChanged += Logs_TextChanged;

            // Select first item in the combo box to select multicast option
            comboDevice.DataContext = ViewModels.StaticSettingsViewModel.HeosSpeakerList;
            if (comboDevice.Items.Count > 0)
            {
                comboDevice.SelectedIndex = 0;
                PageStatus = Status.DeviceSelected;
            }
            else
                PageStatus = Status.NoDeviceSelected;

        }

        /// <summary>
        /// Method OnNavigatedFrom
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {

            // Unregister Network
            UnregisterNetworkHelper();


        }

        private void comboPlayList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (comboPlayList.SelectedItem is Models.PlayList)
            {
                Models.PlayList p = (Models.PlayList)comboPlayList.SelectedItem;
                if(p!=null)
                {
                    if(!string.IsNullOrEmpty(p.ImportedPath))
                    {
                        ViewModelLocator.Settings.CurrentPlayListPath = p.ImportedPath;
                        ViewModelLocator.Settings.CurrentPlayListIndex = comboPlayList.SelectedIndex;
                        ViewModelLocator.Settings.CurrentMediaPath = string.Empty;
                        ViewModelLocator.Settings.CurrentMediaIndex = p.Index;
                    }
                    else if (!string.IsNullOrEmpty(p.Path))
                    {
                        ViewModelLocator.Settings.CurrentPlayListPath = p.Path;
                        ViewModelLocator.Settings.CurrentPlayListIndex = comboPlayList.SelectedIndex;
                        ViewModelLocator.Settings.CurrentMediaPath = string.Empty;
                        ViewModelLocator.Settings.CurrentMediaIndex = p.Index;
                    }
                }
            }
        }




        #region Logs
        void PushMessage(string Message)
        {
            App app = Windows.UI.Xaml.Application.Current as App;
            if (app != null)
                app.MessageList.Enqueue(Message);
        }
        bool PopMessage(out string Message)
        {
            Message = string.Empty;
            App app = Windows.UI.Xaml.Application.Current as App;
            if (app != null)
                return app.MessageList.TryDequeue(out Message);
            return false;
        }
        /// <summary>
        /// Display Message on the application page
        /// </summary>
        /// <param name="Message">String to display</param>
        async void LogMessage(string Message)
        {
            string Text = string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " " + Message + "\n";
            PushMessage(Text);
            System.Diagnostics.Debug.WriteLine(Text);
            await DisplayLogMessage();
        }
        /// <summary>
        /// Display Message on the application page
        /// </summary>
        /// <param name="Message">String to display</param>
        async System.Threading.Tasks.Task<bool> DisplayLogMessage()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {

                    string result;
                    while (PopMessage(out result))
                    {
                        logs.Text += result;
                        if (logs.Text.Length > 16000)
                        {
                            string LocalString = logs.Text;
                            while (LocalString.Length > 12000)
                            {
                                int pos = LocalString.IndexOf('\n');
                                if (pos == -1)
                                    pos = LocalString.IndexOf('\r');


                                if ((pos >= 0) && (pos < LocalString.Length))
                                {
                                    LocalString = LocalString.Substring(pos + 1);
                                }
                                else
                                    break;
                            }
                            logs.Text = LocalString;
                        }
                    }
                }
            );
            return true;
        }
        /// <summary>
        /// This method is called when the content of the Logs TextBox changed  
        /// The method scroll to the bottom of the TextBox
        /// </summary>
        void Logs_TextChanged(object sender, TextChangedEventArgs e)
        {
            //  logs.Focus(FocusState.Programmatic);
            // logs.Select(logs.Text.Length, 0);
            var tbsv = GetFirstDescendantScrollViewer(logs);
            tbsv.ChangeView(null, tbsv.ScrollableHeight, null, true);
        }
        /// <summary>
        /// Retrieve the ScrollViewer associated with a control  
        /// </summary>
        ScrollViewer GetFirstDescendantScrollViewer(DependencyObject parent)
        {
            var c = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < c; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var sv = child as ScrollViewer;
                if (sv != null)
                    return sv;
                sv = GetFirstDescendantScrollViewer(child);
                if (sv != null)
                    return sv;
            }

            return null;
        }
        #endregion



        #region Network

        Helpers.NetworkHelper networkHelper;

        bool RegisterNetworkHelper()
        {
            UnregisterNetworkHelper();
            if (networkHelper == null)
            {
                networkHelper = new Helpers.NetworkHelper();
                networkHelper.InternetConnectionChanged += NetworkHelper_InternetConnectionChanged;
                NetworkHelper_InternetConnectionChanged(this, Helpers.NetworkHelper.IsInternetAvailable());
            }
            return true;
        }
        bool IsNetworkRequired()
        {
            bool bNetworkRequired = false;
            string s = ViewModels.StaticSettingsViewModel.CurrentPlayListPath;
            if (!string.IsNullOrEmpty(s))
            {
                if (s.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    bNetworkRequired = true;
                else if (s.StartsWith("redirect://", StringComparison.OrdinalIgnoreCase))
                    bNetworkRequired = true;
                else if (s.StartsWith("redirects://", StringComparison.OrdinalIgnoreCase))
                    bNetworkRequired = true;

                var p = ViewModelLocator.Settings.PlayListList.FirstOrDefault(x => string.Equals(x.Path, s, StringComparison.OrdinalIgnoreCase) || string.Equals(x.ImportedPath, s, StringComparison.OrdinalIgnoreCase));
                if (p != null)
                {
                    if (p.bRemoteItem == true)
                        bNetworkRequired = true;
                }
            }
            return bNetworkRequired;
        }
        private async void NetworkHelper_InternetConnectionChanged(object sender, bool e)
        {
            if (e == true)
            {
                await Shell.Current.DisplayNetworkWarning(false, "");
            }
            else
            {
                if (IsNetworkRequired())
                {
                    await Shell.Current.DisplayNetworkWarning(true, "The current playlist: " + ViewModels.StaticSettingsViewModel.CurrentPlayListPath + " requires an internet connection");
                }
            }

        }

        bool UnregisterNetworkHelper()
        {
            if (networkHelper != null)
            {
                networkHelper.InternetConnectionChanged -= NetworkHelper_InternetConnectionChanged;
                networkHelper = null;
            }
            return true;
        }



        #endregion Network


        #region Heos
        AudioVideoPlayer.Heos.HeosSpeakerConnectionManager heosSpeakerConnectionManager;
        enum Status
        {
            NoDeviceSelected = 0,
            DeviceSelected,
            AddingDevice
        }

        Status PageStatus = Status.DeviceSelected;
        private void comboDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            
            if (comboDevice.SelectedItem is AudioVideoPlayer.Heos.HeosSpeaker)
            {
                AudioVideoPlayer.Heos.HeosSpeaker p = (AudioVideoPlayer.Heos.HeosSpeaker)comboDevice.SelectedItem;
                if (p != null)
                    PageStatus = Status.DeviceSelected;
                else
                    PageStatus = Status.NoDeviceSelected;
            }
            UpdateControls();

        }
        private void comboDevice_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            RemoveDeviceButton.IsEnabled = false;
            if (comboDevice.Items.Count > 0)
            {
                PageStatus = Status.DeviceSelected;
                comboDevice.SelectedIndex = 0;
            }
            else
                PageStatus = Status.NoDeviceSelected;
            UpdateControls();
        }
        private async void DiscoverDevice_Click(object sender, RoutedEventArgs e)
        {
            if ((heosSpeakerConnectionManager == null) || ((heosSpeakerConnectionManager != null) && (!heosSpeakerConnectionManager.IsDiscovering())))
            {
                if(await StartDiscovery() == true)
                {
                    LogMessage("Start Discovering Heos Speaker ...");
                    DiscoverDeviceButton.Content = "\xE894";
                }
                else
                    LogMessage("Error while starting Discovering Devices ...");
            }
            else
            {
                LogMessage("Stop Discovering Heos Speaker ...");
                StopDiscovery();
                DiscoverDeviceButton.Content = "\xE895";
            }
            UpdateControls();
        }
        private void RemoveDevice_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Removing Device ...");
            if (comboDevice.SelectedItem is AudioVideoPlayer.Heos.HeosSpeaker)
            {
                ObservableCollection<AudioVideoPlayer.Heos.HeosSpeaker> pll = ViewModelLocator.Settings.HeosSpeakerList;
                if ((pll != null) && (comboDevice.SelectedIndex < pll.Count))
                {
                    pll.RemoveAt(comboDevice.SelectedIndex);
                }
                ViewModelLocator.Settings.HeosSpeakerList = pll;
                if (comboDevice.Items.Count > 0)
                {

                    PageStatus = Status.DeviceSelected;
                    comboDevice.SelectedIndex = 0;
                }
                else
                {
                    PageStatus = Status.NoDeviceSelected;
                }

            }

            UpdateControls();
        }
        private async void Play_Click(object sender, RoutedEventArgs e)
        {

            if (comboDevice.SelectedItem is AudioVideoPlayer.Heos.HeosSpeaker)
            {
                AudioVideoPlayer.Heos.HeosSpeaker hs = comboDevice.SelectedItem as AudioVideoPlayer.Heos.HeosSpeaker;
                if(hs!=null)
                {
                    LogMessage("Play url " + mediaUri.Text + " on Speaker: " + hs.FriendlyName);
                    bool result = await hs.PlayUrl(mediaUri.Text);
                    if(result == true)
                    {
                        LogMessage("Play url " + mediaUri.Text + " on Speaker: " + hs.FriendlyName + " successful");
                    }
                    else
                    {
                        LogMessage("Play url " + mediaUri.Text + " on Speaker: " + hs.FriendlyName + " error");
                    }
                }
            }

            UpdateControls();
        }
        #endregion


        #region

        async System.Threading.Tasks.Task<bool> StartDiscovery()
        {
            bool result = false;
            try
            { 
                if (heosSpeakerConnectionManager == null)
                {
                    heosSpeakerConnectionManager = new AudioVideoPlayer.Heos.HeosSpeakerConnectionManager();
                    if (heosSpeakerConnectionManager != null)
                    {
                        heosSpeakerConnectionManager.Initialize();
                    }
                }
                if (heosSpeakerConnectionManager != null)
                {
                    if (await heosSpeakerConnectionManager.StartDiscovery() == true)
                    {
                        heosSpeakerConnectionManager.HeosSpeakerAdded += HeosSpeakerConnectionManager_HeosSpeakerAdded;
                        heosSpeakerConnectionManager.HeosSpeakerUpdated += HeosSpeakerConnectionManager_HeosSpeakerUpdated;
                        result = true;
                    }
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.Write("Exception while starting HEOS discovery: " + ex.Message) ;
            }
            return result;
        }

        private void HeosSpeakerConnectionManager_HeosSpeakerUpdated(AudioVideoPlayer.Heos.HeosSpeakerConnectionManager sender, AudioVideoPlayer.Heos.HeosSpeaker args)
        {
           // LogMessage("Updated Device: " + args.FriendlyName + " IP: " + args.Ip);
        }
        bool IsSpeakerRegistered(string Id)
        {
            bool result = false;
            foreach (var d in ViewModels.StaticSettingsViewModel.HeosSpeakerList)
            {
                if (d.Id == Id)
                    return true;
            }
            return result;
        }
        void UpdateSelection(string Id)
        {
            int index = 0;
            PageStatus = Status.NoDeviceSelected;
            foreach (var item in comboDevice.Items)
            {
                if (item is AudioVideoPlayer.Heos.HeosSpeaker)
                {
                    AudioVideoPlayer.Heos.HeosSpeaker p = item as AudioVideoPlayer.Heos.HeosSpeaker;
                    if (p != null)
                    {
                        if (string.Equals(p.Id, Id))
                        {
                            comboDevice.SelectedIndex = index;
                            PageStatus = Status.DeviceSelected;
                            return;
                        }
                    }
                }
                index++;
            }
            if (comboDevice.Items.Count > 0)
            {
                comboDevice.SelectedIndex = 0;
                PageStatus = Status.DeviceSelected;
            }
            return;
        }
        private async void HeosSpeakerConnectionManager_HeosSpeakerAdded(AudioVideoPlayer.Heos.HeosSpeakerConnectionManager sender, AudioVideoPlayer.Heos.HeosSpeaker args)
        {
            if(args.IsHeosDevice())
                LogMessage("Added HEOS Device: " + args.FriendlyName + " IP: " + args.Ip);
            else
                LogMessage("Added Device: " + args.FriendlyName + " IP: " + args.Ip);


            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ObservableCollection<AudioVideoPlayer.Heos.HeosSpeaker> speakerList = ViewModelLocator.Settings.HeosSpeakerList;
                if (speakerList != null)
                {
                    AudioVideoPlayer.Heos.HeosSpeaker d = speakerList.FirstOrDefault(device => string.Equals(device.Id, args.Id));
                    if (d == null)
                    {
                        LogMessage("Device: " + args.Id + " IP address: " + args.Ip + " added");
                        speakerList.Add(new AudioVideoPlayer.Heos.HeosSpeaker(args.Id, args.Location, args.Version, args.IpCache, args.Server, args.St, args.Usn, args.Ip, args.FriendlyName, args.Manufacturer, args.ModelName, args.ModelNumber));
                    }
                    else
                    {
                        if ((!string.Equals(d.Ip, args.Ip) && (string.IsNullOrEmpty(d.Ip))))
                        {
                            args.Id = d.Id;
                            speakerList.Remove(d);
                            speakerList.Add(args);
                        }
                    }

                    ViewModelLocator.Settings.HeosSpeakerList = speakerList;
                    UpdateSelection(args.Id);
                }
            });

//            if (!IsSpeakerRegistered(args.Id))
//                ViewModels.StaticSettingsViewModel.HeosSpeakerList.Add(args);
        }

        bool StopDiscovery()
        {
            bool result = false;
            try
            {
                if (heosSpeakerConnectionManager != null)
                {
                    heosSpeakerConnectionManager.HeosSpeakerAdded -= HeosSpeakerConnectionManager_HeosSpeakerAdded;
                    heosSpeakerConnectionManager.HeosSpeakerUpdated -= HeosSpeakerConnectionManager_HeosSpeakerUpdated;
                    heosSpeakerConnectionManager.StopDiscovery();
                    heosSpeakerConnectionManager.Uninitialize();
                    heosSpeakerConnectionManager = null;
                    result = true;
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.Write("Exception while stopping HEOS discovery: " + ex.Message) ;
            }
            return result;
        }
        #endregion
    }


}
