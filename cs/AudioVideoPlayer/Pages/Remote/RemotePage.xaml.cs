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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AudioVideoPlayer.Pages.Remote
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RemotePage : Page
    {

        enum Status
        {
            NoDeviceSelected = 0,
            DeviceSelected,
            AddingDevice
        }

        Status PageStatus = Status.DeviceSelected;

        /// <summary>
        /// PlaylistPage constructor 
        /// </summary>
        public RemotePage()
        {
            this.InitializeComponent();
            PageStatus = Status.NoDeviceSelected;
            ShowPointer();
            UpdateControls();

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
        protected  async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.

            // Logs event to refresh the TextBox
            logs.TextChanged += Logs_TextChanged;
            //
            LogMessage("RemotePage OnNavigatedTo");
            // Initialize the Companion mode (Remote or Player)
            await InitializeCompanion();
            // Register Network
            RegisterNetworkHelper();


            // Clear Device list before starting disovery
            if (ViewModels.StaticSettingsViewModel.DeviceList.Count > 0)
            {
                // Clear Device List
                ViewModels.StaticSettingsViewModel.DeviceList = new ObservableCollection<CompanionDevice>();
            }
            // Select first item in the combo box to select multicast option
            comboDevice.DataContext = ViewModels.StaticSettingsViewModel.DeviceList;
            if (comboDevice.Items.Count > 0)
            {
                comboDevice.SelectedIndex = 0;
                PageStatus = Status.DeviceSelected;
            }
            else
                PageStatus = Status.NoDeviceSelected;

            // Show FullWindow on phone
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                // Show fullWindow button
                fullwindowButton.Visibility = Visibility.Visible;
            }

            // start discovering devices
            if (companionConnectionManager != null)
            {
                if (!companionConnectionManager.IsDiscovering())
                {
                    if (await companionConnectionManager.StartDiscovery() == true)
                    {
                        LogMessage("Start Discovering Devices ...");
                        DiscoverDeviceButton.Content = "\xE894";
                    }
                    else
                        LogMessage("Error while starting Discovering Devices ...");
                }
            }
            bRemotePlayerPageOpened = false;
            // Update Controls
            UpdateControls();
        }
        /// <summary>
        /// Method OnNavigatedFrom
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {

            // Stop Companion reception
            UninitializeCompanion();
            // Unregister Network
            UnregisterNetworkHelper();


        }

        /// <summary>
        /// UpdateControls Method which update the controls on the page  
        /// </summary>
        async void UpdateControls(bool bDisable = false)
        {

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                 () =>
                 {
                     if (PageStatus == Status.DeviceSelected)
                     {

                         if ( (((ViewModelLocator.Settings.UdpTransport==true) || (ViewModelLocator.Settings.MulticastDiscovery == true)) &&(!string.IsNullOrEmpty(GetIPAddress()))) ||
                               (((ViewModelLocator.Settings.UdpTransport == false) && (ViewModelLocator.Settings.MulticastDiscovery == false)) &&(IsCurrentDeviceConnected()) && (IsCurrentDevicePlayerPageOpened())))
                         {
                             playButton.IsEnabled = true;
                             playPauseButton.IsEnabled = true;
                             pausePlayButton.IsEnabled = true;
                             stopButton.IsEnabled = true;

                             minusButton.IsEnabled = true;
                             plusButton.IsEnabled = true;

                             minusPlaylistButton.IsEnabled = true;
                             plusPlaylistButton.IsEnabled = true;

                             muteButton.IsEnabled = true;
                             volumeDownButton.IsEnabled = true;
                             volumeUpButton.IsEnabled = true;

                             fullscreenButton.IsEnabled = true;
                             fullwindowButton.IsEnabled = true;

                             contentUri.IsEnabled = true;
                             playlistUri.IsEnabled = true;
                             contentNumber.IsEnabled = true;


                             playPlaylistButton.IsEnabled = true;
                             playContentButton.IsEnabled = true;
                             selectContentButton.IsEnabled = true;

                             UpRemoteButton.IsEnabled = true;
                             DownRemoteButton.IsEnabled = true;
                             LeftRemoteButton.IsEnabled = true;
                             RightRemoteButton.IsEnabled = true;
                             EnterRemoteButton.IsEnabled = true;
                         }
                         else
                         {
                             playButton.IsEnabled = false;
                             playPauseButton.IsEnabled = false;
                             pausePlayButton.IsEnabled = false;
                             stopButton.IsEnabled = false;

                             minusButton.IsEnabled = false;
                             plusButton.IsEnabled = false;

                             minusPlaylistButton.IsEnabled = false;
                             plusPlaylistButton.IsEnabled = false;

                             muteButton.IsEnabled = false;
                             volumeDownButton.IsEnabled = false;
                             volumeUpButton.IsEnabled = false;

                             fullscreenButton.IsEnabled = false;
                             fullwindowButton.IsEnabled = false;

                             contentUri.IsEnabled = false;
                             playlistUri.IsEnabled = false;
                             contentNumber.IsEnabled = false;


                             playPlaylistButton.IsEnabled = false;
                             playContentButton.IsEnabled = false;
                             selectContentButton.IsEnabled = false;

                             UpRemoteButton.IsEnabled = false;
                             DownRemoteButton.IsEnabled = false;
                             LeftRemoteButton.IsEnabled = false;
                             RightRemoteButton.IsEnabled = false;
                             EnterRemoteButton.IsEnabled = false;

                         }
                         comboDevice.IsEnabled = true;
                         DiscoverDeviceButton.IsEnabled = true;
                        // AddDeviceButton.IsEnabled = true;
                         RemoveDeviceButton.IsEnabled = true;
                         DeviceName.IsReadOnly = true;
                         DeviceIPAddress.IsReadOnly = true;

                         playerpageButton.IsEnabled = true;
                         playlistpageButton.IsEnabled = true;
                         remotepageButton.IsEnabled = true;
                         settingspageButton.IsEnabled = true;
                         aboutpageButton.IsEnabled = true;

                     }
                     else if (PageStatus == Status.NoDeviceSelected)
                     {
                         playButton.IsEnabled = false;
                         playPauseButton.IsEnabled = false;
                         pausePlayButton.IsEnabled = false;
                         stopButton.IsEnabled = false;

                         minusButton.IsEnabled = false;
                         plusButton.IsEnabled = false;

                         minusPlaylistButton.IsEnabled = false;
                         plusPlaylistButton.IsEnabled = false;

                         muteButton.IsEnabled = false;
                         volumeDownButton.IsEnabled = false;
                         volumeUpButton.IsEnabled = false;

                         fullscreenButton.IsEnabled = false;
                         fullwindowButton.IsEnabled = false;

                         comboDevice.IsEnabled = false;
                         contentUri.IsEnabled = false;
                         playlistUri.IsEnabled = false;
                         contentNumber.IsEnabled = false;


                         playPlaylistButton.IsEnabled = false;
                         playContentButton.IsEnabled = false;
                         selectContentButton.IsEnabled = false;

                         UpRemoteButton.IsEnabled = false;
                         DownRemoteButton.IsEnabled = false;
                         LeftRemoteButton.IsEnabled = false;
                         RightRemoteButton.IsEnabled = false;
                         EnterRemoteButton.IsEnabled = false;

                         DiscoverDeviceButton.IsEnabled = true;
                         //AddDeviceButton.IsEnabled = true;
                         RemoveDeviceButton.IsEnabled = false;
                         DeviceName.IsReadOnly = true;
                         DeviceIPAddress.IsReadOnly = true;

                         playerpageButton.IsEnabled = false;
                         playlistpageButton.IsEnabled = false;
                         remotepageButton.IsEnabled = false;
                         settingspageButton.IsEnabled = false;
                         aboutpageButton.IsEnabled = false;
                     }
                     else if (PageStatus == Status.AddingDevice)
                     {
                         playButton.IsEnabled = false;
                         playPauseButton.IsEnabled = false;
                         pausePlayButton.IsEnabled = false;
                         stopButton.IsEnabled = false;

                         minusButton.IsEnabled = false;
                         plusButton.IsEnabled = false;

                         minusPlaylistButton.IsEnabled = false;
                         plusPlaylistButton.IsEnabled = false;

                         muteButton.IsEnabled = false;
                         volumeDownButton.IsEnabled = false;
                         volumeUpButton.IsEnabled = false;

                         fullscreenButton.IsEnabled = false;
                         fullwindowButton.IsEnabled = false;

                         comboDevice.IsEnabled = false;
                         contentUri.IsEnabled = false;
                         playlistUri.IsEnabled = false;
                         contentNumber.IsEnabled = false;

                         playPlaylistButton.IsEnabled = false;
                         playContentButton.IsEnabled = false;
                         selectContentButton.IsEnabled = false;

                         UpRemoteButton.IsEnabled = false;
                         DownRemoteButton.IsEnabled = false;
                         LeftRemoteButton.IsEnabled = false;
                         RightRemoteButton.IsEnabled = false;
                         EnterRemoteButton.IsEnabled = false;

                         DiscoverDeviceButton.IsEnabled = false;
                         //AddDeviceButton.IsEnabled = true;
                         RemoveDeviceButton.IsEnabled = false;
                         DeviceName.IsReadOnly = false;
                         DeviceIPAddress.IsReadOnly = false;

                         playerpageButton.IsEnabled = false;
                         playlistpageButton.IsEnabled = false;
                         remotepageButton.IsEnabled = false;
                         settingspageButton.IsEnabled = false;
                         aboutpageButton.IsEnabled = false;
                     }

                 });
        }
        CompanionDevice GetCurrentDevice()
        {
            CompanionDevice cd = comboDevice.SelectedItem as CompanionDevice;
            return cd;
        }
        private bool bRemotePlayerPageOpened;
        bool IsCurrentDevicePlayerPageOpened()
        {
            return bRemotePlayerPageOpened;
        }
        bool IsCurrentDeviceConnected()
        {
            CompanionDevice cd = comboDevice.SelectedItem as CompanionDevice;
            if(cd!=null)
            {
                if ((companionConnectionManager!=null) &&(companionConnectionManager.IsCompanionDeviceConnected(cd)))
                {
                    cd.Status = CompanionDeviceStatus.Connected;
                    return true;
                }
            }
            return false;
        }
        string GetIPAddress()
        {
            Companion.CompanionDevice d = comboDevice.SelectedItem as Companion.CompanionDevice;
            if (d != null)
            {
                if (!string.IsNullOrEmpty(d.IPAddress))
                    return d.IPAddress;
            }
            return string.Empty;
        }
        string GetName()
        {
            Companion.CompanionDevice d = comboDevice.SelectedItem as Companion.CompanionDevice;
            if (d != null)
            {
                if (!string.IsNullOrEmpty(d.Name))
                    return d.Name;
            }
            return string.Empty;
        }
        /// <summary>
        /// Channel up method 
        /// </summary>
        private void Plus_Click(object sender, RoutedEventArgs e)
        {
                Plus_remote_Click(sender, e);
        }
        /// <summary>
        /// Channel down method 
        /// </summary>
        private void Minus_Click(object sender, RoutedEventArgs e)
        {
                Minus_remote_Click(sender, e);
        }


        /// <summary>
        /// Mute method 
        /// </summary>
        private void Mute_Click(object sender, RoutedEventArgs e)
        {
                Mute_remote_Click(sender, e);
        }
        /// <summary>
        /// Volume Up method 
        /// </summary>
        private void VolumeUp_Click(object sender, RoutedEventArgs e)
        {
                VolumeUp_remote_Click(sender, e);
        }
        /// <summary>
        /// Volume Down method 
        /// </summary>
        private void VolumeDown_Click(object sender, RoutedEventArgs e)
        {
                VolumeDown_remote_Click(sender, e);
        }

        #region Companion
        private CompanionConnectionManager companionConnectionManager;
        private CompanionDevice localCompanionDevice;

        private async System.Threading.Tasks.Task<bool> InitializeCompanion()
        {
            bool result = false;
            string multicast = string.Empty;
            UninitializeCompanion();
            if (companionConnectionManager == null)
            {


                if ((ViewModelLocator.Settings.MulticastDiscovery == false) &&
                    (ViewModelLocator.Settings.UdpTransport == false))
                {

                    companionConnectionManager = new CompanionConnectionManager();
                    if (companionConnectionManager != null)
                    {
                        localCompanionDevice = new CompanionDevice(string.Empty, false,Information.SystemInformation.DeviceName, companionConnectionManager.GetSourceIP(), Information.SystemInformation.SystemFamily);
                        companionConnectionManager.MessageReceived += CompanionConnectionManager_MessageReceived;
                        companionConnectionManager.CompanionDeviceAdded += CompanionConnectionManager_CompanionDeviceAdded;
                        companionConnectionManager.CompanionDeviceRemoved += CompanionConnectionManager_CompanionDeviceRemoved;
                        companionConnectionManager.CompanionDeviceUpdated += CompanionConnectionManager_CompanionDeviceUpdated;
                        CompanionConnectionManagerInitializeArgs args = new CompanionConnectionManagerInitializeArgs();
                        if (args != null)
                        {
                            args.ApplicationUri = "testmediaapp://?page=playerpage";
                            args.AppServiceName = "com.testmediaapp.companionservice";
                            //args.PackageFamilyName= "52458FLECOQUI.TestMediaApplication_h29hy11807230";
                            args.PackageFamilyName = Information.SystemInformation.PackageFamilyName;
                            result = await companionConnectionManager.Initialize(localCompanionDevice, args);
                        }
                    }
                }
                else
                {
                    multicast = "Multicast ";

                    companionConnectionManager = new MulticastCompanionConnectionManager();
                    if (companionConnectionManager != null)
                    {
                        localCompanionDevice = new CompanionDevice(string.Empty, false, Information.SystemInformation.DeviceName, companionConnectionManager.GetSourceIP(), Information.SystemInformation.SystemFamily);
                        companionConnectionManager.MessageReceived += CompanionConnectionManager_MessageReceived;
                        companionConnectionManager.CompanionDeviceAdded += CompanionConnectionManager_CompanionDeviceAdded;
                        companionConnectionManager.CompanionDeviceRemoved += CompanionConnectionManager_CompanionDeviceRemoved;
                        companionConnectionManager.CompanionDeviceUpdated += CompanionConnectionManager_CompanionDeviceUpdated;
                        MulticastCompanionConnectionManagerInitializeArgs args = new MulticastCompanionConnectionManagerInitializeArgs();
                        if (args != null)
                        {
                            args.ApplicationUri = "testmediaapp://?page=playerpage";
                            args.AppServiceName = "com.testmediaapp.companionservice";
                            //args.PackageFamilyName= "52458FLECOQUI.TestMediaApplication_h29hy11807230";
                            args.PackageFamilyName = Information.SystemInformation.PackageFamilyName;
                            args.MulticastDiscovery = ViewModelLocator.Settings.MulticastDiscovery;
                            args.UDPTransport = ViewModelLocator.Settings.UdpTransport;
                            args.MulticastIPAddress = ViewModelLocator.Settings.MulticastIPAddress;
                            args.MulticastUDPPort = ViewModelLocator.Settings.MulticastUDPPort;
                            args.UnicastUDPPort = ViewModelLocator.Settings.UnicastUDPPort;

                            result = await companionConnectionManager.Initialize(localCompanionDevice, args);
                        }
                    }

                }

            }
            if (result == true)
                LogMessage(multicast + "Companion Initialization ok");
            else
                LogMessage(multicast + "Companion Initialization Error");

            return true;
        }
        private bool UninitializeCompanion()
        {
            if (companionConnectionManager != null)
            {
                companionConnectionManager.MessageReceived -= CompanionConnectionManager_MessageReceived;
                companionConnectionManager.CompanionDeviceAdded -= CompanionConnectionManager_CompanionDeviceAdded;
                companionConnectionManager.CompanionDeviceRemoved -= CompanionConnectionManager_CompanionDeviceRemoved;
                companionConnectionManager.CompanionDeviceUpdated -= CompanionConnectionManager_CompanionDeviceUpdated;
                companionConnectionManager.Uninitialize();
                companionConnectionManager = null;
            }
            return true;
        }


        void UpdateSelection(string Id)
        {
            int index = 0;
            PageStatus = Status.NoDeviceSelected;
            foreach (var item in comboDevice.Items)
            {
                if (item is Companion.CompanionDevice)
                {
                    Companion.CompanionDevice p = item as Companion.CompanionDevice;
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
        private async void CompanionConnectionManager_CompanionDeviceUpdated(CompanionConnectionManager sender, CompanionDevice args)
        {
            LogMessage("Device: " + args.Name + " ID: " + args.Id + " IP address: " + args.IPAddress + " CompanionDeviceUpdated");

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ObservableCollection<Companion.CompanionDevice> deviceList = ViewModelLocator.Settings.DeviceList;
                if (deviceList != null)
                {
                    Companion.CompanionDevice d = deviceList.FirstOrDefault(device => string.Equals(device.Name, args.Name));
                    if (d == null)
                    {
                        LogMessage("Device: " + args.Name + " IP address: " + args.IPAddress + " added");
                        deviceList.Add(new Companion.CompanionDevice(args.Id, args.IsRemoteSystemDevice, args.Name, args.IPAddress, args.Kind));
                    }
                    else
                    {
                        if ((!string.Equals(d.IPAddress, args.IPAddress) && (string.IsNullOrEmpty(d.IPAddress))))
                        {
                            args.Id = d.Id;
                            deviceList.Remove(d);
                            deviceList.Add(args);
                            LogMessage("Device: " + args.Name + " IP address: " + args.IPAddress + " updated");
                        }
                    }
                    ViewModelLocator.Settings.DeviceList = deviceList;
                    UpdateSelection(args.Id);

                    /*
                    Companion.CompanionDevice d = deviceList.FirstOrDefault(device => string.Equals(device.Id, args.Id));
                    if (d != null)
                    {
                        deviceList.Remove(d);
                    }
                    deviceList.Add(new Companion.CompanionDevice(args.Id, args.Name, args.IPAddress, args.Kind));
                    ViewModelLocator.Settings.DeviceList = deviceList;
                    UpdateSelection(args.Id);
                    LogMessage("Device: " + args.Name + " IP address: " + args.IPAddress + " Updated ");
                    */
                }
            });
        }
        private async void CompanionConnectionManager_CompanionDeviceRemoved(CompanionConnectionManager sender, CompanionDevice args)
        {
            LogMessage("Device: " + args.Name + " ID: " + args.Id + " IP address: " + args.IPAddress + " CompanionDeviceRemoved");
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ObservableCollection<Companion.CompanionDevice> deviceList = ViewModelLocator.Settings.DeviceList;
                if (deviceList != null)
                {
                    Companion.CompanionDevice d = deviceList.FirstOrDefault(device => string.Equals(device.Id, args.Id));
                    if (d != null)
                    {
                        LogMessage("Device: " + d.Name + " IP address: " + d.IPAddress + " removed ");
                        deviceList.Remove(d);
                        ViewModelLocator.Settings.DeviceList = deviceList;
                        if (comboDevice.SelectedItem is Companion.CompanionDevice)
                        {
                            Companion.CompanionDevice sd = (Companion.CompanionDevice)comboDevice.SelectedItem;
                            ViewModelLocator.Settings.DeviceList = deviceList;
                            UpdateSelection(sd.Id);
                        }
                    }
                }
            });
        }

        private async void CompanionConnectionManager_CompanionDeviceAdded(CompanionConnectionManager sender, CompanionDevice args)
        {
            LogMessage("Device: " + args.Name + " ID: " + args.Id + " IP address: " + args.IPAddress + " CompanionDeviceAdded");
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ObservableCollection<Companion.CompanionDevice> deviceList = ViewModelLocator.Settings.DeviceList;
                if (deviceList != null)
                {
                    Companion.CompanionDevice d = deviceList.FirstOrDefault(device => string.Equals(device.Name, args.Name));
                    if (d == null)
                    {
                        LogMessage("Device: " + args.Name + " IP address: " + args.IPAddress + " added");
                        deviceList.Add(new Companion.CompanionDevice(args.Id, args.IsRemoteSystemDevice, args.Name, args.IPAddress, args.Kind));
                    }
                    else
                    {
                        if ((!string.Equals(d.IPAddress, args.IPAddress)&&(string.IsNullOrEmpty(d.IPAddress))))
                        {
                            args.Id = d.Id;
                            deviceList.Remove(d);
                            deviceList.Add(args);
                        }
                    }

                    ViewModelLocator.Settings.DeviceList = deviceList;
                    UpdateSelection(args.Id);
                }
            });

        }

        private void CompanionConnectionManager_MessageReceived(CompanionDevice sender, string args)
        {

            LogMessage("Received a remote command from device: " + sender.Name + " type: " + sender.Kind + " at IP Address: " + sender.IPAddress );
            LogMessage("Received a remote command : " + args);
        }



        private async void PlayerPage_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Opening Player Page remotely on " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
            {
                if (companionConnectionManager != null)
                {
                    bool result = await companionConnectionManager.CompanionDeviceOpenUri(cd, "testmediaapp://?page=playerpage");
                    if (result == true)
                    {
                        bRemotePlayerPageOpened = true;
                        LogMessage("Player Page sucessfully opened on " + GetName());
                    }
                    else
                    {
                        bRemotePlayerPageOpened = false;
                        LogMessage("Error while opening Player Page on " + GetName() + " Is the application installed? Is the device connected?");
                    }
                }
            }
            UpdateControls();

        }
        private async void PlaylistPage_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Opening Playlist Page remotely on " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
            {
                if (companionConnectionManager != null)
                {
                    bool result = await companionConnectionManager.CompanionDeviceOpenUri(cd, "testmediaapp://?page=playlistpage");
                    if (result == true)
                        LogMessage("Playlist Page sucessfully opened on " + GetName());
                    else
                        LogMessage("Error while opening Playlist Page on " + GetName() + " Is the application installed? Is the device connected?");
                    bRemotePlayerPageOpened = false;
                }
            }
            UpdateControls();
        }
        private async void RemotePage_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Opening Remote Page remotely on " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
            {
                if (companionConnectionManager != null)
                {
                    bool result = await companionConnectionManager.CompanionDeviceOpenUri(cd, "testmediaapp://?page=remotepage");
                    if (result == true)
                        LogMessage("Remote Page sucessfully opened on " + GetName());
                    else
                        LogMessage("Error while opening Remote Page on " + GetName() + " Is the application installed? Is the device connected?");
                    bRemotePlayerPageOpened = false;
                }
            }
            UpdateControls();

        }
        private async void SettingsPage_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Opening Settings Page remotely on " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
            {
                if (companionConnectionManager != null)
                {
                    bool result = await companionConnectionManager.CompanionDeviceOpenUri(cd, "testmediaapp://?page=settingspage");
                    if (result == true)
                        LogMessage("Settings Page sucessfully opened on " + GetName());
                    else
                        LogMessage("Error while opening Settings Page on " + GetName() + " Is the application installed? Is the device connected?");
                    bRemotePlayerPageOpened = false;
                }
            }
            UpdateControls();
        }

        private async void AboutPage_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Opening About Page remotely on " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
            {
                if (companionConnectionManager != null)
                {
                    bool result = await companionConnectionManager.CompanionDeviceOpenUri(cd, "testmediaapp://?page=aboutpage");
                    if (result == true)
                        LogMessage("About Page sucessfully opened on " + GetName());
                    else
                        LogMessage("Error while opening About Page on " + GetName() + " Is the application installed? Is the device connected?");
                    bRemotePlayerPageOpened = false;
                }
            }
            UpdateControls();
        }


        private async void Stop_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Stop event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandStop, null));
                }
            UpdateControls();

        }
        private async void Play_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Play event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandPlay, null));
                }
            UpdateControls();

        }
        private async void Playpause_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Playpause event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandPlayPause, null));
                }
            UpdateControls();

        }
        private async void Pause_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Pause event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandPause, null));
                }
            UpdateControls();


        }
        private async void Plus_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Plus event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandPlus, null));
                }
            UpdateControls();

        }
        private async void Minus_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Minus event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandMinus, null));
                }
            UpdateControls();
        }
        private async void PlusPlaylist_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Plus event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandPlusPlaylist, null));
                }
            UpdateControls();

        }
        private async void MinusPlaylist_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Minus event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandMinusPlaylist, null));
                }
            UpdateControls();
        }
        private async void Fullscreen_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Fullscreen event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandFullScreen, null));
                }
            UpdateControls();

        }
        private async void Fullwindow_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Fullwindow event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandFullWindow, null));
                }
            UpdateControls();
        }
        private async void Window_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending window event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandWindow, null));
                }
            UpdateControls();
        }
        private async void Mute_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Mute event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandMute, null));
                }
            UpdateControls();
        }
        private async void VolumeUp_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Volume Up event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandVolumeUp, null));
                }
            UpdateControls();

        }
        private async void VolumeDown_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Volume Down event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandVolumeDown, null));
                }
            UpdateControls();
        }
        private async void Down_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Down event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandDown, null));
                }
            UpdateControls();
        }
        private async void Up_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Up event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandUp, null));
                }
            UpdateControls();
        }
        private async void Left_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Left event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandLeft, null));
                }
            UpdateControls();
        }
        private async void Right_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Right event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandRight, null));
                }
            UpdateControls();
        }
        private async void Enter_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Enter event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandEnter, null));
                }
            UpdateControls();
        }

        private async void down_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Down event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandDown, null));
                }
            UpdateControls();
        }
        private async void up_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Up event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandUp, null));
                }
            UpdateControls();
        }
        private async void left_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Left event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandLeft, null));
                }
            UpdateControls();
        }
        private async void right_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Right event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandRight, null));
                }
            UpdateControls();
        }
        private async void enter_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Enter event to " + GetName());
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
                if (companionConnectionManager != null)
                {
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandEnter, null));
                }
            UpdateControls();
        }
        private async void open_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Open Media event to " + GetName() + " parameter: " + contentUri.Text);
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
            {
                if (companionConnectionManager != null)
                {
                    string commandString = CompanionProtocol.parameterContent + CompanionProtocol.cEQUAL + contentUri.Text;
                    Dictionary<string, string> p = CompanionProtocol.GetParametersFromMessage(commandString);
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandOpen, p));
                }
            }
            UpdateControls();
        }
        private async void openPlaylist_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Open Playlist event to " + GetName() + " parameter: " + playlistUri.Text);
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
            {
                if (companionConnectionManager != null)
                {
                    string commandString = CompanionProtocol.parameterContent + CompanionProtocol.cEQUAL + playlistUri.Text;
                    Dictionary<string, string> p = CompanionProtocol.GetParametersFromMessage(commandString);
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandOpenPlaylist, p));
                }
            }
            UpdateControls();
        }
        private async void select_remote_Click(object sender, RoutedEventArgs e)
        {

            LogMessage("Select event to " + GetName() + " parameter: " + contentNumber.Text);
            CompanionDevice cd = GetCurrentDevice();
            if (cd != null)
            {
                if (companionConnectionManager != null)
                {
                    string commandString = CompanionProtocol.parameterIndex + CompanionProtocol.cEQUAL + contentNumber.Text;
                    Dictionary<string, string> p = CompanionProtocol.GetParametersFromMessage(commandString);
                    await companionConnectionManager.Send(cd, CompanionProtocol.CreateCommand(CompanionProtocol.commandSelect, p));
                }
            }
            UpdateControls();

        }



        #endregion


        private void comboDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {


            if (comboDevice.SelectedItem is Companion.CompanionDevice)
            {
                Companion.CompanionDevice p = (Companion.CompanionDevice)comboDevice.SelectedItem;
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

            if (companionConnectionManager != null)
            {
                if (!companionConnectionManager.IsDiscovering())
                {
                    if (await companionConnectionManager.StartDiscovery() == true)
                    {
                        LogMessage("Start Discovering Devices ...");
                        DiscoverDeviceButton.Content = "\xE894";
                    }
                    else
                        LogMessage("Error while starting Discovering Devices ...");
                }
                else
                {
                    LogMessage("Stop Discovering Devices ...");
                    companionConnectionManager.StopDiscovery();
                    DiscoverDeviceButton.Content = "\xE895";
                }
            }
            UpdateControls();
        }
        /*
        private void AddDevice_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Adding Device ...");
            if (string.Equals(AddDeviceButton.Content,"\xE710"))
            {
                LogMessage("Adding Device ...");
                //AddDeviceButton.Content = "\xE8FB";
                PageStatus = Status.AddingDevice;
            }
            else
            {
                LogMessage("Completing Adding Devices ...");

                string Name = DeviceName.Text;
                string IP = DeviceIPAddress.Text;
                if ((!string.IsNullOrEmpty(Name)) && (MulticastCompanionConnectionManager.IsIPv4Address(IP)))
                {
                    ObservableCollection<Companion.CompanionDevice> deviceList = ViewModelLocator.Settings.DeviceList;
                    if (deviceList != null)
                    {
                        Companion.CompanionDevice d = deviceList.FirstOrDefault(device => string.Equals(device.Name, Name) && string.Equals(device.IPAddress, IP));
                        if (d == null)
                        {
                            LogMessage("Device: " + Name + " IP address: " + IP + " added");
                            deviceList.Add(new Companion.CompanionDevice(Guid.NewGuid().ToString(),false, Name, IP,string.Empty));

                            ViewModelLocator.Settings.DeviceList = deviceList;
                            int index = 0;
                            PageStatus = Status.NoDeviceSelected;
                            foreach (var item in comboDevice.Items)
                            {
                                if (item is Companion.CompanionDevice)
                                {
                                    Companion.CompanionDevice p = item as Companion.CompanionDevice;
                                    if (p != null)
                                    {
                                        if (string.Equals(p.Name, Name) && string.Equals(p.IPAddress, IP))
                                        {
                                            comboDevice.SelectedIndex = index;
                                            PageStatus = Status.DeviceSelected;
                                            AddDeviceButton.Content = "\xE710";
                                            break;
                                        }
                                    }
                                }
                                index++;
                            }
                        }
                    }
                }
            }
            UpdateControls();
        }
        */
        private void RemoveDevice_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Removing Device ...");
            if (comboDevice.SelectedItem is Companion.CompanionDevice)
            {
                ObservableCollection<Companion.CompanionDevice> pll = ViewModelLocator.Settings.DeviceList;
                if ((pll != null) && (comboDevice.SelectedIndex < pll.Count))
                {
                    pll.RemoveAt(comboDevice.SelectedIndex);
                }
                ViewModelLocator.Settings.DeviceList = pll;
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
            bool bNetworkRequired = true;
            return bNetworkRequired;
        }
        private async void NetworkHelper_InternetConnectionChanged(object sender, bool e)
        {
            if (e == true)
            {
                await Shell.Current.DisplayNetworkWarning(false, "");
                LogMessage("Internet Network Connection is on");
            }
            else
            {
                if (IsNetworkRequired())
                {
                    await Shell.Current.DisplayNetworkWarning(true, "The Remote Page requires an Internet connection");
                }
                LogMessage("Internet Network Connection is off");
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




    }


}
