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
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using System.Reflection;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Activation;
using AudioVideoPlayer.CDReader;
using AudioVideoPlayer.Companion;
using Windows.Devices.Custom;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AudioVideoPlayer.Pages.CDPlayer
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CDPlayerPage : Page
    {
        #region Attributes
        // CD Reader Device Manager
        // Used to detect CD Reader connected to the PC or XBOX
        CDReaderManager cdReaderManager;
        // List of CD Reader Device 
        List<CDReaderDevice> ListDeviceInformation;
        // Current CD Metadata
        CDMetadata currentCD;
        // Flag true while copying CD on local disk
        bool bExtracting;
        // Flag true to cancel copying CD on local disk
        bool bCancelExtracting;


        Windows.Media.Playback.MediaPlayer mediaPlayer;
        // Collection of smooth streaming urls 
        private ObservableCollection<CDTrackMetadata> defaultViewModel = new ObservableCollection<CDTrackMetadata>();
        public ObservableCollection<CDTrackMetadata> DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }
        // Current Title if any
        private string CurrentTitle;
        // Url of the current playing media 
        private string CurrentMediaTrack;
        // Url of the current playing poster 
        private string CurrentPosterUrl;
        // Duration for the current playing media 
        private TimeSpan CurrentDuration;
        // Start up position of the current playing media 
        private TimeSpan CurrentStartPosition;

        #endregion

        #region Initialization
        /// <summary>
        /// CDPlayerPage constructor 
        /// </summary>
        public CDPlayerPage()
        {
            this.InitializeComponent();
            currentCD = new CDMetadata();
            ListDeviceInformation = new List<CDReaderDevice>();
            if (ComboDevices.Items != null)
                ComboDevices.Items.Clear();
            LogMessage("Application CDPlayerPage Initialized");
        }
        // localPath is used to store the path of the content to be played
        string localPath = string.Empty;

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
            LogMessage("CDPlayerPage OnNavigatedTo");
                       
            if (e.NavigationMode != NavigationMode.New)
                RestoreState();

            // Register Suspend/Resume
            Application.Current.EnteredBackground += EnteredBackground;
            Application.Current.LeavingBackground += LeavingBackground;



            // Bind player to element
            mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            mediaPlayerElement.SetMediaPlayer(mediaPlayer);
            mediaPlayer.CommandManager.IsEnabled = false;

            // Register UI components and events
            RegisterUI();
            // Register Companion
            await InitializeCompanion();
            // Register CD Manager
            RegisterCDManager();

            // Update control and play first video
            UpdateControls();




        }



        /// <summary>
        /// Method OnNavigatedFrom
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
           // LogMessage("CDPlayerPage OnNavigatedFrom");
            // Unregister Suspend/Resume
            Application.Current.EnteredBackground -= EnteredBackground;
            Application.Current.LeavingBackground -= LeavingBackground;

            // Unregister UI components and events
            UnregisterUI();

            // Stop Companion reception
            UninitializeCompanion();
            // Stop CD Manager
            UnregisterCDManager();
            // Save State
            SaveState();

            // Stop Media Player
            try
            {
                mediaPlayer.Source = null;
            }
            catch(Exception ex)
            {
                LogMessage("Exception while stopping MediaPlayer:" + ex.Message);
            }
        
        }
        /// <summary>
        /// This method Register the UI components .
        /// </summary>
        public  bool RegisterUI()
        {
            bool bResult = false;
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                SystemControls = Windows.Media.SystemMediaTransportControls.GetForCurrentView();
                if (SystemControls != null)
                {
                    SystemControls.IsEnabled = false;
                    SystemControls.ButtonPressed += SystemControls_ButtonPressed;
                    SystemControls.IsPlayEnabled = true;
                    SystemControls.IsPauseEnabled = true;
                    SystemControls.IsStopEnabled = true;
                    SystemControls.PlaybackStatus = Windows.Media.MediaPlaybackStatus.Closed;
                }
            }
            // DisplayInformation used to detect orientation changes
            displayInformation = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
            if (displayInformation != null)
            {
                // Register for orientation change
                displayInformation.OrientationChanged += displayInformation_OrientationChanged;
            }
            else
                return false;




            // Initialize mediaPlayer events
            mediaPlayer.MediaOpened += new TypedEventHandler<Windows.Media.Playback.MediaPlayer, object>(MediaElement_MediaOpened);
            mediaPlayer.MediaFailed += new TypedEventHandler<Windows.Media.Playback.MediaPlayer, Windows.Media.Playback.MediaPlayerFailedEventArgs>(MediaElement_MediaFailed);
            mediaPlayer.MediaEnded += new TypedEventHandler<Windows.Media.Playback.MediaPlayer, object>(MediaElement_MediaEnded);
            mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

            // Combobox event
            ComboTrackNumber.SelectionChanged += ComboTrackNumber_SelectionChanged;
            // Logs event to refresh the TextBox
            logs.TextChanged += Logs_TextChanged;


            return bResult;
        }






        /// <summary>
        /// This method Unregister the UI components .
        /// </summary>
        public bool UnregisterUI()
        {
            bool bResult = false;
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                SystemControls = Windows.Media.SystemMediaTransportControls.GetForCurrentView();
                if (SystemControls != null)
                    SystemControls.ButtonPressed -= SystemControls_ButtonPressed;
            }
            // DisplayInformation used to detect orientation changes
            displayInformation = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
            if (displayInformation != null)
            {
                // Unregister for orientation change
                displayInformation.OrientationChanged -= displayInformation_OrientationChanged;
            }
            else
                return false;

            // Initialize MediaElement events
            if (mediaPlayer != null)
            {
                mediaPlayer.MediaOpened -= MediaElement_MediaOpened;
                mediaPlayer.MediaFailed -= MediaElement_MediaFailed;
                mediaPlayer.MediaEnded -= MediaElement_MediaEnded;
                mediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            }
            // Combobox event
            ComboTrackNumber.SelectionChanged -= ComboTrackNumber_SelectionChanged;
            // Logs event to refresh the TextBox
            logs.TextChanged -= Logs_TextChanged;


            return bResult;
        }

        #endregion
        #region CDManager
        void RegisterCDManager()
        {
            bExtracting = false;
            bCancelExtracting = false;
            if (ComboDevices.Items != null)
                ComboDevices.Items.Clear();
            CheckListDevices();
            string selector = CustomDevice.GetDeviceSelector(new Guid("53f56308-b6bf-11d0-94f2-00a0c91efb8b"));
            IEnumerable<string> additionalProperties = new string[] { "System.Devices.DeviceInstanceId" };
            if (cdReaderManager != null)
            {
                cdReaderManager.StopDiscovery();
                cdReaderManager.CDReaderDeviceAdded -= CDReaderDevice_Added;
                cdReaderManager.CDReaderDeviceRemoved -= CDReaderDevice_Removed;
                cdReaderManager = null;
            }
            cdReaderManager = new CDReaderManager();
            cdReaderManager.CDReaderDeviceAdded += CDReaderDevice_Added;
            cdReaderManager.CDReaderDeviceRemoved += CDReaderDevice_Removed;
            cdReaderManager.StartDiscovery();

            ComboDevices.IsEnabled = true;

        }
        void UnregisterCDManager()
        {
            if (cdReaderManager != null)
            {
                cdReaderManager.StopDiscovery();
                cdReaderManager.CDReaderDeviceAdded -= CDReaderDevice_Added;
                cdReaderManager.CDReaderDeviceRemoved -= CDReaderDevice_Removed;
                cdReaderManager = null;
            }
            ComboDevices.IsEnabled = false;

        }
        void CheckListDevices()
        {
            if (ComboDevices.Items != null)
                if (ComboDevices.Items.Count == 0)
                    ComboDevices.Items.Add("None");
            if (ComboDevices.Items.Count > 1)
                ComboDevices.Items.Remove("None");

            if (ComboDevices.Items.Count > 0)
                ComboDevices.SelectedIndex = 0;
        }
        void FillComboTrack()
        {
            ComboTrackNumber.Items.Clear();
            if ((currentCD != null) &&
                (currentCD.Tracks.Count > 1))
            {
                ComboTrackNumber.IsEnabled = true;

                for (int i = 0; i < currentCD.Tracks.Count; i++)
                {
                    if (string.IsNullOrEmpty(currentCD.Tracks[i].Title))
                        currentCD.Tracks[i].Title = "Track " + currentCD.Tracks[i].Number.ToString();
                    if (string.IsNullOrEmpty(currentCD.Tracks[i].Poster))
                        currentCD.Tracks[i].Poster = "ms-appx:///Assets/MUSIC.png";
                    //string s = currentCD.Tracks[i].ToString();
                    ComboTrackNumber.Items.Add(currentCD.Tracks[i]);
                }
            }
            if (ComboTrackNumber.Items.Count > 0)
            {
                ComboTrackNumber.SelectedIndex = 0;
            }
            else
                ComboTrackNumber.IsEnabled = false;
        }
        private async void CDReaderDevice_Removed(CDReaderManager sender, CDReaderDevice args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
             () =>
             {
                 if ((ListDeviceInformation != null) && (ListDeviceInformation.Count > 0))
                 {
                     foreach (var d in ListDeviceInformation)
                     {
                         if (d.Id == args.Id)
                         {
                             ListDeviceInformation.Remove(d);
                             break;
                         }
                     }
                 }
                 ComboDevices.Items.Remove(args.Id);
                 CheckListDevices();
             });
        }

        private async void CDReaderDevice_Added(CDReaderManager sender, CDReaderDevice args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
             async () =>
             {
                 if (ListDeviceInformation != null)
                     ListDeviceInformation.Add(args);
                 ComboDevices.Items.Add(args.Id);
                 CheckListDevices();
                 await LoadMetadata();
                 if (ViewModels.StaticSettingsViewModel.AutoStart == true)
                 {
                     PlayCurrentTrack();
                 }
             });
        }
        public  async void AutoPlay(string verb)
        {
            if (string.Equals(verb, "PlayCD", StringComparison.OrdinalIgnoreCase))
            {
                LogMessage("Auto Play CD event");
                await LoadMetadata();
                PlayCurrentTrack();

            }
        }
        private async System.Threading.Tasks.Task<bool> LoadMetadata()
        {
            bool result = false;
            string id = ComboDevices.SelectedItem as string;
            if (!string.IsNullOrEmpty(id) && (id != "None"))
            {
                CDReaderDevice device = null;
                if (ListDeviceInformation != null)
                {
                    foreach (var d in ListDeviceInformation)
                    {
                        if (d.Id == id)
                            device = d;
                    }
                }
                if (device != null)
                {
                    try
                    {
                        LogMessage("Device Name: " + device.Name);
                        currentCD = await cdReaderManager.ReadMediaMetadata(device.Id);
                        if ((currentCD != null) && (currentCD.Tracks.Count > 1))
                        {
                            FillComboTrack();
                            LogMessage("Get CD Table Map successfull: " + currentCD.Tracks.Count.ToString() + " tracks");
                            result = true;
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogMessage("Exception while reading Table: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Exception while reading Table: " + ex.Message);
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Load method which load metadata on CD 
        /// </summary>
        private async void Load_Click(object sender, RoutedEventArgs e)
        {
            await LoadMetadata();
        }
        /// <summary>
        /// Eject method which eject CD 
        /// </summary>
        private async void Eject_Click(object sender, RoutedEventArgs e)
        {
            string id = ComboDevices.SelectedItem as string;
            if (!string.IsNullOrEmpty(id) && (id != "None"))
            {
                CDReaderDevice device = null;
                if (ListDeviceInformation != null)
                {
                    foreach (var d in ListDeviceInformation)
                    {
                        if (d.Id == id)
                            device = d;
                    }
                }
                if (device != null)
                {
                    try
                    {
                        {
                            LogMessage("Device Name: " + device.Name);
                            bool result = await cdReaderManager.EjectMedia(device.Id);
                            if (result == true)
                            {
                                LogMessage("Media Ejection successful");
                                if (currentCD.Tracks != null)
                                    currentCD.Tracks.Clear();
                                FillComboTrack();
                            }

                        }

                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        LogMessage("Exception while ejecting the media: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Exception while ejecting the media: " + ex.Message);
                    }
                }
            }
        }
        private async void PlayWAV_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
                filePicker.FileTypeFilter.Add(".wav");
                filePicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                filePicker.SettingsIdentifier = "WavPicker";
                filePicker.CommitButtonText = "Open WAV File to Play";

                var wavFile = await filePicker.PickSingleFileAsync();
                if (wavFile != null)
                {
                    string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(wavFile);
                    LogMessage("Selected file: " + wavFile.Path);
                    // Audio or video
                    Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl("file://" + wavFile.Path);
                    if (file != null)
                    {
                        mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);
                        mediaPlayer.Play();
                    }
                    else
                        LogMessage("Failed to load media file: " + wavFile.Path);
                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to select WAV file: Exception: " + ex.Message);
            }

        }
        private async void ExtractTrack_Click(object sender, RoutedEventArgs e)
        {
            if (bExtracting == false)
            {
                
                string id = ComboDevices.SelectedItem as string;
                if (!string.IsNullOrEmpty(id) && (id != "None"))
                {
                    CDReaderDevice device = null;
                    if (ListDeviceInformation != null)
                    {
                        foreach (var d in ListDeviceInformation)
                        {
                            if (d.Id == id)
                                device = d;
                        }
                    }
                    if (device != null)
                    {
                        try
                        {
                            {
                                LogMessage("Device Name: " + device.Name);
                                if ((ComboTrackNumber.Items != null) &&
                                    (ComboTrackNumber.Items.Count > 0))
                                {
                                    CDTrackMetadata t = ComboTrackNumber.SelectedItem as CDTrackMetadata;
                                    if (t != null)
                                    {
                                        LogMessage("Extracting Track " + t.Number.ToString());
                                        string fileName = t.Number.ToString("00") + "_track.wav";

                                        var filePicker = new Windows.Storage.Pickers.FileSavePicker();
                                        filePicker.DefaultFileExtension = ".wav";
                                        filePicker.SuggestedFileName = fileName;
                                        filePicker.FileTypeChoices.Add("WAV files", new List<string>() { ".wav" });
                                        filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                                        filePicker.SettingsIdentifier = "WavPicker";
                                        filePicker.CommitButtonText = "Save Track into a WAV File";

                                        var wavFile = await filePicker.PickSaveFileAsync();
                                        if (wavFile != null)
                                        {
                                            bExtracting = true;
                                            bCancelExtracting = false;
                                            extractTrackButton.Content = "\xE8D8";
                                            UpdateControls();
                                            string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(wavFile);
                                            if (await RecordTrack("file://" + wavFile.Path, device.Id, t.FirstSector, t.LastSector))
                                            {
                                                if(bCancelExtracting==true)
                                                    LogMessage("Extraction of track in WAV file: " + wavFile.Path.ToString() +" cancelled");
                                                else
                                                    LogMessage("Record track in WAV file: " + wavFile.Path.ToString());
                                            }
                                            else
                                                LogMessage("Error while saving record buffer in file: " + wavFile.Path.ToString());
                                            bExtracting = false;
                                            bCancelExtracting = false;
                                            extractTrackButton.Content = "\xE8ED";
                                        }
                                    }
                                }
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            LogMessage("Exception while ejecting the media: " + ex.Message);
                            bExtracting = false;
                        }
                        catch (Exception ex)
                        {
                            LogMessage("Exception while ejecting the media: " + ex.Message);
                            bExtracting = false;
                        }
                    }
                }
                UpdateControls();
            }
            else
            {
                bCancelExtracting = true;
                bExtracting = false;
                extractTrackButton.Content = "\xE8ED";
                UpdateControls();
            }
        }
        private async System.Threading.Tasks.Task<bool> RecordTrack(string wavFile, string deviceID, int startSector, int endSector)
        {

            try
            {

                bool result = false;
                if (string.IsNullOrEmpty(deviceID))
                {
                    LogMessage("Empty deviceID");
                    return result;
                }

                // Stop the current stream
                mediaPlayer.Source = null;
                mediaPlayerElement.PosterSource = null;
                mediaPlayer.AutoPlay = true;


                CDTrackStream s = await CDTrackStream.Create(deviceID, startSector, endSector);
                if (s != null)
                {
                    Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(wavFile);
                    if (file != null)
                    {
                        LogMessage("Writing into : " + file.Path);

                        Stream fs = await file.OpenStreamForWriteAsync();
                        if (fs != null)
                        {
                            const int bufferSize = 2352 * 20 * 16;
                            const int WAVHeaderLen = 44;
                            ulong FileSize = s.MaxSize;
                            byte[] array = new byte[bufferSize];
                            ulong currentSize = WAVHeaderLen;
                            for (ulong i = 0; i < FileSize + bufferSize; i += currentSize)
                            {
                                if (bCancelExtracting == true)
                                    break;
                                if (i == WAVHeaderLen)
                                    currentSize = bufferSize;
                                var b = await s.ReadAsync(array.AsBuffer(), (uint)currentSize, InputStreamOptions.None);
                                if (b != null)
                                {
                                    fs.Write(array, 0, (int)b.Length);
                                    LogMessage("Writing : " + b.Length.ToString() + " bytes " + ((((ulong)fs.Position + 1) * 100) / FileSize).ToString() + "% copied ");
                                    if (currentSize != b.Length)
                                        break;
                                }
                            }
                            fs.Flush();
                            return true;
                        }
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                LogMessage("Exception Recording Track into WAV File : " + ex.Message.ToString());
            }
            return false;
        }

        private async void ExtractTracks_Click(object sender, RoutedEventArgs e)
        {
            if(bExtracting==false)
            { 
                string id = ComboDevices.SelectedItem as string;
                if (!string.IsNullOrEmpty(id) && (id != "None"))
                {
                    CDReaderDevice device = null;
                    if (ListDeviceInformation != null)
                    {
                        foreach (var d in ListDeviceInformation)
                        {
                            if (d.Id == id)
                                device = d;
                        }
                    }
                    if (device != null)
                    {
                        try
                        {
                            {
                                LogMessage("Device Name: " + device.Name);
                                if ((ComboTrackNumber.Items != null) &&
                                    (ComboTrackNumber.Items.Count > 0))
                                {
                                    LogMessage("Extracting CD Tracks ");

                                    var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                                    folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
                                    folderPicker.FileTypeFilter.Add("*");

                                    var folder = await folderPicker.PickSingleFolderAsync();
                                    if (folder != null)
                                    {
                                        bExtracting = true;
                                        bCancelExtracting = false;
                                        extractTracksButton.Content = "\xE8D8";
                                        UpdateControls();
                                        string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);
                                        for (int i = 0; i < currentCD.Tracks.Count; i++)
                                        {
                                            if (bCancelExtracting == true)
                                                break;
                                            string fileName = currentCD.Tracks[i].Number.ToString("00") + "_track.wav";
                                            Windows.Storage.StorageFile file = await folder.CreateFileAsync(fileName,Windows.Storage.CreationCollisionOption.ReplaceExisting);
                                            if (file != null)
                                            {
                                                string Path = file.Path;
                                                if (await RecordTrack("file://" + Path, device.Id, currentCD.Tracks[i].FirstSector, currentCD.Tracks[i].LastSector))
                                                {
                                                    if (bCancelExtracting == true)
                                                        LogMessage("Extraction of track in WAV file: " + Path.ToString() + " cancelled");
                                                    else
                                                        LogMessage("Record track in WAV file: " + Path.ToString());
                                                }
                                                else
                                                    LogMessage("Error while saving track in file: " + Path.ToString());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            LogMessage("Exception while extracting tracks: " + ex.Message);
                        }
                        catch (Exception ex)
                        {
                            LogMessage("Exception while extracting tracks: " + ex.Message);
                        }
                        bExtracting = false;
                        bCancelExtracting = false;
                        extractTracksButton.Content = "\xE8EE";
                        UpdateControls();

                    }
                }
            }
            else
            {
                bCancelExtracting = true;
                bExtracting = false;
                extractTracksButton.Content = "\xE8EE";
                UpdateControls();
            }
        }

        #endregion


        #region Orientation

        /// <summary>
        /// Invoked when there is a change in the display orientation.
        /// </summary>
        /// <param name="sender">
        /// DisplayInformation object from which the new Orientation can be determined
        /// </param>
        /// <param name="e"></param>
        void displayInformation_OrientationChanged(Windows.Graphics.Display.DisplayInformation sender, object args)
        {
            LogMessage("Orientation Changed: " + sender.CurrentOrientation.ToString());
        }

        #endregion

        #region MediaElement
        /// <summary>
        /// Local Media Player use to keep the network up while in background mode
        /// </summary>
        Windows.Media.Playback.MediaPlayer localMediaPlayer = null;
        Windows.Media.Core.MediaBinder localMediaBinder = null;
        Windows.Media.Core.MediaSource localMediaSource = null;
        // used to prevent deferral from being gc'd
        Windows.Foundation.Deferral deferral = null;
        // Methode used to keep the netwotk on while the application is in background.
        // it creates a fake MediaPlayer playing from a MediaBinder source.
        // On Phone you need to create this MediaPlayer before the MediaPlayer used by the application.
        public bool BookNetworkForBackground()
        {
            bool result = false;
            try
            {
                if (localMediaBinder == null)
                {
                    localMediaBinder = new Windows.Media.Core.MediaBinder();
                    if (localMediaBinder != null)
                    {
                        localMediaBinder.Binding += LocalMediaBinder_Binding;
                    }
                }
                if (localMediaSource == null)
                {
                    localMediaSource = Windows.Media.Core.MediaSource.CreateFromMediaBinder(localMediaBinder);
                }
                if (localMediaPlayer == null)
                {
                    localMediaPlayer = new Windows.Media.Playback.MediaPlayer();
                    if (localMediaPlayer != null)
                    {
                        localMediaPlayer.CommandManager.IsEnabled = false;
                        localMediaPlayer.Source = localMediaSource;
                        result = true;
                        LogMessage("Booking network for Background task successful");
                        return result;
                    }

                }
            }
            catch(Exception ex)
            {
                LogMessage("Exception while booking network for Background task: Exception: " + ex.Message);
            }
            LogMessage("Booking network for Background task failed");
            return result;
        }
        // Method used to keep the network on while the application is in background
        private void LocalMediaBinder_Binding(Windows.Media.Core.MediaBinder sender, Windows.Media.Core.MediaBindingEventArgs args)
        {
            deferral = args.GetDeferral();
            LogMessage("Booking network for Background task running...");
        }

        // Displayinformation 
        Windows.Graphics.Display.DisplayInformation displayInformation;


        // Create this variable at a global scope. Set it to null.
        private Windows.System.Display.DisplayRequest dispRequest = null;
        /// <summary>
        /// This method request the display to prevent screen saver while the MediaElement is playing a video.
        /// </summary>
        public void RequestDisplay()
        {
            if (dispRequest == null)
            {

                // Activate a display-required request. If successful, the screen is 
                // guaranteed not to turn off automatically due to user inactivity.
                dispRequest = new Windows.System.Display.DisplayRequest();
                dispRequest.RequestActive();
            }
        }
        /// <summary>
        /// This method release the display to allow screen saver.
        /// </summary>
        public async void ReleaseDisplay()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                 () =>
                 {
                     // Insert your own code here to stop the video.
                     if (dispRequest != null)
                     {
                         // Deactivate the display request and set the var to null.
                         dispRequest.RequestRelease();
                         dispRequest = null;
                     }
                 });
        }
        /// <summary>
        /// This method is called to display the live buffer window with Creator Update SDK.
        /// </summary>
        //private void PlaybackSession_SeekableRangesChanged(Windows.Media.Playback.MediaPlaybackSession sender, object args)
        //{
            //var ranges = sender.GetSeekableRanges();
            //if ((ranges != null) && (adaptiveMediaSource != null))
            //{
            //    foreach (var time in ranges)
            //    {
            //        var times = adaptiveMediaSource.GetCorrelatedTimes();
            //        if (times != null)
            //            LogMessage("Video Buffer available from StartTime: " + time.Start.ToString() + " till EndTime: " + time.End.ToString() + " Current Position: " + times.Position.ToString() + " ProgramDateTime: " + times.ProgramDateTime.ToString());
            //    }
            //}

        //}

        /// <summary>
        /// This method is called when the Media State changed .
        /// </summary>
        private async void PlaybackSession_PlaybackStateChanged(Windows.Media.Playback.MediaPlaybackSession sender, object args)
        {
            switch (sender.PlaybackState)
            {
                case Windows.Media.Playback.MediaPlaybackState.Playing:
                    SystemControls.PlaybackStatus = Windows.Media.MediaPlaybackStatus.Playing;
                    break;
                case Windows.Media.Playback.MediaPlaybackState.Paused:
                    if(mediaPlayer.PlaybackSession.Position == TimeSpan.Zero)
                        SystemControls.PlaybackStatus = Windows.Media.MediaPlaybackStatus.Stopped;
                    else
                        SystemControls.PlaybackStatus = Windows.Media.MediaPlaybackStatus.Paused;
                    break;
                case Windows.Media.Playback.MediaPlaybackState.Buffering:
                    break;
                case Windows.Media.Playback.MediaPlaybackState.Opening:
                    break;
                case Windows.Media.Playback.MediaPlaybackState.None:
                    SystemControls.PlaybackStatus = Windows.Media.MediaPlaybackStatus.Closed;
                    break;
                default:
                    break;
            }
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                 () =>
                 {
                    LogMessage("Media CurrentState Changed: " + sender.PlaybackState.ToString());
                    if ((sender.PlaybackState == Windows.Media.Playback.MediaPlaybackState.None) ||
                        (sender.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused))
                        ReleaseDisplay();
                    if (sender.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
                        RequestDisplay();
                     UpdateControls();
                 });

        }
        /// <summary>
        /// This method is called when the Media is opened.
        /// </summary>
        private async void MediaElement_MediaOpened(Windows.Media.Playback.MediaPlayer sender, object e)
        {
            LogMessage("Media opened");
            if((mediaPlayer.PlaybackSession!=null) && (mediaPlayer.PlaybackSession.CanSeek))
            {
                mediaPlayer.PlaybackSession.Position = CurrentStartPosition;
            }
            await UpdateControlsDisplayUpdater(CurrentTitle, CurrentMediaTrack, CurrentPosterUrl);
            UpdateControls();
        }
        /// <summary>
        /// This method is called when the Media ended.
        /// </summary>
        private async void MediaElement_MediaEnded(Windows.Media.Playback.MediaPlayer sender, Object e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    LogMessage("Media ended");
                    ReleaseDisplay();
                    UpdateControls();
                    if (ViewModels.StaticSettingsViewModel.AutoStart)
                    {
                        LogMessage("Skipping to next Media on media end...");

                        if (ViewModels.StaticSettingsViewModel.ContentLoop)
                            Loop_Click(null, null);
                        else
                            Plus_Click(null, null);
                    }
                });
        }

        /// <summary>
        /// This method is called when the Media failed.
        /// </summary>
        private void MediaElement_MediaFailed(Windows.Media.Playback.MediaPlayer sender, Windows.Media.Playback.MediaPlayerFailedEventArgs e)
        {
            LogMessage("Media failed: " + e.ErrorMessage + (e.ExtendedErrorCode !=null? " Error: " + e.ExtendedErrorCode.Message: ""));
            CurrentMediaTrack = string.Empty;
            CurrentPosterUrl = string.Empty;
            ReleaseDisplay();
            UpdateControls();
        }

        /// <summary>
        /// Gets the instance of the SystemMediaTransportControls being used.
        /// </summary>
        public Windows.Media.SystemMediaTransportControls SystemControls { get; private set; }
        async void SystemControls_ButtonPressed(Windows.Media.SystemMediaTransportControls sender, Windows.Media.SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case Windows.Media.SystemMediaTransportControlsButton.Pause:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        LogMessage("Pause from SystemMediaTransportControls");
                        mediaPlayer.Pause();
                    });
                    break;
                case Windows.Media.SystemMediaTransportControlsButton.Play:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        LogMessage("Play from SystemMediaTransportControls");
                        mediaPlayer.Play();
                    });
                    break;
                case Windows.Media.SystemMediaTransportControlsButton.Stop:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        LogMessage("Stop from SystemMediaTransportControls");
                        mediaPlayer.Pause();
                        mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
                    });
                    break;
                case Windows.Media.SystemMediaTransportControlsButton.Previous:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        LogMessage("Previous from SystemMediaTransportControls");
                        Minus_Click(null, null);
                    });
                    break;
                case Windows.Media.SystemMediaTransportControlsButton.Next:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        LogMessage("Next from SystemMediaTransportControls");
                        Plus_Click(null, null);
                    });
                    break;
                case Windows.Media.SystemMediaTransportControlsButton.Rewind:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        LogMessage("Rewind from SystemMediaTransportControls");
                    });
                    break;
                case Windows.Media.SystemMediaTransportControlsButton.FastForward:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        LogMessage("FastForward from SystemMediaTransportControls");
                    });
                    break;
            }
        }
        #endregion

        #region SuspendResume



        /// <summary>
        /// This method saves the MediaElement position and the media state 
        /// it also saves the MediaCache
        /// </summary>
        void SaveState()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (mediaPlayer != null)
            {
                localSettings.Values["PlayerPosition"] = mediaPlayer.PlaybackSession.Position;
                int i = (int)mediaPlayer.PlaybackSession.PlaybackState;
                localSettings.Values["PlayerState"] = i;
                //LogMessage("SaveState - Position: " + mediaPlayer.PlaybackSession.Position.ToString() + " State: " + mediaPlayer.PlaybackSession.PlaybackState.ToString());
                mediaPlayer.Pause();
            }
        }
        /// <summary>
        /// This method restores the MediaElement position and the media state 
        /// it also restores the MediaCache
        /// </summary>
        void RestoreState()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            Object value = localSettings.Values["PlayerPosition"];
            if (value != null)
            {
                TimeSpan t = (TimeSpan)value;
                if (t != null)
                {
                    if((mediaPlayer!=null)&&(mediaPlayer.PlaybackSession!=null))
                        mediaPlayer.PlaybackSession.Position = t;
                }
            }
            value = localSettings.Values["PlayerState"];
            if (value != null)
            {
                int i = (int)value;
                Windows.Media.Playback.MediaPlaybackState t = (Windows.Media.Playback.MediaPlaybackState)i;
                if (t != Windows.Media.Playback.MediaPlaybackState.Paused)
                    if(mediaPlayer!=null)mediaPlayer.Play();
                else
                    if (mediaPlayer != null) mediaPlayer.Pause();
            }
            
            LogMessage("RestoreState - Position: " + (mediaPlayer!=null ? mediaPlayer.PlaybackSession.Position.ToString():"") + " State: " + (mediaPlayer != null ? mediaPlayer.PlaybackSession.PlaybackState.ToString():""));
        }
        /// <summary>
        /// This method is called when the application is resuming
        /// </summary>
        async void LeavingBackground(object sender, object e)
        {
            LogMessage("LeavingBackground in XAML Page");

            // Hack for the Application running on XBOX One to avoid displaying "Remote" border  
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                playButton.Focus(FocusState.Pointer);
            }
            );
        }
        /// <summary>
        /// This method is called when the application is suspending
        /// </summary>
        void EnteredBackground(System.Object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            LogMessage("EnteredBackground in XAML Page");
            var deferal = e.GetDeferral();

            deferal.Complete();
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
                                if(pos == -1)
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

        #region UIEvents 
        /// <summary>
        /// Check if the bitrates are consistent 
        /// </summary>
        private bool CheckBitrates(string smin, string smax)
        {
            uint max, min;
            if (uint.TryParse(smin, out min))
                if (uint.TryParse(smax, out max))
                    if (min <= max)
                        return true;
            return false;
        }
        /// <summary>
        /// UpdateControls Method which update the controls on the page  
        /// </summary>
        async void UpdateControls(bool bDisable = false)
        {

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                 () =>
                 {
                     if(bExtracting == true)
                     {
                         playButton.IsEnabled = false;
                         playPauseButton.IsEnabled = false;
                         pausePlayButton.IsEnabled = false;
                         stopButton.IsEnabled = false;


                         minusButton.IsEnabled = false;
                         plusButton.IsEnabled = false;

                         muteButton.IsEnabled = false;
                         volumeDownButton.IsEnabled = false;
                         volumeUpButton.IsEnabled = false;

                         ejectButton.IsEnabled = false;
                         loadButton.IsEnabled = false;
                         if(extractTrackButton.Content.ToString() == "\xE8D8")
                             extractTrackButton.IsEnabled = true;
                         else
                             extractTrackButton.IsEnabled = false;
                         if (extractTracksButton.Content.ToString() == "\xE8D8")
                             extractTracksButton.IsEnabled = true;
                         else
                             extractTracksButton.IsEnabled = false;
                         
                         playWAVButton.IsEnabled = false;

                         ComboDevices.IsEnabled = false;
                         ComboTrackNumber.IsEnabled = false;
                     }
                     else
                     {
                         if (bDisable == true)
                         {

                             playButton.IsEnabled = false;
                             playPauseButton.IsEnabled = false;
                             pausePlayButton.IsEnabled = false;
                             stopButton.IsEnabled = false;


                             minusButton.IsEnabled = false;
                             plusButton.IsEnabled = false;

                             muteButton.IsEnabled = false;
                             volumeDownButton.IsEnabled = false;
                             volumeUpButton.IsEnabled = false;

                             ejectButton.IsEnabled = false;
                             loadButton.IsEnabled = false;
                             extractTrackButton.IsEnabled = false;
                             extractTracksButton.IsEnabled = false;
                             playWAVButton.IsEnabled = true;

                             ComboTrackNumber.IsEnabled = false;
                         }
                         else
                         {
                             {
                                 ComboTrackNumber.IsEnabled = true;

                                 if ((ComboTrackNumber.Items.Count > 0))
                                 {
                                     playButton.IsEnabled = true;

                                     ejectButton.IsEnabled = false;
                                     loadButton.IsEnabled = false;
                                     extractTrackButton.IsEnabled = false;
                                     extractTracksButton.IsEnabled = false;
                                     playWAVButton.IsEnabled = true;

                                     minusButton.IsEnabled = true;
                                     plusButton.IsEnabled = true;
                                     muteButton.IsEnabled = true;
                                     volumeDownButton.IsEnabled = true;
                                     volumeUpButton.IsEnabled = true;

                                     playPauseButton.IsEnabled = false;
                                     pausePlayButton.IsEnabled = false;
                                     stopButton.IsEnabled = false;


                                     {
                                         if (mediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
                                         {
                                             playPauseButton.IsEnabled = false;
                                             pausePlayButton.IsEnabled = true;
                                             stopButton.IsEnabled = true;

                                         }
                                         else if (mediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused)
                                         {
                                             playPauseButton.IsEnabled = true;
                                             stopButton.IsEnabled = true;
                                         }
                                         else if (mediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.None)
                                         {
                                             ejectButton.IsEnabled = true;
                                             loadButton.IsEnabled = true;
                                             extractTrackButton.IsEnabled = true;
                                             extractTracksButton.IsEnabled = true;
                                         }
                                     }
                                     // Volume buttons control
                                     if (mediaPlayer.IsMuted)
                                         muteButton.Content = "\xE767";
                                     else
                                         muteButton.Content = "\xE74F";
                                     if (mediaPlayer.Volume == 0)
                                     {
                                         volumeDownButton.IsEnabled = false;
                                         volumeUpButton.IsEnabled = true;
                                     }
                                     else if (mediaPlayer.Volume >= 1)
                                     {
                                         volumeDownButton.IsEnabled = true;
                                         volumeUpButton.IsEnabled = false;
                                     }
                                     else
                                     {
                                         volumeDownButton.IsEnabled = true;
                                         volumeUpButton.IsEnabled = true;
                                     }
                                 }
                             }
                         }
                     }
                 });
        }

        /// <summary>
        /// Play method which plays the video with the MediaElement from position 0
        /// </summary>
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PlayCurrentTrack();
            }
            catch (Exception ex)
            {
                LogMessage("Failed to play current track Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Stop method which stops the video currently played by the MediaElement
        /// </summary>
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("Stop current track");
                mediaPlayer.Source = null;
            }
            catch (Exception ex)
            {
                LogMessage("Failed to stop current track Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Select an Item in the Combo box
        /// </summary>
        private void select_Click(object sender, RoutedEventArgs e, int newIndex)
        {
            try
            {
                if ((newIndex >= 0) && (newIndex < ComboTrackNumber.Items.Count))
                {
                    int Index = ComboTrackNumber.SelectedIndex;
                    ComboTrackNumber.SelectedIndex = newIndex;
                    if (Index != newIndex)
                    {
                        PlayCurrentTrack();
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to to play current track Exception: " + ex.Message);
            }
        }


        /// <summary>
        /// Play method which plays the video currently paused by the MediaElement
        /// </summary>
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                    LogMessage("Play current track");
                    mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                LogMessage("Failed to play current track  Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Pause method which pauses the video currently played by the MediaElement
        /// </summary>
        private void PausePlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("Play current track");
                mediaPlayer.Pause();
            }
            catch (Exception ex)
            {
                LogMessage("Failed to play current track Exception: " + ex.Message);
            }
        }




        /// <summary>
        /// Loop method 
        /// </summary>
        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int Index = ComboTrackNumber.SelectedIndex;
                if (Index >= 0)
                {
                    ComboTrackNumber.SelectedIndex = Index;
                    PlayCurrentTrack();
                }
                else
                    LogMessage("Error getting current playlis index");

            }
            catch (Exception ex)
            {
                LogMessage("Failed to to play current track Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Channel up method 
        /// </summary>
        private void Plus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int Index = ComboTrackNumber.SelectedIndex;
                if ((Index + 1) >= ComboTrackNumber.Items.Count)
                {
                    if (ViewModels.StaticSettingsViewModel.PlaylistLoop == true)
                        Index = 0;
                    else
                        Index = -1;
                }
                else
                    ++Index;
                if (Index >= 0)
                {
                    ComboTrackNumber.SelectedIndex = Index;
                    PlayCurrentTrack();
                }
                else
                    LogMessage("End of playlist loop");

            }
            catch (Exception ex)
            {
                LogMessage("Failed to to play Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Channel down method 
        /// </summary>
        private void Minus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int Index = ComboTrackNumber.SelectedIndex;

                if (Index - 1 >= 0)
                    --Index;
                else
                {
                    if (ViewModels.StaticSettingsViewModel.PlaylistLoop == true)
                        Index = ComboTrackNumber.Items.Count - 1;
                    else
                        Index = -1; 
                }
                if (Index >= 0)
                {
                    ComboTrackNumber.SelectedIndex = Index;
                    PlayCurrentTrack();
                }
                else
                    LogMessage("End of playlist loop");
            }
            catch (Exception ex)
            {
                LogMessage("Failed to play current track Exception: " + ex.Message);
            }
        }


        /// <summary>
        /// Mute method 
        /// </summary>
        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Toggle Mute");
            mediaPlayer.IsMuted = !mediaPlayer.IsMuted;
            UpdateControls();
        }
        /// <summary>
        /// Volume Up method 
        /// </summary>
        private void VolumeUp_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Volume Up");
            mediaPlayer.Volume = (mediaPlayer.Volume + 0.10 <= 1 ? mediaPlayer.Volume + 0.10 : 1) ;
            UpdateControls();
        }
        /// <summary>
        /// Volume Down method 
        /// </summary>
        private void VolumeDown_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Volume Down");
            mediaPlayer.Volume = (mediaPlayer.Volume - 0.10 >= 0 ? mediaPlayer.Volume - 0.10 : 0);
            UpdateControls();
        }

        /// <summary>
        /// This method is called when the ComboTrackNumber selection changes 
        /// </summary>
        private void ComboTrackNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboTrackNumber.SelectedItem != null)
            {
                CDTrackMetadata ms = ComboTrackNumber.SelectedItem as CDTrackMetadata;
                mediaPlayer.Source = null;
                UpdateControls();
            }

        }
        #endregion


        #region Media
        /// <summary>
        /// This method checks if the url is a picture url 
        /// </summary>
        private bool IsPicture(string url)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(url))
            {
                if ((url.ToLower().EndsWith(".jpg")) ||
                    (url.ToLower().EndsWith(".png")) ||
                    (url.ToLower().EndsWith(".bmp")) ||
                    (url.ToLower().EndsWith(".ico")) ||
                    (url.ToLower().EndsWith(".gif")))
                {
                    result = true;
                }
            }
            return result;
        }
        /// <summary>
        /// This method checks if the url is a video url 
        /// </summary>
        private bool IsVideo(string url)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(url))
            {
                if ((url.ToLower().EndsWith(".wmv")) ||
                    (url.ToLower().EndsWith(".mp4")) ||
                    (url.ToLower().EndsWith(".mov")) ||
                    (url.ToLower().EndsWith(".mkv")) ||
                    (url.ToLower().EndsWith(".ismv")) ||
                    (url.ToLower().EndsWith(".avi")) ||
                    (url.ToLower().EndsWith(".asf")) ||
                    (url.ToLower().EndsWith(".ts")))
                {
                    result = true;
                }
            }
            return result;
        }        /// <summary>
                 /// This method checks if the url is a music url 
                 /// </summary>
        private bool IsMusic(string url)
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
        /// This method checks if the url is the url of a local file 
        /// </summary>
        private bool IsLocalFile(string url)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(url))
            {
                if ((url.ToLower().StartsWith("file://")) ||
                    (url.ToLower().StartsWith("ms-appx://")) ||
                    (url.ToLower().StartsWith("picture://")) ||
                    (url.ToLower().StartsWith("video://")) ||
                    (url.ToLower().StartsWith("music://")))
                {
                    result = true;
                }
            }
            return result;
        }
        /// <summary>
        /// This method prepare the MediaElement to play any content (video, audio, pictures): SMOOTH, DASH, HLS, MP4, WMV, MPEG2-TS, JPG, PNG,...
        /// </summary>
        private async void PlayCurrentTrack()
        {

            string id = ComboDevices.SelectedItem as string;
            if (!string.IsNullOrEmpty(id) && (id != "None"))
            {
                CDReaderDevice device = null;
                if (ListDeviceInformation != null)
                {
                    foreach (var d in ListDeviceInformation)
                    {
                        if (d.Id == id)
                            device = d;
                    }
                }
                if (device != null)
                {
                    CDTrackMetadata item = ComboTrackNumber.SelectedItem as CDTrackMetadata;
                    if (item != null)
                    {
                        await StartPlay(item.Title, item.Number, item.Poster, 0, 0, id, item.FirstSector, item.LastSector);
                        UpdateControls();
                    }
                }
            }
        }
        string GetExtension(string url)
        {
            string result = null;
            if(!string.IsNullOrEmpty(url))
            {
                int pos = url.LastIndexOf('.');
                if(pos>0)
                    result = url.Substring(pos);
            }
            return result;
        }
        async System.Threading.Tasks.Task<string> DownloadRemoteFile(string sourceUrl)
        {
            string result = null;
            string extension = GetExtension(sourceUrl);
            string rootname = "poster";
            string filename = string.Empty;
            int index = -1;
            if (!string.IsNullOrEmpty(extension))
            {
                
                while ((result == null)&&(index<2))
                {
                    index++;
                    filename = rootname + "_" + index.ToString();
                    try
                    {
                        Windows.Storage.StorageFolder folder = Windows.Storage.ApplicationData.Current.LocalFolder;
                        if (folder != null)
                        {
                            Windows.Storage.StorageFile file = await folder.CreateFileAsync(filename + extension, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                            if (file != null)
                            {
                                using (var client = new Windows.Web.Http.HttpClient())
                                {
                                    var response = await client.GetAsync(new Uri(sourceUrl));
                                    if (response != null && response.StatusCode == Windows.Web.Http.HttpStatusCode.Ok)
                                    {
                                        using (var stream = await response.Content.ReadAsInputStreamAsync())
                                        {
                                            Stream writeStream = await file.OpenStreamForWriteAsync();
                                            if (writeStream != null)
                                            {
                                                await stream.AsStreamForRead().CopyToAsync(writeStream);
                                                await writeStream.FlushAsync();
                                                result = file.Path;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (UnauthorizedAccessException e)
                    {
                         System.Diagnostics.Debug.WriteLine("UnauthorizedAccessException while saving remote file locally : " + e.Message);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception while saving remote file locally : " + ex.Message);
                        break;
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Return poster stream
        /// </summary>
        private async System.Threading.Tasks.Task<Windows.Storage.Streams.RandomAccessStreamReference> CreatePosterStream(string poster)
        {
            Windows.Storage.Streams.RandomAccessStreamReference s = null;
            if (!string.IsNullOrEmpty(poster))
            {
                if (!IsLocalFile(poster))
                {
                    string localPath = await DownloadRemoteFile(poster);
                    if (!string.IsNullOrEmpty(localPath))
                        poster = "file://" + localPath;
                    else
                        return null;
                }
                if (IsPicture(poster))
                {
                    Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(poster);
                    if (file == null)
                        file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new System.Uri("ms-appx:///Assets/Music.png"));
                    if (file != null)
                        s = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(file);
                }

            }
            return s;
        }

        /// <summary>
        /// This method Update the SystemControls Display information
        /// </summary>
        private async System.Threading.Tasks.Task<bool> UpdateControlsDisplayUpdater(string title, string content, string poster)
        {
            LogMessage("Updating SystemControls");
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
            async () =>
            {

                if ((ComboTrackNumber.Items.Count > 1) && (ViewModels.StaticSettingsViewModel.AutoStart))
                {
                    SystemControls.IsPreviousEnabled = true;
                    SystemControls.IsNextEnabled = true;
                }
                if (string.IsNullOrEmpty(content))
                    SystemControls.DisplayUpdater.ClearAll();
                else
                {
                    if (IsPicture(content))
                    {
                        SystemControls.DisplayUpdater.Type = Windows.Media.MediaPlaybackType.Image;
                        SystemControls.DisplayUpdater.ImageProperties.Title = title;
                        SystemControls.DisplayUpdater.ImageProperties.Subtitle = content;
                        SystemControls.DisplayUpdater.Thumbnail = await CreatePosterStream(poster);
                        LogMessage("Updating SystemControls done for image");
                    }
                    else if (IsMusic(content))
                    {
                        SystemControls.DisplayUpdater.Type = Windows.Media.MediaPlaybackType.Music;
                        SystemControls.DisplayUpdater.MusicProperties.Title = title;
                        SystemControls.DisplayUpdater.MusicProperties.Artist = content;
                        SystemControls.DisplayUpdater.Thumbnail = await CreatePosterStream(poster);
                        LogMessage("Updating SystemControls done for music");
                    }
                    else
                    {
                        SystemControls.DisplayUpdater.Type = Windows.Media.MediaPlaybackType.Video;
                        SystemControls.DisplayUpdater.VideoProperties.Title = title;
                        SystemControls.DisplayUpdater.VideoProperties.Subtitle = content;
                        SystemControls.DisplayUpdater.Thumbnail = await CreatePosterStream(poster);
                        LogMessage("Updating SystemControls done for video");

                    }
                }
                SystemControls.DisplayUpdater.Update();
            });
            return true;
        }
        /// <summary>
        /// Set the source for the picture : windows and fullscreen 
        /// </summary>
        void SetPictureSource(Windows.UI.Xaml.Media.Imaging.BitmapImage b)
        {
            // Set picture source for windows element
            pictureElement.Source = b;
        }
        /// <summary>
        /// Resize the pictureElement to match with the BackgroundVideo size
        /// </summary>      
        void SetPictureElementSize()
        {
            Windows.UI.Xaml.Media.Imaging.BitmapImage b = pictureElement.Source as Windows.UI.Xaml.Media.Imaging.BitmapImage;
            if (b != null)
            {
                int nWidth;
                int nHeight;
                double ratioBackground = backgroundVideo.ActualWidth / backgroundVideo.ActualHeight;
                double ratioPicture = ((double)b.PixelWidth / (double)b.PixelHeight);
                if (ratioPicture > ratioBackground)
                {
                    nWidth = (int)backgroundVideo.ActualWidth;
                    nHeight = (int)(nWidth / ratioPicture);
                }
                else
                {
                    nHeight = (int)backgroundVideo.ActualHeight;
                    nWidth = (int)(nHeight * ratioPicture);

                }
                pictureElement.Width = nWidth;
                pictureElement.Height = nHeight;
            }
        }
        private async System.Threading.Tasks.Task<bool> SetDefaultPoster()
        {
            var uri = new System.Uri("ms-appx:///Assets/Music.png");
            Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            if (file != null)
            {
                using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    if (fileStream != null)
                    {
                        Windows.UI.Xaml.Media.Imaging.BitmapImage b = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                        if (b != null)
                        {
                            b.SetSource(fileStream);
                            SetPictureSource(b);
                            SetPictureElementSize();
                            return true;
                        }
                    }
                }
            }
            return false;

        }
        /// <summary>
        /// This method set the poster source for the MediaElement 
        /// </summary>
        private async System.Threading.Tasks.Task<bool> SetPosterUrl(string PosterUrl)
        {
            if (IsPicture(PosterUrl))
            {
                if (IsLocalFile(PosterUrl))
                {
                    try
                    {
                        Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(PosterUrl);
                        if (file != null)
                        {
                            using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                            {
                                if (fileStream != null)
                                {
                                    Windows.UI.Xaml.Media.Imaging.BitmapImage b = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                                    if (b != null)
                                    {
                                        b.SetSource(fileStream);
                                        SetPictureSource(b);
                                        SetPictureElementSize();
                                        return true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            await SetDefaultPoster();
                            LogMessage("Failed to load poster: " + PosterUrl);
                            return true;
                        }

                    }
                    catch (Exception e)
                    {
                        LogMessage("Exception while loading poster: " + PosterUrl + " - " + e.Message);
                    }
                }
                else
                {
                    try
                    {
                        
                        // Load the bitmap image over http
                        //Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();
                        //Windows.Storage.Streams.InMemoryRandomAccessStream ras = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                        //using (var stream = await httpClient.GetInputStreamAsync(new Uri(PosterUrl)))
                        //{
                        //    if (stream != null)
                        //    {
                        //        await stream.AsStreamForRead().CopyToAsync(ras.AsStreamForWrite());
                        //        ras.Seek(0);
                        //        var b = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                        //        if (b != null)
                        //        {
                        //            await b.SetSourceAsync(ras);
                        //            SetPictureSource(b);
                        //            SetPictureElementSize();
                        //            return true;
                        //        }
                        //    }
                        //}

                        using (var client = new Windows.Web.Http.HttpClient())
                        {
                            
                            var response = await client.GetAsync(new Uri(PosterUrl));
                            var b = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                            if (response != null && response.StatusCode == Windows.Web.Http.HttpStatusCode.Ok)
                            {
                                using (var stream = await response.Content.ReadAsInputStreamAsync())
                                {
                                    using (var memStream = new MemoryStream())
                                    {
                                        await stream.AsStreamForRead().CopyToAsync(memStream);
                                        memStream.Position = 0;
                                        b.SetSource(memStream.AsRandomAccessStream());
                                        SetPictureSource(b);
                                        SetPictureElementSize();
                                        return true;
                                    }
                                }
                            }
                            else
                            {
                                await SetDefaultPoster();
                                return true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception: " + e.Message);
                    }
                }
            }
            if (IsMusic(PosterUrl))
            {
                if (IsLocalFile(PosterUrl))
                {
                    try
                    {
                        Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(PosterUrl);
                        if (file != null)
                        {

                            // Thumbnail
                            using (var thumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.MusicView, 300))
                            {
                                if (thumbnail != null && 
                                    (thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Image))
                                    //||
                                    //thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Icon))
                                {
                                    var fileStream = thumbnail.AsStream().AsRandomAccessStream();
                                    if (fileStream != null)
                                    {
                                        Windows.UI.Xaml.Media.Imaging.BitmapImage b = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                                        if (b != null)
                                        {
                                            b.SetSource(fileStream);
                                            SetPictureSource(b);
                                            SetPictureElementSize();
                                            return true;
                                        }
                                    }

                                }
                                else
                                {
                                    //Error Message here
                                }
                            }

                        }
                        else
                        {
                            await SetDefaultPoster();
                            LogMessage("Failed to load poster: " + PosterUrl);
                            return true;
                        }

                    }
                    catch (Exception e)
                    {
                        LogMessage("Exception while loading poster: " + PosterUrl + " - " + e.Message);
                    }
                }
            }

            return false;
        }
        private async System.Threading.Tasks.Task<bool> StartPlay(string title, int trackNumber, string poster, long start, long duration,string deviceID, int startSector, int endSector)
        {

            try
            {

                bool result = false;
                if (string.IsNullOrEmpty(deviceID))
                {
                    LogMessage("Empty deviceID");
                    return result;
                }

                // Stop the current stream
                mediaPlayer.Source = null;
                mediaPlayerElement.PosterSource = null;
                mediaPlayer.AutoPlay = true;
                CurrentMediaTrack = string.Empty;
                CurrentPosterUrl = string.Empty;
                CurrentStartPosition = new TimeSpan(0);
                CurrentDuration = new TimeSpan(0);
                string contentType = "audio/wav";
                mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStream(await CDTrackStream.Create(deviceID, startSector, endSector), contentType);
                mediaPlayer.Play();
                CurrentStartPosition = new TimeSpan(start * 10000);
                CurrentDuration = new TimeSpan(duration * 10000);
                CurrentMediaTrack = trackNumber.ToString();
                CurrentPosterUrl = poster;
                CurrentTitle = title;
                SystemControls.IsEnabled = true;
                pictureElement.Visibility = Visibility.Visible;
                mediaPlayerElement.Visibility = Visibility.Collapsed;
                await SetPosterUrl(CurrentPosterUrl);
                return true;
            }
            catch (Exception ex)
            {
                LogMessage("Exception while Playing current track: " + ex.Message.ToString());
            }
            return false;
        }

        /// <summary>
        /// StartPlay
        /// Start to play pictures, audio content or video content
        /// </summary>
        /// <param name="content">Url string of the content to play </param>
        /// <param name="poster">Url string of the poster associated with the content to play </param>
        /// <param name="start">start position of the content to play in milliseconds</param>
        /// <param name="duration">duration of the content to play in milliseconds</param>
        /// <returns>true if success</returns>
        private async System.Threading.Tasks.Task<bool> StartPlay(string title, string content, string poster, long start, long duration)
        {

            try
            {

                bool result = false;
                if (string.IsNullOrEmpty(content))
                {
                    LogMessage("Empty Uri");
                    return result;
                }
                LogMessage("Start to play: " + content + (string.IsNullOrEmpty(poster)?"" : " with poster: " + poster) + (start>0?" from " + start.ToString() + "ms":"") + (start > 0 ? " during " + duration.ToString() + "ms" : "")) ;

                // Stop the current stream
                mediaPlayer.Source = null;
                mediaPlayerElement.PosterSource = null;
                mediaPlayer.AutoPlay = true;
                CurrentMediaTrack = string.Empty;
                CurrentPosterUrl = string.Empty;
                CurrentStartPosition = new TimeSpan(0);
                CurrentDuration = new TimeSpan(0);
                pictureElement.Visibility = Visibility.Visible;
                mediaPlayerElement.Visibility = Visibility.Collapsed;
                // Audio or video
                if (!IsPicture(content))
                {
                    if (!IsMusic(content))
                    {
                        // fix for video 
                        pictureElement.Visibility = Visibility.Collapsed;
                        mediaPlayerElement.Visibility = Visibility.Visible;
                    }
                    result = await SetAudioVideoUrl(content);
                    if (result == true)
                        mediaPlayer.Play();

                }
                if (result == true)
                {
                    CurrentStartPosition = new TimeSpan(start * 10000);
                    CurrentDuration = new TimeSpan(duration * 10000);
                    CurrentMediaTrack = content;
                    CurrentPosterUrl = poster;
                    CurrentTitle = title;
                    SystemControls.IsEnabled = true;
                    if(IsPicture(content))
                        await UpdateControlsDisplayUpdater(CurrentTitle, CurrentMediaTrack, CurrentPosterUrl);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogMessage("Exception Playing: " + ex.Message.ToString());
                CurrentMediaTrack = string.Empty;
                CurrentPosterUrl = string.Empty;
            }
            return false;
        }
        /// <summary>
        /// GetFileFromLocalPathUrl
        /// Return the StorageFile associated with the url  
        /// </summary>
        /// <param name="PosterUrl">Url string of the content</param>
        /// <returns>StorageFile</returns>
        private async System.Threading.Tasks.Task<Windows.Storage.StorageFile> GetFileFromLocalPathUrl(string PosterUrl)
        {
            string path = null;
            Windows.Storage.StorageFolder folder = null;
            if (PosterUrl.ToLower().StartsWith("picture://"))
            {
                folder = Windows.Storage.KnownFolders.PicturesLibrary;
                path = PosterUrl.Replace("picture://", "");
            }
            else if (PosterUrl.ToLower().StartsWith("music://"))
            {
                folder = Windows.Storage.KnownFolders.MusicLibrary;
                path = PosterUrl.Replace("music://", "");
            }
            else if (PosterUrl.ToLower().StartsWith("video://"))
            {
                folder = Windows.Storage.KnownFolders.VideosLibrary;
                path = PosterUrl.Replace("video://", "");
            }
            else if (PosterUrl.ToLower().StartsWith("file://"))
            {
                path = PosterUrl.Replace("file://", "");
                // As it's local path should be on USB storage
                /*
                var devices = Windows.Storage.KnownFolders.RemovableDevices;
                IReadOnlyList<Windows.Storage.StorageFolder> rootFolders = await devices.GetFoldersAsync();
                if ((rootFolders != null) && (rootFolders.Count > 0))
                {
                    foreach (Windows.Storage.StorageFolder rootFolder in rootFolders)
                    {
                        if (path.StartsWith(rootFolder.Path, StringComparison.CurrentCultureIgnoreCase))
                        {
                            folder = rootFolder;
                          //  path = path.Replace(rootFolder.Path, "");
                            break;
                        }
                    }
                }*/
            }
            else if (PosterUrl.ToLower().StartsWith("ms-appx://"))
            {
                path = PosterUrl;
            }
            Windows.Storage.StorageFile file = null;
            try
            {
                if (folder != null)
                {
                    string ext = System.IO.Path.GetExtension(path);
                    string filename = System.IO.Path.GetFileName(path);
                    string directory = System.IO.Path.GetDirectoryName(path);
                    while (!string.IsNullOrEmpty(directory))
                    {

                        string subdirectory = directory;
                        int pos = -1;
                        if ((pos = directory.IndexOf('\\')) > 0)
                            subdirectory = directory.Substring(0, pos);
                        folder = await folder.GetFolderAsync(subdirectory);
                        if (folder != null)
                        {
                            if (pos > 0)
                                directory = directory.Substring(pos + 1);
                            else
                                directory = string.Empty;
                        }
                    }
                    if (folder != null)
                        file = await folder.GetFileAsync(filename);
                }
                else
                {
                    if(path.StartsWith("ms-appx://"))
                        file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri(path));
                    else
                        file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                }
            }
            catch(Exception e)
            {
                LogMessage("Exception while opening file: " + PosterUrl + " exception: " + e.Message);
            }
            return file;
        }

        /// <summary>
        /// ParseQueryString
        /// Parse Query string .
        /// </summary>
        /// <param name="content">Uri to parse</param>
        /// <returns>Disctionnary</returns>
        public static IReadOnlyDictionary<string, string> ParseQueryString(Uri uri)
        {
            Regex _regex = new Regex(@"[?|&]([\w\.]+)=([^?|^&]+)");
            var match = _regex.Match(uri.PathAndQuery);
            var paramaters = new Dictionary<string, string>();
            while (match.Success)
            {
                paramaters.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            }
            return paramaters;
        }

        /// <summary>
        /// SetAudioVideoUrl
        /// Prepare the MediaElement to play audio or video content 
        /// </summary>
        /// <param name="content">Url string of the content to play </param>
        /// <returns>true if success</returns>
        private async System.Threading.Tasks.Task<bool> SetAudioVideoUrl(string Content)
        {
            try
            {
                if (IsLocalFile(Content))
                {
                    Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(Content);
                    if (file != null)
                    {
                        mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);
                        return true;
                    }
                    else
                        LogMessage("Failed to load media file: " + Content);
                }
            }
            catch (Exception ex)
            {
                LogMessage("Exception Playing: " + ex.Message.ToString());
                CurrentMediaTrack = string.Empty;
                CurrentPosterUrl = string.Empty;
            }
            return false;
        }
        #endregion


        #region Settings


        #endregion


        #region DeviceWatcher
        private Windows.Devices.Enumeration.DeviceWatcher deviceWatcher;

        /// <summary>
        /// This method Register the Device Watcher .
        /// </summary>
        public bool RegisterDeviceWatcher()
        {
            try
            {
                LogMessage("Registering DeviceWatcher...");
                deviceWatcher = Windows.Devices.Enumeration.DeviceInformation.CreateWatcher(Windows.Devices.Enumeration.DeviceClass.PortableStorageDevice);
                if (deviceWatcher != null)
                {
                    deviceWatcher.Added += DeviceAdded;
                    deviceWatcher.Removed += DeviceRemoved;
                    deviceWatcher.Start();
                    return true;
                }
            }
            catch(Exception e)
            {
                LogMessage("Exception while registering DeviceWatcher: " + e.Message);
            }
            return false;
        }
        /// <summary>
        /// This method Unregister the Device Watcher .
        /// </summary>
        public bool UnregisterDeviceWatcher()
        {
            if (deviceWatcher != null)
            {
                deviceWatcher.Stop();
                deviceWatcher.Added -= DeviceAdded;
                deviceWatcher.Removed -= DeviceRemoved;
                deviceWatcher = null;
                return true;
            }
            return false;
        }
        private async System.Threading.Tasks.Task<bool> DisplayExternalStorage()
        {
            try
            {

                var devices = Windows.Storage.KnownFolders.RemovableDevices;
                IReadOnlyList<Windows.Storage.StorageFolder> rootFolders = await devices.GetFoldersAsync();
                if ((rootFolders != null) && (rootFolders.Count > 0))
                {
                    LogMessage("List of external Devices:");
                    foreach (Windows.Storage.StorageFolder rootFolder in rootFolders)
                    {
                        LogMessage("  External Device: " + rootFolder.Name + " path: " + rootFolder.Path);
                    }
                }
            }
            catch (Exception e)
            {
                LogMessage("Exception while displaying Removable Devices: " + e.Message);
            }

            return true;

        }
        private async void DeviceAdded(Windows.Devices.Enumeration.DeviceWatcher dw, Windows.Devices.Enumeration.DeviceInformation di)
        {


            LogMessage("Removable Device Added...");
            await DisplayExternalStorage();
        }
        private async void DeviceRemoved(Windows.Devices.Enumeration.DeviceWatcher dw, Windows.Devices.Enumeration.DeviceInformationUpdate diu)
        {
            LogMessage("Removable Device Removed...");
            await DisplayExternalStorage();
        }
        #endregion





        #region Companion
        private CompanionConnectionManager companionConnectionManager;
        private CompanionDevice localCompanionDevice;
        private System.Threading.Mutex companionMutex = new System.Threading.Mutex(false, "CompanionSync");

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

                    try
                    {
                        companionMutex.WaitOne();
                        companionConnectionManager = new CompanionConnectionManager();
                        if (companionConnectionManager != null)
                        {
                            localCompanionDevice = new CompanionDevice(string.Empty, false, Information.SystemInformation.DeviceName, companionConnectionManager.GetSourceIP(), Information.SystemInformation.SystemFamily);
                            companionConnectionManager.MessageReceived += CompanionConnectionManager_MessageReceived;
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
                    catch (Exception ex)
                    {
                        LogMessage("Exception while initializing Companion Maanger: " + ex.Message);
                    }
                    finally
                    {
                        companionMutex.ReleaseMutex();
                    }
                }
                else
                {
                    try
                    {
                        companionMutex.WaitOne();
                        multicast = "Multicast ";
                        companionConnectionManager = new MulticastCompanionConnectionManager();
                        if (companionConnectionManager != null)
                        {
                            localCompanionDevice = new CompanionDevice(string.Empty, false, Information.SystemInformation.DeviceName, companionConnectionManager.GetSourceIP(), Information.SystemInformation.SystemFamily);
                            companionConnectionManager.MessageReceived += CompanionConnectionManager_MessageReceived;
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
                    catch (Exception ex)
                    {
                        LogMessage("Exception while initializing Companion Maanger: " + ex.Message);
                    }
                    finally
                    {
                        companionMutex.ReleaseMutex();
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
            try
            {
                companionMutex.WaitOne();
                if (companionConnectionManager != null)
                {
                    companionConnectionManager.MessageReceived -= CompanionConnectionManager_MessageReceived;
                    companionConnectionManager.Uninitialize();
                    companionConnectionManager = null;
                }
            }
            catch (Exception ex)
            {
                LogMessage("Exception while uninitializing Companion Maanger: " + ex.Message);
            }
            finally
            {
                companionMutex.ReleaseMutex();
            }
            return true;
        }

        private async void CompanionConnectionManager_MessageReceived(CompanionDevice sender, string args)
        {

            LogMessage("Received a remote command from device: " + sender.Name + " type: " + sender.Kind + " at IP Address: " + sender.IPAddress);
            LogMessage("Received a remote command : " + args);
            string Command = CompanionProtocol.GetCommandFromMessage(args);
            Dictionary<string, string> Parameters = CompanionProtocol.GetParametersFromMessage(args);

            if (Parameters == null)
                LogMessage("Command Received: " + Command);
            else
            {
                string parameter = string.Empty;
                foreach (var v in Parameters)
                {
                    parameter += " " + v.Key + "=" + v.Value;
                }
                LogMessage("Command Received: " + Command + " Parameter: " + parameter);
            }
            switch (Command)
            {
                //https://testcertstorage.blob.core.windows.net/images/Radio.json
                //https://testcertstorage.blob.core.windows.net/images/Photos.json
                //https://testcertstorage.blob.core.windows.net/images/TVLive.json
                //https://testcertstorage.blob.core.windows.net/images/Video.json
                //https://testcertstorage.blob.core.windows.net/images/AudioVideoData.json

                case CompanionProtocol.commandPlusPlaylist:

                    break;
                case CompanionProtocol.commandMinusPlaylist:

                    break;
                case CompanionProtocol.commandOpenPlaylist:
                    break;
                case CompanionProtocol.commandOpen:
                    break;
                case CompanionProtocol.commandPlay:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Play_Click(null, null);
                    });
                    break;
                case CompanionProtocol.commandStop:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Stop_Click(null, null);
                    });
                    break;
                case CompanionProtocol.commandPause:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        PausePlay_Click(null, null);
                    });
                    break;
                case CompanionProtocol.commandPlayPause:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        PlayPause_Click(null, null);
                    });
                    break;
                case CompanionProtocol.commandSelect:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        int newIndex = -1;
                        if ((Parameters != null) &&
                            (Parameters.ContainsKey(CompanionProtocol.parameterIndex)))
                        {

                            if (int.TryParse(Parameters[CompanionProtocol.parameterIndex], out newIndex))
                                select_Click(null, null, newIndex);
                        }
                    });
                    break;
                case CompanionProtocol.commandPlus:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Plus_Click(null, null);
                    });
                    break;
                case CompanionProtocol.commandMinus:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Minus_Click(null, null);
                    });
                    break;
                case CompanionProtocol.commandFullWindow:
                    break;
                case CompanionProtocol.commandFullScreen:
                    break;
                case CompanionProtocol.commandWindow:
                    break;
                case CompanionProtocol.commandMute:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Mute_Click(null, null);
                    });
                    break;
                case CompanionProtocol.commandVolumeUp:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        VolumeUp_Click(null, null);
                    });
                    break;
                case CompanionProtocol.commandVolumeDown:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        VolumeDown_Click(null, null);
                    });
                    break;
                case CompanionProtocol.commandPing:
                    LogMessage("PING Command received");
                    break;
                case CompanionProtocol.commandPingResponse:
                    LogMessage("PINGRESPONSE Command received");
                    break;
                default:
                    LogMessage("Unknown Command");
                    break;
            }
        }





        #endregion



    }


}
