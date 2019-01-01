﻿//*********************************************************
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
using Windows.System.Threading;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AudioVideoPlayer.Pages.DLNA
{


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DLNAPage : Page
    {
        // Collection of smooth streaming urls 
        private ObservableCollection<MediaItem> defaultViewModel = new ObservableCollection<MediaItem>();
        string CurrentContentUrl;
        string CurrentAlbumArtUrl;
        string CurrentTitle;


        /// <summary>
        /// DLNAPage constructor 
        /// </summary>
        public DLNAPage()
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
        async void  UpdateControls(bool bDisable = false)
        {

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                 async () =>
                 {
                        comboPlayList.IsEnabled = true;
                        comboStream.IsEnabled = true;
                        mediaUri.IsEnabled = true;
                        if((comboDevice.Items.Count>0) && (bDisable == false))
                        {
                            comboDevice.IsEnabled = true;
                            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
                            {
                                AudioVideoPlayer.DLNA.DLNADevice dd = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                                if(dd!=null)
                                {
                                    RemoveDeviceButton.IsEnabled = true;
                                    comboDevice.IsEnabled = true;
                                    DeviceName.IsEnabled = true;
                                    DeviceIPAddress.IsEnabled = true;
                                    
                                    displayPlaylistButton.IsEnabled = true;
                                    if(await dd.IsConnected())
                                    {
                                        removePlaylistButton.IsEnabled = true;
                                        insertToPlaylistButton.IsEnabled = true;
                                        addToPlaylistButton.IsEnabled = true;

                                     if (comboDeviceStream.Items.Count>0)
                                        {
                                            comboDeviceStream.IsEnabled = true;
                                            minusButton.IsEnabled = true;
                                            plusButton.IsEnabled = true;
                                            playButton.IsEnabled = true;
                                            if (await dd.IsContentReady())
                                                playPauseButton.IsEnabled = true;
                                            else
                                                playPauseButton.IsEnabled = false;
                                            if (await dd.IsPlaying())
                                            {
                                                 playPauseButton.Content = "\xE769";
                                                 stopButton.IsEnabled = true;
                                            }
                                            else 
                                            {
                                                playPauseButton.Content = "\xE768";
                                                if (await dd.IsStopped())
                                                     stopButton.IsEnabled = false;
                                                else
                                                    stopButton.IsEnabled = true;
                                         }
                                     }
                                     else
                                        {
                                            comboDeviceStream.IsEnabled = false;
                                            minusButton.IsEnabled = false;
                                            plusButton.IsEnabled = false;
                                         playButton.IsEnabled = false;
                                         playPauseButton.IsEnabled = false;
                                         stopButton.IsEnabled = false;
                                        }
                                        if(dd.IsHeosDevice())
                                        {
                                            muteButton.IsEnabled = true;
                                            volumeUpButton.IsEnabled = true;
                                            volumeDownButton.IsEnabled = true;
                                            if(comboDeviceInput.Items.Count>0)
                                            {
                                                comboDeviceInput.IsEnabled = true;
                                                inputButton.IsEnabled = true;
                                            }
                                            else
                                            {
                                                comboDeviceInput.IsEnabled = false;
                                                inputButton.IsEnabled = false;
                                            }
                                        }
                                        else
                                        {
                                            muteButton.IsEnabled = false;
                                            volumeUpButton.IsEnabled = false;
                                            volumeDownButton.IsEnabled = false;
                                            comboDeviceInput.IsEnabled = false;
                                            inputButton.IsEnabled = false;
                                        }
                                    }
                                    else
                                    {
                                        removePlaylistButton.IsEnabled = false;
                                        insertToPlaylistButton.IsEnabled = false;
                                        addToPlaylistButton.IsEnabled = false;

                                        comboDeviceStream.IsEnabled = false;
                                        minusButton.IsEnabled = false;
                                        plusButton.IsEnabled = false;
                                        playButton.IsEnabled = false;
                                     playPauseButton.IsEnabled = false;
                                     stopButton.IsEnabled = false;

                                        muteButton.IsEnabled = false;
                                        volumeUpButton.IsEnabled = false;
                                        volumeDownButton.IsEnabled = false;

                                        comboDeviceInput.IsEnabled = false;
                                        inputButton.IsEnabled = false;

                                    }


                                }

                            }
                        }
                        else
                        {
                            RemoveDeviceButton.IsEnabled = false;
                            comboDevice.IsEnabled = false;
                            DeviceName.IsEnabled = false;
                            DeviceIPAddress.IsEnabled = false;

                            displayPlaylistButton.IsEnabled = false;
                            removePlaylistButton.IsEnabled = false;
                            insertToPlaylistButton.IsEnabled = false;
                            addToPlaylistButton.IsEnabled = false;

                            comboDeviceStream.IsEnabled = false;
                            minusButton.IsEnabled = false;
                            plusButton.IsEnabled = false;
                            playButton.IsEnabled = false;
                         playPauseButton.IsEnabled = false;
                         stopButton.IsEnabled = false;

                            muteButton.IsEnabled = false;
                            volumeUpButton.IsEnabled = false;
                            volumeDownButton.IsEnabled = false;

                            comboDeviceInput.IsEnabled = false;
                            inputButton.IsEnabled = false;
                        }

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
        /// This method is called when the ComboStream selection changes 
        /// </summary>
        private void ComboStream_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboStream.SelectedItem != null)
            {
                MediaItem ms = comboStream.SelectedItem as MediaItem;
                CurrentContentUrl = mediaUri.Text = ms.Content;
                CurrentTitle = ms.Title;
                CurrentAlbumArtUrl = ms.PosterContent;
                UpdateControls();
            }

        }
        // MediaDataGroup used to load the Device playlist
        //MediaDataGroup DefaultDevicePlaylist = null;
        // MediaDataGroup used to load the playlist
        MediaDataGroup DefaultPlaylist = null;
        /// <summary>
        /// Method LoadingData which loads the JSON playlist file
        /// </summary>
        bool IsDataLoaded()
        {
            if ((DefaultPlaylist != null) && (DefaultPlaylist.Items.Count > 0))
                return true;
            return false;
        }
        /// <summary>
        /// Method LoadingData which loads the JSON playlist file
        /// </summary>
        async System.Threading.Tasks.Task<bool> LoadingData(string path)
        {

            string oldPath = MediaDataSource.MediaDataPath;

            try
            {
                Shell.Current.DisplayWaitRing = true;
                UpdateControls(true);

                MediaDataSource.Clear();
                LogMessage(string.IsNullOrEmpty(path) ? "Loading default playlist" : "Loading playlist :" + path);
                DefaultPlaylist = await MediaDataSource.GetGroupAsync(path, "audio_video_picture");
                if ((DefaultPlaylist != null) && (DefaultPlaylist.Items.Count > 0))
                {
                    LogMessage("Loading playlist successful with " + DefaultPlaylist.Items.Count.ToString() + " items");
                    this.defaultViewModel = DefaultPlaylist.Items;
                    comboStream.DataContext = this.defaultViewModel;
                    comboStream.SelectedIndex = 0;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogMessage("Loading playlist failed: " + ex.Message);

            }
            finally
            {
                Shell.Current.DisplayWaitRing = false;
                UpdateControls();

            }
            return false;
        }
        /// <summary>
        /// Method LoadingData which loads the Device JSON playlist file
        /// </summary>
        /*
        async System.Threading.Tasks.Task<bool> LoadingDevicePlaylist(string name, string path)
        {
            try
            {
                Shell.Current.DisplayWaitRing = true;
                UpdateControls(true);

                MediaDataSource.Clear();
                LogMessage(string.IsNullOrEmpty(path) ? "Loading default Device playlist" : "Loading Device playlist :" + path);
                DefaultDevicePlaylist = await MediaDataSource.GetGroupAsync(path, "audio_video_picture");
                if (DefaultDevicePlaylist == null)
                {
                    await Helpers.MediaHelper.CreateEmptyPlaylist(name, path);
                    DefaultDevicePlaylist = await MediaDataSource.GetGroupAsync(path, "audio_video_picture");
                }
                if (DefaultDevicePlaylist != null) 
                {
                    LogMessage("Loading Device playlist successful with " + DefaultDevicePlaylist.Items.Count.ToString() + " items");
                    comboDeviceStream.DataContext = DefaultDevicePlaylist.Items;
                    if(DefaultDevicePlaylist.Items.Count > 0)
                        comboDeviceStream.SelectedIndex = 0;
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogMessage("Loading Device playlist failed: " + ex.Message);

            }
            finally
            {
                Shell.Current.DisplayWaitRing = false;
                UpdateControls();

            }
            return false;
        }
        */
        /*
        ThreadPoolTimer MonitorDeviceTimer;
        bool StartMonitorDeviceTimer()
        {
            TimeSpan period = TimeSpan.FromSeconds(1);
            MonitorDeviceTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        AudioVideoPlayer.DLNA.DLNADevice dd = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                        if (dd != null)
                        {
                            if (await dd.IsConnected())
                            {
                                if(await dd.IsPlaying())
                                {
                                    string url = await dd.GetContentUrl();
                                    if (!string.IsNullOrEmpty(url))
                                    {
                                        if (url.StartsWith("https"))
                                            url = url.Replace("https", "");
                                        else if (url.StartsWith("http"))
                                            url = url.Replace("http", "");
                                        int index = 0;
                                        int j = 0;
                                        foreach (var i in comboDeviceStream.Items)
                                        {
                                            MediaItem m = i as MediaItem;
                                            if (m != null)
                                            {
                                                if (m.Content.IndexOf(url) > 0)
                                                {
                                                    index = j;
                                                    break;
                                                }
                                            }
                                            j++;
                                        }
                                        if (comboDeviceStream.Items.Count > 0)
                                        {
                                            if (comboDeviceStream.SelectedIndex != index)
                                            {
                                                comboDeviceStream.SelectedIndex = index;
                                                int nextIndex = (index + 1 >= comboDeviceStream.Items.Count) ? 0 : index + 1;
                                                MediaItem m = comboDeviceStream.Items[nextIndex] as MediaItem;
                                                if(m!=null)
                                                {
                                                    LogMessage("Add a new URL in the Playlist: " + m.Title);
                                                    await UpdatePlaylist(dd, null, m);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }); ;

            },
            period);
            if (MonitorDeviceTimer != null)
                return true;
            return false;
        }
        bool StopMonitorDeviceTimer()
        {
            if (MonitorDeviceTimer != null)
            {
                MonitorDeviceTimer.Cancel();
                MonitorDeviceTimer = null;
                return true;
            }
            return true;
        }
        */
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

            // Combobox event
            comboStream.SelectionChanged += ComboStream_SelectionChanged;
            // Logs updated
            logs.TextChanged += Logs_TextChanged;

            if (comboDeviceInput.Items.Count > 0)
                comboDeviceInput.SelectedIndex = 0;

            // Select first item in the combo box to select multicast option
            comboDevice.DataContext = ViewModels.StaticSettingsViewModel.DLNADeviceList;
            

            UpdateControls(true);
            UpdateControls();

        }

        /// <summary>
        /// Method OnNavigatedFrom
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {

            // Unregister Network
            UnregisterNetworkHelper();

            // Combobox event
            comboStream.SelectionChanged -= ComboStream_SelectionChanged;
            // Logs event to refresh the TextBox
            logs.TextChanged -= Logs_TextChanged;

            // Stop Monitoring
            foreach (var item in comboDevice.Items)
            {
                AudioVideoPlayer.DLNA.DLNADevice d = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (d != null)
                {
                    bool result = d.StopMonitoringDevice();
                    if (result == true)
                        LogMessage("Stop Monitoring Device: " + d.GetUniqueName());
                    else
                        LogMessage("Failed to stop Monitoring Device: " + d.GetUniqueName());
                }
            }


        }

        private async void comboPlayList_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                    await LoadingData(ViewModelLocator.Settings.CurrentPlayListPath);
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
        AudioVideoPlayer.DLNA.DLNADeviceConnectionManager DLNADeviceConnectionManager;
        enum Status
        {
            NoDeviceSelected = 0,
            DeviceSelected,
            AddingDevice
        }


        private async void comboDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in e.RemovedItems)
            {
                if (item is AudioVideoPlayer.DLNA.DLNADevice)
                {
                    AudioVideoPlayer.DLNA.DLNADevice d = (AudioVideoPlayer.DLNA.DLNADevice)item;
                    if (d != null)
                    {
                        d.DeviceMediaInformationUpdated -= DLNA_DeviceMediaInformationUpdated;
                        d.DeviceMediaPositionUpdated -= DLNA_DeviceMediaPositionUpdated;
                        d.DeviceMediaTransportInformationUpdated -= DLNA_DeviceMediaTransportInformationUpdated;
                        d.DeviceMediaTransportSettingsUpdated -= DLNA_DeviceMediaTransportSettingsUpdated;
                    }
                }
            }
            foreach (var item in e.AddedItems)
            {
                if (item is AudioVideoPlayer.DLNA.DLNADevice)
                {
                    AudioVideoPlayer.DLNA.DLNADevice d = (AudioVideoPlayer.DLNA.DLNADevice)item;
                    if (d != null)
                    {
                        d.DeviceMediaInformationUpdated += DLNA_DeviceMediaInformationUpdated;
                        d.DeviceMediaPositionUpdated += DLNA_DeviceMediaPositionUpdated;
                        d.DeviceMediaTransportInformationUpdated += DLNA_DeviceMediaTransportInformationUpdated;
                        d.DeviceMediaTransportSettingsUpdated += DLNA_DeviceMediaTransportSettingsUpdated;
                        d.RequestRefresh();
                    }
                }
            }
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice p = (AudioVideoPlayer.DLNA.DLNADevice)comboDevice.SelectedItem;
                if (p != null)
                {
                    Shell.Current.DisplayWaitRing = true;
                    LogMessage(string.IsNullOrEmpty(p.FriendlyName) ? "Loading default Device playlist" : "Loading Device playlist for device:" + p.FriendlyName);
                    await p.LoadDevicePlaylist();
                    if (p.ListMediaItem != null)
                    {
                        LogMessage("Loading Device playlist successful with " + p.ListMediaItem.Items.Count.ToString() + " items");
                        comboDeviceStream.DataContext = p.ListMediaItem.Items;
                        if (p.ListMediaItem.Items.Count > 0)
                            comboDeviceStream.SelectedIndex = 0;
                    }
                    Shell.Current.DisplayWaitRing = false;

                    if (await p.IsConnected())
                    {
                        string url = await p.GetContentUrl();
                        if(!string.IsNullOrEmpty(url))
                        {
                            if(url.StartsWith("https"))
                                url = url.Replace("https", "");
                            else if(url.StartsWith("http"))
                                url = url.Replace("http", "");
                            int index = 0;
                            int j = 0;
                            foreach (var i in comboDeviceStream.Items)
                            {
                                MediaItem m = i as MediaItem;
                                if(m!=null)
                                {
                                    if (m.Content.IndexOf(url) > 0)
                                    {
                                        index = j;
                                        break;
                                    }
                                }
                                j++;
                            }
                            if (comboDeviceStream.Items.Count > 0)
                            {
                                comboDeviceStream.SelectedIndex = index;
                            }

                        }
                    }
                    if (comboDeviceInput.Items.Count > 0)
                    {
                        comboDeviceInput.SelectedIndex = 0;
                    }
                }
            }
            UpdateControls();

        }

        private async void DLNA_DeviceMediaTransportInformationUpdated(AudioVideoPlayer.DLNA.DLNADevice sender, AudioVideoPlayer.DLNA.DLNAMediaTransportInformation args)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
            });
        }

        void DisplayPlayModeButtons(string PlayMode)
        {
            if (PlayMode == AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_NORMAL)
            {
                shuffleButton.Content = "\xE8B1";
                repeatButton.Content = "\xE8EE";
            }
            else if (PlayMode == AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_REPEAT_ALL)
            {
                shuffleButton.Content = "\xE8B1";
                repeatButton.Content = "\xE8ED";
            }
            else if (PlayMode == AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_REPEAT_ONE)
            {
                shuffleButton.Content = "\xE8B1";
                repeatButton.Content = "\xEC57";
            }
            else if (PlayMode == AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_SHUFFLE)
            {
                shuffleButton.Content = "\xEC57";
                repeatButton.Content = "\xE8EE";
            }

        }
        private async void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Set Shuffle mode on speaker");
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice d = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (d != null)
                {
                    AudioVideoPlayer.DLNA.DLNAMediaTransportSettings TransportSettings = await d.GetTransportSettings();
                    if (TransportSettings != null)
                    {
                        if(TransportSettings.PlayMode == AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_SHUFFLE)
                            await d.SetPlayMode(AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_NORMAL);
                        else
                            await d.SetPlayMode(AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_SHUFFLE);
                    }
                    TransportSettings = await d.GetTransportSettings();
                    if (TransportSettings != null)
                        DisplayPlayModeButtons(TransportSettings.PlayMode);
                }
            }
        }
        private async void Repeat_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Set Shuffle mode on speaker");
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice d = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (d != null)
                {
                    AudioVideoPlayer.DLNA.DLNAMediaTransportSettings TransportSettings = await d.GetTransportSettings();
                    if (TransportSettings != null)
                    {
                        if ((TransportSettings.PlayMode == AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_SHUFFLE) ||
                            (TransportSettings.PlayMode == AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_NORMAL))
                            await d.SetPlayMode(AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_REPEAT_ALL);
                        else if (TransportSettings.PlayMode == AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_REPEAT_ALL)
                            await d.SetPlayMode(AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_REPEAT_ONE);
                        else 
                            await d.SetPlayMode(AudioVideoPlayer.DLNA.DLNADevice.PLAY_MODE_NORMAL);
                    }
                    TransportSettings = await d.GetTransportSettings();
                    if (TransportSettings != null)
                        DisplayPlayModeButtons(TransportSettings.PlayMode);
                }
            }
        }
        private async void DLNA_DeviceMediaTransportSettingsUpdated(AudioVideoPlayer.DLNA.DLNADevice sender, AudioVideoPlayer.DLNA.DLNAMediaTransportSettings args)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (args != null)
                {
                    DisplayPlayModeButtons(args.PlayMode);
                }
            });
        }

        private void DLNA_DeviceMediaInformationUpdated(AudioVideoPlayer.DLNA.DLNADevice sender, AudioVideoPlayer.DLNA.DLNAMediaInformation args)
        {
            LogMessage("Media Information updated for device: " + sender.GetUniqueName() + " Title: " + AudioVideoPlayer.DLNA.DLNADevice.GetTitleFromMetadataString(args.CurrentUriMetaData) + " AlbumArtUri: " + AudioVideoPlayer.DLNA.DLNADevice.GetAlbumArtUriFromMetadataString(args.CurrentUriMetaData) + " Uri: " + args.CurrentUri);
        }
        private async void DLNA_DeviceMediaPositionUpdated(AudioVideoPlayer.DLNA.DLNADevice sender, AudioVideoPlayer.DLNA.DLNAMediaPosition args)
        {
            //LogMessage("Media Position updated: " + sender.GetUniqueName());
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                TrackDuration.Text = args.TrackDuration.ToString();
                TrackTime.Text = args.RelTime.ToString();
            });

        }
        private void comboDevice_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            RemoveDeviceButton.IsEnabled = false;
            if (comboDevice.Items.Count > 0)
            {
                comboDevice.SelectedIndex = 0;
                RemoveDeviceButton.IsEnabled = true;
            }
            // Start Monitoring
            foreach (var item in comboDevice.Items)
            {
                AudioVideoPlayer.DLNA.DLNADevice d = item as AudioVideoPlayer.DLNA.DLNADevice;
                if (d != null)
                {
                    bool result = d.StartMonitoringDevice();
                    if (result == true)
                        LogMessage("Start Monitoring Device: " + d.GetUniqueName());
                    else
                        LogMessage("Failed to start Monitoring Device: " + d.GetUniqueName());
                }
            }
            UpdateControls();
        }
        private async void DiscoverDevice_Click(object sender, RoutedEventArgs e)
        {
            if ((DLNADeviceConnectionManager == null) || ((DLNADeviceConnectionManager != null) && (!DLNADeviceConnectionManager.IsDiscovering())))
            {
                if(await StartDiscovery() == true)
                {
                    LogMessage("Start Discovering DLNA/UPNP Devices ...");
                    DiscoverDeviceButton.Content = "\xE894";
                }
                else
                    LogMessage("Error while starting Discovering Devices ...");
            }
            else
            {
                LogMessage("Stop Discovering DLNA/UPNP Devices ...");
                StopDiscovery();
                DiscoverDeviceButton.Content = "\xE895";
            }
            UpdateControls();
        }
        private void RemoveDevice_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Removing Device ...");
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                ObservableCollection<AudioVideoPlayer.DLNA.DLNADevice> pll = ViewModelLocator.Settings.DLNADeviceList;
                if ((pll != null) && (comboDevice.SelectedIndex < pll.Count))
                {
                    pll.RemoveAt(comboDevice.SelectedIndex);
                }
                ViewModelLocator.Settings.DLNADeviceList = pll;
                if (comboDevice.Items.Count > 0)
                {
                    comboDevice.SelectedIndex = 0;
                }

            }

            UpdateControls();
        }
    /// <summary>
    /// This method checks if the url is a music url 
    /// </summary>
    private bool IsAudio(string url)
    {
        bool result = false;
        if (!string.IsNullOrEmpty(url))
        {
            if ((url.ToLower().EndsWith(".mp3")) ||
                (url.ToLower().EndsWith(".wma")) ||
                (url.ToLower().EndsWith(".aac")) ||
                (url.ToLower().EndsWith(".m4a")) ||
                (url.ToLower().EndsWith(".flac")))
            {
                result = true;
            }
        }
        return result;
    }
    /// <summary>
    /// This method checks if the url is a music url 
    /// </summary>
    private string GetCodec(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
                if (url.ToLower().EndsWith(".mp3"))
                    return "mp3";
                if (url.ToLower().EndsWith(".mp4"))
                    return "mp4";
                if (url.ToLower().EndsWith(".aac"))
                    return "mp4";
                if (url.ToLower().EndsWith(".m4a"))
                    return "mp4";
                if (url.ToLower().EndsWith(".flac"))
                    return "flac";
        }
        return "mp4";
    }
        private async void AddToPlaylistAndPlay_Click(object sender, RoutedEventArgs e)
        {

            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice hs = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if(hs!=null)
                {
                    MediaItem item = comboStream.SelectedItem as AudioVideoPlayer.DataModel.MediaItem;
                    if (item != null)
                    {
                        hs.ListMediaItem.Items.Add(item);
                        comboDeviceStream.DataContext = hs.ListMediaItem.Items;
                        if(hs.ListMediaItem.Items.Count>0)
                            comboDeviceStream.SelectedIndex = 0;
                        string name = hs.FriendlyName.Replace(' ', '_') + "_" + hs.Ip;
                        string path = await Helpers.MediaHelper.GetPlaylistPath(name);
                        await hs.SaveDevicePlaylist();
                    }
                    LogMessage("Play url " + mediaUri.Text + " on Device: " + hs.FriendlyName);
                    string Codec = GetCodec(mediaUri.Text);
                    string CurrentUrl = mediaUri.Text;
                    if (hs.IsSamsungDevice())
                    {
                        CurrentUrl = CurrentUrl.Replace("https://", "http://");
                        CurrentAlbumArtUrl = CurrentAlbumArtUrl.Replace("https://", "http://");
                    }
                    bool result = await hs.PlayUrl(IsAudio(CurrentUrl), CurrentUrl, CurrentAlbumArtUrl, CurrentTitle,Codec);
                    if(result == true)
                    {
                        LogMessage("Play url " + mediaUri.Text + " on Device: " + hs.FriendlyName + " successful");
                    }
                    else
                    {
                        LogMessage("Play url " + mediaUri.Text + " on Device: " + hs.FriendlyName + " error");
                    }
                }
            }

            UpdateControls();
        }
        private async void InsertToPlaylist_Click(object sender, RoutedEventArgs e)
        {

            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice hs = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (hs != null)
                {
                    MediaItem item = comboStream.SelectedItem as AudioVideoPlayer.DataModel.MediaItem;
                    if (item != null)
                    {
                        int index = comboDeviceStream.SelectedIndex;
                        if (index < 0)
                            index = 0;

                        hs.ListMediaItem.Items.Insert(index,item);
                        comboDeviceStream.DataContext = hs.ListMediaItem.Items;
                        if (hs.ListMediaItem.Items.Count > 0)
                        {
                            if (hs.ListMediaItem.Items.Count > index)
                                comboDeviceStream.SelectedIndex = index;
                            else
                                comboDeviceStream.SelectedIndex = 0;
                        }
                        await hs.SaveDevicePlaylist();
                    }
                }
            }
            UpdateControls();
        }
        private async void RemoveFromPlaylist_Click(object sender, RoutedEventArgs e)
        {

            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice hs = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (hs != null)
                {
                    MediaItem item = comboDeviceStream.SelectedItem as AudioVideoPlayer.DataModel.MediaItem;
                    if (item != null)
                    {
                        int index = comboDeviceStream.SelectedIndex;
                        if (index < 0)
                            index = 0;

                        hs.ListMediaItem.Items.Remove(item);
                        comboDeviceStream.DataContext = hs.ListMediaItem.Items;
                        if (hs.ListMediaItem.Items.Count > 0)
                        {
                            if (hs.ListMediaItem.Items.Count > index)
                                comboDeviceStream.SelectedIndex = index;
                            else
                            {
                                while (--index >= 0)
                                {
                                    if (hs.ListMediaItem.Items.Count > index)
                                    {
                                        comboDeviceStream.SelectedIndex = index;
                                        break;
                                    }
                                }
                            }
                        }
                        await hs.SaveDevicePlaylist();
                    }
                }
            }
            UpdateControls();
        }
        private AudioVideoPlayer.DLNA.DLNADevice GetCurrentSpeaker()
        {
            AudioVideoPlayer.DLNA.DLNADevice hs = null;
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                hs = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
            }
            return hs;
        }
        private async void AddToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice hs = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (hs != null)
                {
                    MediaItem item = comboStream.SelectedItem as AudioVideoPlayer.DataModel.MediaItem;
                    if (item != null)
                    {
                        int index = comboDeviceStream.SelectedIndex;
                        if (index < 0)
                            index = 0;
                        hs.ListMediaItem.Items.Add(item);
                        comboDeviceStream.DataContext = hs.ListMediaItem.Items;
                        if (hs.ListMediaItem.Items.Count > 0)
                        {
                            if (hs.ListMediaItem.Items.Count > index)
                                comboDeviceStream.SelectedIndex = index;
                            else
                                comboDeviceStream.SelectedIndex = 0;
                        }

                        await hs.SaveDevicePlaylist();
                    }
                }
            }
            /*
            LogMessage("Play url " + mediaUri.Text + " on Device: " + hs.FriendlyName);
                    string Codec = GetCodec(mediaUri.Text);
                    
                    string CurrentUrl = mediaUri.Text;
                    if (hs.IsSamsungDevice())
                    {
                        CurrentUrl = CurrentUrl.Replace("https://", "http://");
                        CurrentAlbumArtUrl = CurrentAlbumArtUrl.Replace("https://", "http://");
                    }
                    bool result = await hs.AddUrl(IsAudio(CurrentUrl), CurrentUrl, CurrentAlbumArtUrl, CurrentTitle, Codec);
                    if (result == true)
                    {
                        LogMessage("Play url " + mediaUri.Text + " on Device: " + hs.FriendlyName + " successful");
                    }
                    else
                    {
                        LogMessage("Play url " + mediaUri.Text + " on Device: " + hs.FriendlyName + " error");
                    }
                }
            }
            */
            UpdateControls();
        }
        async System.Threading.Tasks.Task<bool> UpdatePlaylist(AudioVideoPlayer.DLNA.DLNADevice dd, MediaItem  item1, MediaItem item2)
        {
            bool result = false;
            if (item1 != null)
            {
                string ContentUrl = item1.Content;
                string AlbumUrl = item1.PosterContent;
                string Title = item1.Title;
                string Codec = GetCodec(ContentUrl);
                if (dd.IsSamsungDevice())
                {
                    ContentUrl = ContentUrl.Replace("https://", "http://");
                    AlbumUrl = AlbumUrl.Replace("https://", "http://");
                }
                result = await dd.PlayUrl(IsAudio(ContentUrl), ContentUrl, AlbumUrl, Title, Codec);
            }
            if (item2 != null)
            {
                string ContentUrl = item2.Content;
                string AlbumUrl = item2.PosterContent;
                string Title = item2.Title;
                string Codec = GetCodec(ContentUrl);
                if (dd.IsSamsungDevice())
                {
                    ContentUrl = ContentUrl.Replace("https://", "http://");
                    AlbumUrl = AlbumUrl.Replace("https://", "http://");
                }
                result = await dd.AddNextUrl(IsAudio(ContentUrl), ContentUrl, AlbumUrl, Title, Codec);

            }
            return result;
        }
        private async void Play_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Play current content audio on device");
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice dd = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (dd != null)
                {
                    MediaItem item1 = comboDeviceStream.SelectedItem as AudioVideoPlayer.DataModel.MediaItem;
                    if (item1 != null)
                    {
                        int index = comboDeviceStream.SelectedIndex;
                        if (index < 0)
                            index = 0;                     
                        if (++index >= comboDeviceStream.Items.Count)
                            index = 0;
                        MediaItem item2 = comboDeviceStream.Items[index] as AudioVideoPlayer.DataModel.MediaItem;
                        if (item2!=null)
                        {
                            await UpdatePlaylist(dd, item1, item2);
                        }
                    }
                }
            }

            /*
            AudioVideoPlayer.DLNA.DLNADevice hs = GetCurrentSpeaker();
            if (hs != null)
            {
                LogMessage("Play player on Device: " + hs.FriendlyName);
                bool result = await hs.PlayerSetState("play");
                if (result == true)
                {
                    LogMessage("Play player on Device: " + hs.FriendlyName + " successful");
                }
                else
                {
                    LogMessage("Play player on Device: " + hs.FriendlyName + " error");
                }
            }
            */
            UpdateControls();
        }
        private async void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice dd = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (dd != null)
                {
                    if (await dd.IsPlaying())
                        await dd.Pause();
                    else
                        dd.Play().Wait(450);
                    UpdateControls();
                }
            }
            
        }
        private async void Pause_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Pause audio on speaker");
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice hs = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (hs != null)
                {
                    LogMessage("Pause on Device: " + hs.FriendlyName);
                    bool result = await hs.Pause();
                    if (result == true)
                    {
                        LogMessage("Pause on Device: " + hs.FriendlyName + " successful");
                    }
                    else
                    {
                        LogMessage("Pause on Device: " + hs.FriendlyName + " error");
                    }
                }
            }
            /*
            AudioVideoPlayer.DLNA.DLNADevice hs = GetCurrentSpeaker();
            if (hs != null)
            {
                LogMessage("Pause player on Device: " + hs.FriendlyName);
                bool result = await hs.PlayerSetState("pause");
                if (result == true)
                {
                    LogMessage("Pause player on Device: " + hs.FriendlyName + " successful");
                }
                else
                {
                    LogMessage("Play player on Device: " + hs.FriendlyName + " error");
                }
            }
            */
            UpdateControls();
        }
        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Stop audio on speaker" );
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice hs = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (hs != null)
                {
                    LogMessage("Stop on Device: " + hs.FriendlyName);
                    bool result = await hs.Stop();
                    if (result == true)
                        LogMessage("Stop on Device: " + hs.FriendlyName + " successful");
                    else
                        LogMessage("Stop on Device: " + hs.FriendlyName + " error");
                    await System.Threading.Tasks.Task.Delay(300);
                    UpdateControls();
                }
            }
        }
        private  async void Minus_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Step to previous content in the playlist on speaker");
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice dd = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (dd != null)
                {
                    if ((comboDeviceStream.SelectedIndex-1)>=0)
                        comboDeviceStream.SelectedIndex--;
                    else
                        comboDeviceStream.SelectedIndex = comboDeviceStream.Items.Count-1;
                    MediaItem item1 = comboDeviceStream.SelectedItem as AudioVideoPlayer.DataModel.MediaItem;
                    if (item1 != null)
                    {
                        int index = comboDeviceStream.SelectedIndex;
                        if (index < 0)
                            index = 0;
                        if (++index >= comboDeviceStream.Items.Count)
                            index = 0;
                        MediaItem item2 = comboDeviceStream.Items[index] as AudioVideoPlayer.DataModel.MediaItem;
                        if (item2 != null)
                        {
                            await UpdatePlaylist(dd, item1, item2);
                        }
                    }
                }
            }

            /*
            AudioVideoPlayer.DLNA.DLNADevice hs = GetCurrentSpeaker();
            if (hs != null)
            {
                LogMessage("Step to previous content in the playlist on Device: " + hs.FriendlyName);
                bool result = await hs.PlayerPrevious();
                if (result == true)
                {
                    LogMessage("Step to previous content in the playlist on Device: " + hs.FriendlyName + " successful");
                }
                else
                {
                    LogMessage("Step to previous content in the playlist on Device: " + hs.FriendlyName + " error");
                }
            }
            */
            UpdateControls();
        }
        private  async void Plus_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Step to next content in the playlist on speaker");
            if (comboDevice.SelectedItem is AudioVideoPlayer.DLNA.DLNADevice)
            {
                AudioVideoPlayer.DLNA.DLNADevice dd = comboDevice.SelectedItem as AudioVideoPlayer.DLNA.DLNADevice;
                if (dd != null)
                {
                    if (comboDeviceStream.SelectedIndex < (comboDeviceStream.Items.Count - 1))
                        comboDeviceStream.SelectedIndex++;
                    else
                        comboDeviceStream.SelectedIndex = 0;

                    MediaItem item1 = comboDeviceStream.SelectedItem as AudioVideoPlayer.DataModel.MediaItem;
                    if (item1 != null)
                    {
                        int index = comboDeviceStream.SelectedIndex;
                        if (index < 0)
                            index = 0;
                        if (++index >= comboDeviceStream.Items.Count)
                            index = 0;
                        MediaItem item2 = comboDeviceStream.Items[index] as AudioVideoPlayer.DataModel.MediaItem;
                        if (item2 != null)
                        {
                            await UpdatePlaylist(dd, item1, item2);
                        }
                    }
                }
            }

            /*
            AudioVideoPlayer.DLNA.DLNADevice hs = GetCurrentSpeaker();
            if (hs != null)
            {
                LogMessage("Step to next content in the playlist on Device: " + hs.FriendlyName);
                bool result = await hs.PlayerNext();
                if (result == true)
                {
                    LogMessage("Step to next content in the playlist on Device: " + hs.FriendlyName + " successful");
                }
                else
                {
                    LogMessage("Step to next content in the playlist on Device: " + hs.FriendlyName + " error");
                }
            }
            */
            UpdateControls();
        }
        private async void Input_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Select audio input");
            AudioVideoPlayer.DLNA.DLNADevice hs = GetCurrentSpeaker();
            if (hs != null)
            {
                Models.DeviceInput di = comboDeviceInput.SelectedItem as Models.DeviceInput;
                if(di!=null)
                {
                    LogMessage("Select audio input " + di.Name + " on Device: " + hs.FriendlyName);
                    bool result = await hs.PlayInput(di.Name);
                    if (result==true)
                        LogMessage("Select audio input " + di.Name + " on Device: " + hs.FriendlyName + " successful" );
                    else
                        LogMessage("Select audio input " + di.Name + " on Device: " + hs.FriendlyName + " error");
                }
            }
            UpdateControls();
        }
        private async void Mute_Click(object sender, RoutedEventArgs e)
        {
//            LogMessage("Mute audio on speaker");
            AudioVideoPlayer.DLNA.DLNADevice hs = GetCurrentSpeaker();
            if (hs != null)
            {
                bool on = await hs.GetPlayerMute();
                if(!on)
                LogMessage("Mute player on Device: " + hs.FriendlyName);
                else
                LogMessage("Unmute player on Device: " + hs.FriendlyName);
                bool result = await hs.PlayerSetMute(!on);
                if (result == true)
                {
                    int level = await hs.GetPlayerVolume();
                    LogMessage("Mute/Unmute player on Device: " + hs.FriendlyName + " level: " + level.ToString() + " successful");
                }
                else
                {
                    LogMessage("Mute/Unmute player on Device: " + hs.FriendlyName + " error");
                }
            }
            UpdateControls();
        }
        private async void VolumeUp_Click(object sender, RoutedEventArgs e)
        {
  //          LogMessage("Volume Up on speaker");
            AudioVideoPlayer.DLNA.DLNADevice hs = GetCurrentSpeaker();
            if (hs != null)
            {
 //               LogMessage("Volume Up on Device: " + hs.FriendlyName);
                bool result = await hs.PlayerVolumeUp();
                if (result == true)
                {
                    int level = await hs.GetPlayerVolume();
                    LogMessage("Volume Up on Device: " + hs.FriendlyName + " level: " + level.ToString() +  " successful");
                }
                else
                {
                    LogMessage("Volume Up on Device: " + hs.FriendlyName + " error");
                }
            }
            UpdateControls();
        }
        private async void VolumeDown_Click(object sender, RoutedEventArgs e)
        {
            //LogMessage("Volume Down on speaker");
            AudioVideoPlayer.DLNA.DLNADevice hs = GetCurrentSpeaker();
            if (hs != null)
            {
                //LogMessage("Volume Down on Device: " + hs.FriendlyName);
                bool result = await hs.PlayerVolumeDown();
                if (result == true)
                {
                    int level = await hs.GetPlayerVolume();
                    LogMessage("Volume Down on Device: " + hs.FriendlyName + " level: " + level.ToString() + " successful");
                }
                else
                {
                    LogMessage("Volume Down on Device: " + hs.FriendlyName + " error");
                }
            }
            UpdateControls();
        }
        private async void DisplayPlaylist_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Display information about the current speaker");
            AudioVideoPlayer.DLNA.DLNADevice hs = GetCurrentSpeaker();
            if (hs != null)
            {

                AudioVideoPlayer.DLNA.DLNAMediaInformation MediaInfo = await hs.GetMediaInformation();
                if (MediaInfo != null)
                {
                    LogMessage("GetMediaInfo for Device: " + hs.FriendlyName + " successful \r\n  NumberTrack: " + MediaInfo.NrTrack.ToString() + "\r\n  Duration: " + MediaInfo.MediaDuration.ToString()
                        + "\r\n  CurrentUri: " + MediaInfo.CurrentUri.ToString()
                        + "\r\n  NextUri: " + MediaInfo.NextUri.ToString()
                        +"\r\n  CurrentUriMetaData: " + MediaInfo.CurrentUriMetaData.ToString()
                        + "\r\n  NextUriMetaData: " + MediaInfo.NextUriMetaData.ToString());
                }
                else
                {
                    LogMessage("GetMediaInfo for Device: " + hs.FriendlyName + " error");
                }
                AudioVideoPlayer.DLNA.DLNAMediaPosition PositionInfo = await hs.GetMediaPosition();
                if (PositionInfo != null)
                {
                    LogMessage("GetPositionInfo for Device: " + hs.FriendlyName + " successful"
                        + "\r\n  Track: " + PositionInfo.Track.ToString()
                        + "\r\n  Duration: " + PositionInfo.TrackDuration.ToString()
                        + "\r\n  Uri: " + PositionInfo.TrackUri.ToString()
                        + "\r\n  RelTime: " + PositionInfo.RelTime.ToString()
                        + "\r\n  AbsTime: " + PositionInfo.AbsTime.ToString()

                        );
                }
                else
                {
                    LogMessage("GetPosition for Device: " + hs.FriendlyName + " error");
                }
                AudioVideoPlayer.DLNA.DLNAMediaTransportInformation TransportInfo = await hs.GetTransportInformation();
                if (TransportInfo != null)
                {
                    LogMessage("GetTransportInfo for Device: " + hs.FriendlyName + " successful"
                        + "\r\n  State: " + TransportInfo.CurrentTransportState.ToString()
                        + "\r\n  Status: " + TransportInfo.CurrentTransportStatus.ToString()
                        + "\r\n  Speed: " + TransportInfo.CurrentSpeed.ToString()
                        );

                }
                else
                {
                    LogMessage("GetTransportInfo for Device: " + hs.FriendlyName + " error");
                }
                AudioVideoPlayer.DLNA.DLNAMediaTransportSettings TransportSettings  = await hs.GetTransportSettings();
                if (TransportSettings != null)
                {
                    LogMessage("GetTransportSettings for Device: " + hs.FriendlyName + " successful \r\n  PlayMode: " + TransportSettings.PlayMode);
                }
                else
                {
                    LogMessage("GetTransportSettings for Device: " + hs.FriendlyName + " error");
                }
                LogMessage("Get Volume Level on Device: " + hs.FriendlyName);
                int level = await hs.GetPlayerVolume();
                if (level >=0)
                {
                    LogMessage("Get Volume Level on Device: " + hs.FriendlyName + " value: " + level.ToString());
                }
                else
                {
                    LogMessage("Get Volume Level on Device: " + hs.FriendlyName + " error");
                }
                LogMessage("Get Mute on Device: " + hs.FriendlyName);
                bool bmute = await hs.GetPlayerMute();
                if (bmute == true)
                {
                    LogMessage("Get Mute on Device: " + hs.FriendlyName + " mute: on" );
                }
                else
                {
                    LogMessage("Get Mute on Device: " + hs.FriendlyName + " mute: off");
                }

                LogMessage("Get Player State on Device: " + hs.FriendlyName);
                string state = await hs.GetPlayerState();
                if (!string.IsNullOrEmpty(state))
                {
                    LogMessage("Get Player State on Device: " + hs.FriendlyName + " state: " + state);
                }
                else
                {
                    LogMessage("Get Player State on Device: " + hs.FriendlyName + " error");
                }

                LogMessage("Get Player Queue Count on Device: " + hs.FriendlyName);
                int count = await hs.GetPlayerQueue();
                if (count >= 0)
                {
                    LogMessage("Get Player Queue Count on Device: " + hs.FriendlyName + " count: " + count.ToString());
                }
                else
                {
                    LogMessage("Get Player Queue Count on Device: " + hs.FriendlyName + " error");
                }


            }
            UpdateControls();
        }
        private async void ClearPlaylist_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Clear Playlist on speaker");
            AudioVideoPlayer.DLNA.DLNADevice hs = GetCurrentSpeaker();
            if (hs != null)
            {
                LogMessage("Clear Queue on Device: " + hs.FriendlyName);
                bool result = await hs.ClearPlayerQueue();
                if (result == true)
                {
                    LogMessage("Clear Queue on Device: " + hs.FriendlyName + " successful");
                }
                else
                {
                    LogMessage("Clear Queue on Device: " + hs.FriendlyName + " error");
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
                if (DLNADeviceConnectionManager == null)
                {
                    DLNADeviceConnectionManager = new AudioVideoPlayer.DLNA.DLNADeviceConnectionManager();
                    if (DLNADeviceConnectionManager != null)
                    {
                        DLNADeviceConnectionManager.Initialize();
                    }
                }
                if (DLNADeviceConnectionManager != null)
                {
                    if (await DLNADeviceConnectionManager.StartDiscovery() == true)
                    {
                        DLNADeviceConnectionManager.DLNADeviceAdded += DLNADeviceConnectionManager_DLNADeviceAdded;
                        DLNADeviceConnectionManager.DLNADeviceUpdated += DLNADeviceConnectionManager_DLNADeviceUpdated;
                        result = true;
                    }
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.Write("Exception while starting DLNA discovery: " + ex.Message) ;
            }
            return result;
        }

        private void DLNADeviceConnectionManager_DLNADeviceUpdated(AudioVideoPlayer.DLNA.DLNADeviceConnectionManager sender, AudioVideoPlayer.DLNA.DLNADevice args)
        {
           // LogMessage("Updated Device: " + args.FriendlyName + " IP: " + args.Ip);
        }
        bool IsSpeakerRegistered(string Id)
        {
            bool result = false;
            foreach (var d in ViewModels.StaticSettingsViewModel.DLNADeviceList)
            {
                if (d.Id == Id)
                    return true;
            }
            return result;
        }
        void UpdateSelection(string Id)
        {
            int index = 0;
            foreach (var item in comboDevice.Items)
            {
                if (item is AudioVideoPlayer.DLNA.DLNADevice)
                {
                    AudioVideoPlayer.DLNA.DLNADevice p = item as AudioVideoPlayer.DLNA.DLNADevice;
                    if (p != null)
                    {
                        if (string.Equals(p.Id, Id))
                        {
                            comboDevice.SelectedIndex = index;
                            return;
                        }
                    }
                }
                index++;
            }
            if (comboDevice.Items.Count > 0)
            {
                comboDevice.SelectedIndex = 0;
            }
            return;
        }
        private async void DLNADeviceConnectionManager_DLNADeviceAdded(AudioVideoPlayer.DLNA.DLNADeviceConnectionManager sender, AudioVideoPlayer.DLNA.DLNADevice args)
        {
            if(args.IsHeosDevice())
                LogMessage("Added DLNA/HEOS Device: " + args.FriendlyName + " IP: " + args.Ip);
            else
                LogMessage("Added DLNA Device: " + args.FriendlyName + " IP: " + args.Ip);


            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ObservableCollection<AudioVideoPlayer.DLNA.DLNADevice> speakerList = ViewModelLocator.Settings.DLNADeviceList;
                if (speakerList != null)
                {
                    AudioVideoPlayer.DLNA.DLNADevice d = speakerList.FirstOrDefault(device => string.Equals(device.Id, args.Id));
                    if (d == null)
                    {
                        LogMessage("Device: " + args.Id + " IP address: " + args.Ip + " added");
                        speakerList.Add(new AudioVideoPlayer.DLNA.DLNADevice(args.Id, args.Location, args.Version, args.IpCache, args.Server, args.St, args.Usn, args.Ip, args.FriendlyName, args.Manufacturer, args.ModelName, args.ModelNumber));
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

                    ViewModelLocator.Settings.DLNADeviceList = speakerList;
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
                if (DLNADeviceConnectionManager != null)
                {
                    DLNADeviceConnectionManager.DLNADeviceAdded -= DLNADeviceConnectionManager_DLNADeviceAdded;
                    DLNADeviceConnectionManager.DLNADeviceUpdated -= DLNADeviceConnectionManager_DLNADeviceUpdated;
                    DLNADeviceConnectionManager.StopDiscovery();
                    DLNADeviceConnectionManager.Uninitialize();
                    DLNADeviceConnectionManager = null;
                    result = true;
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.Write("Exception while stopping DLNA discovery: " + ex.Message) ;
            }
            return result;
        }
        #endregion
    }


}
