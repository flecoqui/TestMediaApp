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

            // Select first item in the combo box to select multicast option
            comboDevice.DataContext = ViewModels.StaticSettingsViewModel.DeviceList;
            if(comboDevice.Items.Count>0)
                comboDevice.SelectedIndex = 0;
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
                    selectContentButton.IsEnabled = true;

                     UpRemoteButton.IsEnabled = true;
                     DownRemoteButton.IsEnabled = true;
                     LeftRemoteButton.IsEnabled = true;
                     RightRemoteButton.IsEnabled = true;
                     EnterRemoteButton.IsEnabled = true;



                 });
        }
        string GetIPAddress()
        {
            Models.Device d = comboDevice.SelectedItem as Models.Device;
            if (d != null)
            {
                if (!string.IsNullOrEmpty(d.IpAddress))
                    return d.IpAddress;
            }
            return Companion.CompanionClient.cMulticastAddress;
        }

        private async void down_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Down event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(),CompanionClient.commandDown, null);
            UpdateControls();
        }
        private async void up_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Up event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(),CompanionClient.commandUp, null);
            UpdateControls();
        }
        private async void left_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Left event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandLeft, null);
            UpdateControls();
        }
        private async void right_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Right event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandRight, null);
            UpdateControls();
        }
        private async void enter_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Enter event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandEnter, null);
            UpdateControls();
        }
        private async void open_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Open Media event to " + GetIPAddress() + " parameter: " + contentUri.Text);
            string commandString = CompanionClient.parameterContent + CompanionClient.cEQUAL + playlistUri.Text;
            Dictionary<string, string> p = companion.GetParametersFromString(commandString);
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandOpen, p);
            UpdateControls();
        }
        private async void openPlaylist_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Open Playlist event to " + GetIPAddress() + " parameter: " + playlistUri.Text);
            string commandString = CompanionClient.parameterContent + CompanionClient.cEQUAL + playlistUri.Text;
            Dictionary<string, string> p = companion.GetParametersFromString(commandString);
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandOpenPlaylist, p);
            UpdateControls();
        }
        private async void select_remote_Click(object sender, RoutedEventArgs e)
        {

            LogMessage("Select event to " + GetIPAddress() + " parameter: " + contentNumber.Text);
            string commandString = CompanionClient.parameterIndex + CompanionClient.cEQUAL + contentNumber.Text;
            Dictionary<string, string> p = companion.GetParametersFromString(contentNumber.Text);
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandSelect, p);
            UpdateControls();

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
            LogMessage("Sending Stop event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress() ,CompanionClient.commandStop, null);
            UpdateControls();

        }
        private async void Play_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Play event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandPlay, null);
            UpdateControls();

        }
        private async void Playpause_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Playpause event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandPlayPause, null);
            UpdateControls();

        }
        private async void Pause_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Pause event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandPause, null);
            UpdateControls();


        }
        private async void Plus_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Plus event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandPlus, null);
            UpdateControls();

        }
        private async void Minus_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Minus event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandMinus, null);
            UpdateControls();
        }
        private async void Fullscreen_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Fullscreen event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandFullScreen, null);
            UpdateControls();

        }
        private async void Fullwindow_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Fullwindow event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandFullWindow, null);
            UpdateControls();
        }
        private async void Window_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending window event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandWindow, null);
            UpdateControls();
        }
        private async void Mute_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Mute event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandMute, null);
            UpdateControls();
        }
        private async void VolumeUp_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Volume Up event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandVolumeUp, null);
            UpdateControls();

        }
        private async void VolumeDown_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Volume Down event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandVolumeDown, null);
            UpdateControls();
        }
        private async void Down_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Down event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandDown, null);
            UpdateControls();
        }
        private async void Up_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Up event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandUp, null);
            UpdateControls();
        }
        private async void Left_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Left event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandLeft, null);
            UpdateControls();
        }
        private async void Right_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Right event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandRight, null);
            UpdateControls();
        }
        private async void Enter_remote_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Sending Enter event to " + GetIPAddress());
            bool bResult = await companion.SendCommand(GetIPAddress(), CompanionClient.commandEnter, null);
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
