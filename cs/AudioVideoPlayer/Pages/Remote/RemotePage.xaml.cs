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
using Companion;
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

        /// <summary>
        /// PlaylistPage constructor 
        /// </summary>
        public RemotePage()
        {
            this.InitializeComponent();


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

            // Logs event to refresh the TextBox
            logs.TextChanged += Logs_TextChanged;
            //
            LogMessage("RemotePage OnNavigatedTo");
            // Register Companion
            RegisterCompanion();
            // Initialize the Companion mode (Remote or Player)
            InitializeCompanionMode();

            // Update Controls
            UpdateControls();
        }
        /// <summary>
        /// UpdateControls Method which update the controls on the page  
        /// </summary>
        async void UpdateControls(bool bDisable = false)
        {

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                 () =>
                 {
                    playButton.IsEnabled = true;
                    playPauseButton.IsEnabled = true;
                    pausePlayButton.IsEnabled = true;
                    stopButton.IsEnabled = true;

                    minusButton.IsEnabled = true;
                    plusButton.IsEnabled = true;

                    muteButton.IsEnabled = true;
                    volumeDownButton.IsEnabled = true;
                    volumeUpButton.IsEnabled = true;

                    fullscreenButton.IsEnabled = true;
                    fullwindowButton.IsEnabled = true;

                    comboDevice.IsEnabled = true;
                    contentUri.IsEnabled = true;
                    playlistUri.IsEnabled = true;

                    playPlaylistButton.IsEnabled = true;
                    playContentButton.IsEnabled = true;
                 });
        }

        /// <summary>
        /// Full screen method 
        /// </summary>
        private void Fullscreen_Click(object sender, RoutedEventArgs e)
        {
                Fullscreen_remote_Click(sender, e);
        }
        /// <summary>
        /// Full window method 
        /// </summary>
        private void Fullwindow_Click(object sender, RoutedEventArgs e)
        {
                Fullwindow_remote_Click(sender, e);
        }
        /// <summary>
        /// Play method which plays the video with the MediaElement from position 0
        /// </summary>
        private void Play_Click(object sender, RoutedEventArgs e)
        {
                Play_remote_Click(sender, e);
        }
        /// <summary>
        /// Stop method which stops the video currently played by the MediaElement
        /// </summary>
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
                Stop_remote_Click(sender, e);
        }
        /// <summary>
        /// Play method which plays the video currently paused by the MediaElement
        /// </summary>
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
                Playpause_remote_Click(sender, e);
        }
        /// <summary>
        /// Pause method which pauses the video currently played by the MediaElement
        /// </summary>
        private void PausePlay_Click(object sender, RoutedEventArgs e)
        {
                Pause_remote_Click(sender, e);
        }


        /// <summary>
        /// Playlist method which loads another JSON playlist for the application 
        /// </summary>
        private async void Playlist_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.FileTypeFilter.Add(".json");
            filePicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            filePicker.SettingsIdentifier = "PlaylistPicker";
            filePicker.CommitButtonText = "Open JSON Playlist File to Process";

            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);
            }
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
        private CompanionClient companion;

        private void RegisterCompanion()
        {
            companion = new CompanionClient();
            companion.MessageReceived += Companion_MessageReceived;

        }
        private void UnregisterCompanion()
        {
            if (companion != null)
                companion.MessageReceived -= Companion_MessageReceived;
        }
        private void Companion_MessageReceived(CompanionClient sender, string Command, Dictionary<string, string> Parameters)
        {
        }
        private async void InitializeCompanionMode()
        {
                // Show FullWindow on phone
                if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    // Show fullWindow button
                    fullwindowButton.Visibility = Visibility.Visible;
                }

                companion.StopRecv();
                bool result = await companion.InitializeSend();
                if (result == true)
                    LogMessage("Companion Initialize Send ok");
                else
                    LogMessage("Error Companion Initialize Send");

        }

        //private async void open_remote_Click(object sender, RoutedEventArgs e)
        //{
        //    LogMessage("Open Media event, parameter: " + parameter.Text);
        //    Dictionary<string, string> p = companion.GetParametersFromString(parameter.Text);
        //    bool bResult = await companion.SendCommand(CompanionClient.commandOpen, p);
        //    UpdateControls();
        //}
        //private async void openPlaylist_remote_Click(object sender, RoutedEventArgs e)
        //{
        //    LogMessage("Open Playlist event, parameter: " + parameter.Text);
        //    Dictionary<string, string> p = companion.GetParametersFromString(parameter.Text);
        //    bool bResult = await companion.SendCommand(CompanionClient.commandOpenPlaylist, p);
        //    UpdateControls();
        //}
        //private async void select_remote_Click(object sender, RoutedEventArgs e)
        //{

        //    LogMessage("Select event, parameter: " + parameter.Text);
        //    Dictionary<string, string> p = companion.GetParametersFromString(parameter.Text);
        //    bool bResult = await companion.SendCommand(CompanionClient.commandSelect, p);
        //    UpdateControls();

        //}
        private async void Stop_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Stop event");
            bool bResult = await companion.SendCommand(CompanionClient.commandStop, null);
            UpdateControls();

        }
        private async void Play_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Play event");
            bool bResult = await companion.SendCommand(CompanionClient.commandPlay, null);
            UpdateControls();

        }
        private async void Playpause_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Playpause event");
            bool bResult = await companion.SendCommand(CompanionClient.commandPlayPause, null);
            UpdateControls();

        }
        private async void Pause_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Pause event");
            bool bResult = await companion.SendCommand(CompanionClient.commandPause, null);
            UpdateControls();


        }
        private async void Plus_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Plus event");
            bool bResult = await companion.SendCommand(CompanionClient.commandPlus, null);
            UpdateControls();

        }
        private async void Minus_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Minus event");
            bool bResult = await companion.SendCommand(CompanionClient.commandMinus, null);
            UpdateControls();
        }
        private async void Fullscreen_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Fullscreen event");
            bool bResult = await companion.SendCommand(CompanionClient.commandFullScreen, null);
            UpdateControls();

        }
        private async void Fullwindow_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Fullwindow event");
            bool bResult = await companion.SendCommand(CompanionClient.commandFullWindow, null);
            UpdateControls();
        }
        private async void Window_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending window event");
            bool bResult = await companion.SendCommand(CompanionClient.commandWindow, null);
            UpdateControls();
        }
        private async void Mute_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Mute event");
            bool bResult = await companion.SendCommand(CompanionClient.commandMute, null);
            UpdateControls();
        }
        private async void VolumeUp_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Volume Up event ");
            bool bResult = await companion.SendCommand(CompanionClient.commandVolumeUp, null);
            UpdateControls();

        }
        private async void VolumeDown_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Volume Down event ");
            bool bResult = await companion.SendCommand(CompanionClient.commandVolumeDown, null);
            UpdateControls();
        }
        private async void Down_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Down event ");
            bool bResult = await companion.SendCommand(CompanionClient.commandDown, null);
            UpdateControls();
        }
        private async void Up_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Up event ");
            bool bResult = await companion.SendCommand(CompanionClient.commandUp, null);
            UpdateControls();
        }
        private async void Left_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Left event ");
            bool bResult = await companion.SendCommand(CompanionClient.commandLeft, null);
            UpdateControls();
        }
        private async void Right_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Right event ");
            bool bResult = await companion.SendCommand(CompanionClient.commandRight, null);
            UpdateControls();
        }
        private async void Enter_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Enter event ");
            bool bResult = await companion.SendCommand(CompanionClient.commandEnter, null);
            UpdateControls();
        }

        #endregion

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


    }


}
