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
using Windows.ApplicationModel.Activation;
using Windows.Storage.FileProperties;
using Windows.Web.Http.Filters;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AudioVideoPlayer.Pages.Player
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayerPage : Page, Microsoft.Media.AdaptiveStreaming.IDownloaderPlugin
    {
        #region Attributes
        Windows.Media.Playback.MediaPlayer mediaPlayer;
        // Collection of smooth streaming urls 
        private ObservableCollection<MediaItem> defaultViewModel = new ObservableCollection<MediaItem>();
        public ObservableCollection<MediaItem> DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }
        // Timer used to automatically skip to the next stream
        private DispatcherTimer timer ;
        // Popup used to display picture in fullscreen mode
        private Popup picturePopup = null;
        enum WindowMediaState
        {
            WindowMode = 0,
            FullWindow,
            FullScreen
        };
        private WindowMediaState windowState;
        // WindowMode define the way the Media is displayed: Window, Full Window, Full Screen mode
        private WindowMediaState WindowState {
            get { return windowState; }
            set
            {
                if(windowState != value)
                {
                    windowState = value;
                    LogMessage("Media in " + windowState.ToString() + " state");
                }
            }
        }
        // attribute used to register Smooth Streaming component
        private Windows.Media.MediaExtensionManager extension = null;
        // attribute used to play HLS and DASH component
        private Windows.Media.Streaming.Adaptive.AdaptiveMediaSource adaptiveMediaSource = null; //ams represents the AdaptiveMedaSource used throughout this sample
        // attribute used to register Smooth Streaming component
        private Microsoft.Media.AdaptiveStreaming.AdaptiveSourceManager smoothStreamingManager = null;
        // PlaybackItem used for the Subtitle
        Windows.Media.Playback.MediaPlaybackItem playbackItem = null;
        // Subtitle Dictionary
        Dictionary<Windows.Media.Core.TimedTextSource, Uri> timedTextSourceMap;
        // Current Title if any
        private string CurrentTitle;
        // Url of the current playing media 
        private string CurrentMediaUrl;
        // Url of the poster of the current playing media 
        private string CurrentPosterUrl;
        // Duration for the current playing media 
        private TimeSpan CurrentDuration;
        // Start up position of the current playing media 
        private TimeSpan CurrentStartPosition;
        // flag to force LiveCurrentStartPosition (Smooth Streaming only) 
        private bool SetLiveCurrentStartPosition;
        // flag to log Live Buffer 
        private bool LogLiveBuffer;
        // flag to log Live Buffer information
        private bool LogDownload;
        // flag to log Live Buffer information
        private bool LogSubtitle;
        // Time when the application is starting to play a picture 
        private DateTime StartPictureTime;

        // Constant Keys used to store parameter in the isolate storage
      //  private const string keyMediaDataPath = "MediaDataPath";
      //  private const string keyMediaDataIndex = "MediaDataIndex";
      //  private const string keyMediaUri = "MediaUri";

        #endregion

        #region Initialization
        /// <summary>
        /// PlayerPage constructor 
        /// </summary>
        public PlayerPage()
        {
            this.InitializeComponent();
            LogMessage("Application PlayerPage Initialized");
        }
        // localPath is used to store the path of the content to be played
        string localPath = string.Empty;

        public async System.Threading.Tasks.Task<bool> LoadingNewPlaylist(string path)
        {
            try
            {
                //   mediaPlayer.Stop();
                mediaPlayer.Source = null;
            }
            catch (Exception)
            {

            }
            if (await LoadingData(path) == false)
                await LoadingData(string.Empty);
            //Update control and play first video
            if (ViewModels.StaticSettingsViewModel.AutoStart)
                PlayCurrentUrl();
            // Update PlaylistList

            Models.PlayList playlist = await Models.PlayList.GetNewPlaylist(path);
            if ((playlist != null)&&(playlist.Count>0))
            {
                if (ViewModelLocator != null)
                {
                    var p = ViewModelLocator.Settings.PlayListList.FirstOrDefault(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Name, playlist.Name, StringComparison.OrdinalIgnoreCase));
                    if (p == null)
                    {
                        ObservableCollection<Models.PlayList> PlayListList = ViewModelLocator.Settings.PlayListList;
                        PlayListList.Add(playlist);
                        ViewModelLocator.Settings.PlayListList = PlayListList;
                    }
                }
            }
            return true;
        }
        public async System.Threading.Tasks.Task<bool> LoadingNewContent(string path)
        {
            // If the playlist is loaded the application can directly 
            // play the content
            mediaUri.Text = "file://" + path;
            if (ViewModels.StaticSettingsViewModel.AutoStart == true)
            {
                SetAutoStartWindowState();
                await StartPlay(mediaUri.Text, mediaUri.Text, null, 0, 0);
            }
            return true;
        }
        public bool SetProtocolArgs(Uri uri)
        {
            LogMessage("Receive URI request for : " + uri.ToString());
            return true;
        }
        public async System.Threading.Tasks.Task<bool> SetPath(string path)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(path))
            {

                if (IsDataLoaded())
                {
                    if (path.EndsWith(".tma", StringComparison.OrdinalIgnoreCase))
                    {
                        LogMessage("Loading new playlist: " + path);
                        result = await LoadingNewPlaylist(path);

                    }
                    else
                    {
                        LogMessage("Loading new content: " + path);
                        result = await LoadingNewContent(path);
                    }
                }
                else
                {
                    // the content or the playlist will be loaded later on.
                    localPath =  path;
                    result = true;
                }
                UpdateControls();
            }
            return result;
        }
        Windows.UI.Color titleBarColor = Windows.UI.Colors.Transparent; 
        void ShowTitleBar(bool bShow)
        {
            // Refresh the pages with the new Color                         
            AudioVideoPlayer.Shell.Current.UpdateTitleBarAndColor(bShow);
           /* 
            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            Windows.ApplicationModel.Core.CoreApplicationViewTitleBar coreTitleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
            // Save titleBarColor
            if (titleBarColor == Windows.UI.Colors.Transparent)
                titleBarColor = formattableTitleBar.ButtonBackgroundColor?? Windows.UI.Colors.Transparent;
            if (bShow == true)
            {
                formattableTitleBar.ButtonBackgroundColor = titleBarColor;
                formattableTitleBar.ButtonHoverBackgroundColor = titleBarColor;
                formattableTitleBar.ButtonInactiveBackgroundColor = titleBarColor;
                coreTitleBar.ExtendViewIntoTitleBar = false;
            }
            else
            {
                formattableTitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                formattableTitleBar.ButtonHoverBackgroundColor = Windows.UI.Colors.Transparent;
                formattableTitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
                coreTitleBar.ExtendViewIntoTitleBar = true;
            }
            */
        }
        void SetAutoStartWindowState()
        {
            int state = (int)ViewModels.StaticSettingsViewModel.WindowState;
            if (state == 0)
                WindowState = WindowMediaState.WindowMode;
            else if ((state == 1) && (ViewModels.StaticSettingsViewModel.AutoStart == true))
                WindowState = WindowMediaState.FullWindow;
            else if ((state == 2) && (ViewModels.StaticSettingsViewModel.AutoStart == true))
                WindowState = WindowMediaState.FullScreen;
            else
                WindowState = WindowMediaState.WindowMode;

            SetWindowMode(WindowState);
        }
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
            LogMessage("PlayerPage OnNavigatedTo");
            var protocolForResultsArgs = e.Parameter as ProtocolForResultsActivatedEventArgs;
            if (protocolForResultsArgs != null)
            {
                var op = protocolForResultsArgs.ProtocolForResultsOperation;
                var caller = protocolForResultsArgs.CallerPackageFamilyName;
                if (string.Equals(caller, Information.SystemInformation.PackageFamilyName))

                {
                    if((protocolForResultsArgs.Data.Keys.Count == 1)&&
                        (protocolForResultsArgs.Data.ContainsKey("message")))
                    {
                        ValueSet result = new ValueSet();
                        result["result"] = "OK";
                        op.ReportCompleted(result);
                        return;
                    }
                }
            }
            var protocolArgs = e.Parameter as ProtocolActivatedEventArgs;
            if (protocolArgs != null)
            {
                var caller = protocolArgs.CallerPackageFamilyName;
                if (string.Equals(caller, Information.SystemInformation.PackageFamilyName))

                {
                    if ((protocolArgs.Data.Keys.Count == 1) &&
                        (protocolArgs.Data.ContainsKey("message")))
                    {
                        return;
                    }
                }
                return;
            }

            await ReadSettings();
                       
            if (e.NavigationMode != NavigationMode.New)
                RestoreState();

            // Register Suspend/Resume
            Application.Current.EnteredBackground += EnteredBackground;
            Application.Current.LeavingBackground += LeavingBackground;


            // Booking network for background task
            BookNetworkForBackground();

            // Bind player to element
            mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            mediaPlayerElement.SetMediaPlayer(mediaPlayer);
            mediaPlayer.CommandManager.IsEnabled = false;

            // Register UI components and events
           RegisterUI();


            // Register Smooth Streaming component
            RegisterSmoothStreaming();
            // Register PlayReady component
            RegisterPlayReady();
            // Register Device Watcher
            RegisterDeviceWatcher();
            // Register Companion
            await InitializeCompanion();
            // Register NetworkHelper
            RegisterNetworkHelper();


            // Load Data
            if (string.IsNullOrEmpty(MediaDataSource.MediaDataPath))
            {
                LogMessage("PlayerPage Loading Data...");
                await LoadingData(string.Empty);
            }

            // Take into account the argument if file activated
            var args = e.Parameter as Windows.ApplicationModel.Activation.IActivatedEventArgs;
            if (args != null)
            {
                if (args.Kind == Windows.ApplicationModel.Activation.ActivationKind.File)
                {
                    var fileArgs = args as Windows.ApplicationModel.Activation.FileActivatedEventArgs;
                    mediaUri.Text = "file://" + fileArgs.Files[0].Path;
                }
            }

            // If the path has been set before
            // the application will use this path 
            // to play the content or playlist
            if (!string.IsNullOrEmpty(localPath))
            {

                if (localPath.EndsWith(".tma", StringComparison.OrdinalIgnoreCase))
                {
                    LogMessage("Loading new playlist: " + localPath);
                    await LoadingNewPlaylist(localPath);
                }
                else
                {
                    LogMessage("Loading new content: " + localPath);
                    await LoadingNewContent(localPath);
                }
            }
            // Start to play the first asset
            if (ViewModels.StaticSettingsViewModel.AutoStart)
            {
                SetAutoStartWindowState();
                PlayCurrentUrl();
            }

            LogDownload = ViewModels.StaticSettingsViewModel.DownloadLogs;
            LogSubtitle = ViewModels.StaticSettingsViewModel.SubtitleLogs;
            LogLiveBuffer = ViewModels.StaticSettingsViewModel.LiveBufferLogs;

            // Update control and play first video
            UpdateControls();

            // Display OS, Device information
            LogMessage(Information.SystemInformation.GetString());

            // Message for localhost content 
            if (string.Equals(Information.SystemInformation.SystemFamily, "Windows.Desktop"))
            {
                LogMessage("If you need to play locally hosted content (url starting with \"http://localhost/\"), run the following command:\r\nCheckNetIsolation.exe LoopbackExempt -a -p=S-1-15-2-2982070501-967104153-2962845618-3522483597-428830490-3611180981-3161762328\r\n");
            }
            // Focus on PlayButton
            playButton.Focus(FocusState.Programmatic);

        }



        /// <summary>
        /// Method OnNavigatedFrom
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
           // LogMessage("PlayerPage OnNavigatedFrom");
            // Unregister Suspend/Resume
            Application.Current.EnteredBackground -= EnteredBackground;
            Application.Current.LeavingBackground -= LeavingBackground;

            // Register Smooth Streaming component
            UnregisterSmoothStreaming();
            // Register PlayReady component
            UnregisterPlayReady();
            // Unregister Device Watcher
            UnregisterDeviceWatcher();

            // Unregister UI components and events
            UnregisterUI();

            // Stop Companion reception
            UninitializeCompanion();

            // Unregister NetworkHelper
            UnregisterNetworkHelper();

            // Save State
            SaveState();
            //Save Settings
            SaveSettings();

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

            // Hide Systray on phone
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                // Hide fullWindow button
                fullwindowButton.Visibility = Visibility.Collapsed;
            }

            // Register Size Changed and Key Down
            Window.Current.SizeChanged += OnWindowResize;
            backgroundVideo.SizeChanged += BackgroundVideo_SizeChanged;
            this.KeyDown += OnKeyDown;
            this.DoubleTapped += doubleTapped;
            if (string.Equals(Information.SystemInformation.SystemFamily, "Windows.Xbox", StringComparison.OrdinalIgnoreCase))
                Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            // Create the popup used to display the pictures in fullscreen
            if (picturePopup == null)
                CreatePicturePopup();


            // Initialize mediaPlayer events
            mediaPlayer.MediaOpened += new TypedEventHandler<Windows.Media.Playback.MediaPlayer, object>(MediaElement_MediaOpened);
            mediaPlayer.MediaFailed += new TypedEventHandler<Windows.Media.Playback.MediaPlayer, Windows.Media.Playback.MediaPlayerFailedEventArgs>(MediaElement_MediaFailed);
            mediaPlayer.MediaEnded += new TypedEventHandler<Windows.Media.Playback.MediaPlayer, object>(MediaElement_MediaEnded);
            mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            if (Windows.Foundation.Metadata.ApiInformation.IsEventPresent("Windows.Media.Playback.MediaPlaybackSession", "SeekableRangesChanged"))
                mediaPlayer.PlaybackSession.SeekableRangesChanged += PlaybackSession_SeekableRangesChanged;
            mediaPlayerElement.DoubleTapped += doubleTapped;
            mediaPlayerElement.KeyDown += OnKeyDown;
            IsFullWindowToken = mediaPlayerElement.RegisterPropertyChangedCallback(MediaPlayerElement.IsFullWindowProperty, new DependencyPropertyChangedCallback(IsFullWindowChanged));

            // Combobox event
            comboStream.SelectionChanged += ComboStream_SelectionChanged;
            // Logs event to refresh the TextBox
            logs.TextChanged += Logs_TextChanged;


            // Initialize minBitrate and maxBitrate TextBox
            maxBitrate.TextChanged += BitrateTextChanged;
            minBitrate.TextChanged += BitrateTextChanged;


            // Start timer
            timer = new DispatcherTimer();
            if (timer != null)
            {
                timer.Interval = TimeSpan.FromMilliseconds(1000);
                timer.Tick += Timer_Tick;
                timer.Start();
                bResult = true;
            }
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

            // Register Size Changed and Key Down
            Window.Current.SizeChanged -= OnWindowResize;
            backgroundVideo.SizeChanged -= BackgroundVideo_SizeChanged;
            this.KeyDown -= OnKeyDown;
            this.DoubleTapped -= doubleTapped;
            if (string.Equals(Information.SystemInformation.SystemFamily, "Windows.Xbox", StringComparison.OrdinalIgnoreCase))
                Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;


            // Remove the popup used to display the pictures in fullscreen
            RemovePicturePopup();

            // Initialize MediaElement events
            if (mediaPlayer != null)
            {
                mediaPlayer.MediaOpened -= MediaElement_MediaOpened;
                mediaPlayer.MediaFailed -= MediaElement_MediaFailed;
                mediaPlayer.MediaEnded -= MediaElement_MediaEnded;
                mediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            }
            if (Windows.Foundation.Metadata.ApiInformation.IsEventPresent("Windows.Media.Playback.MediaPlaybackSession", "SeekableRangesChanged"))
                mediaPlayer.PlaybackSession.SeekableRangesChanged -= PlaybackSession_SeekableRangesChanged;
            mediaPlayerElement.DoubleTapped -= doubleTapped;
            mediaPlayerElement.KeyDown -= OnKeyDown;
            mediaPlayerElement.UnregisterPropertyChangedCallback(MediaElement.IsFullWindowProperty, IsFullWindowToken);




            // Combobox event
            comboStream.SelectionChanged -= ComboStream_SelectionChanged;
            // Logs event to refresh the TextBox
            logs.TextChanged -= Logs_TextChanged;

            // Initialize minBitrate and maxBitrate TextBox
            maxBitrate.TextChanged -= BitrateTextChanged;
            minBitrate.TextChanged -= BitrateTextChanged;

            // Stop timer
            if (timer != null)
            {
                timer.Tick -= Timer_Tick;
                timer.Stop();
                bResult = true;
            }
            return bResult;
        }
        // MediaDataGroup used to load the playlist
        MediaDataGroup audio_video = null;
        /// <summary>
        /// Method LoadingData which loads the JSON playlist file
        /// </summary>
        bool IsDataLoaded()
        {
            if ((audio_video != null) && (audio_video.Items.Count > 0))
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
                audio_video = await MediaDataSource.GetGroupAsync(path, "audio_video_picture");
                if ((audio_video != null) && (audio_video.Items.Count > 0))
                {
                    LogMessage("Loading playlist successful with " + audio_video.Items.Count.ToString() + " items");
                    TestTitle.Text = audio_video.Title;
                    try
                    {
                        TestLogo.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(audio_video.ImagePath));
                    }
                    catch
                    {

                    }
                    this.defaultViewModel = audio_video.Items;
                    comboStream.DataContext = this.DefaultViewModel;
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

        // Media Element FullWindow token
        private long IsFullWindowToken;

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
        private void PlaybackSession_SeekableRangesChanged(Windows.Media.Playback.MediaPlaybackSession sender, object args)
        {
            if (LogLiveBuffer)
            {
                var ranges = sender.GetSeekableRanges();
                if ((ranges != null) && (adaptiveMediaSource != null))
                {
                    foreach (var time in ranges)
                    {
                        var times = adaptiveMediaSource.GetCorrelatedTimes();
                        if (times != null)
                            LogMessage("Video Buffer available from StartTime: " + time.Start.ToString() + " till EndTime: " + time.End.ToString() + " Current Position: " + times.Position.ToString() + " ProgramDateTime: " + times.ProgramDateTime.ToString());

                        try
                        {
                            // Create our timeline properties object 
                            var timelineProperties = new Windows.Media.SystemMediaTransportControlsTimelineProperties();

                            // Fill in the data, using the media elements properties 
                            timelineProperties.StartTime = time.Start;
                            timelineProperties.MinSeekTime = time.Start;
                            timelineProperties.Position = mediaPlayer.PlaybackSession.Position;
                            timelineProperties.MaxSeekTime = time.End;
                            timelineProperties.EndTime = time.End;

                            // Update the System Media transport Controls 
                            SystemControls.UpdateTimelineProperties(timelineProperties);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Exception while updating the timeline: " + ex.Message);

                        }
                    }
                }
            }

        }

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
            if((mediaPlayer.PlaybackSession!=null) && (mediaPlayer.PlaybackSession.CanSeek) && (SetLiveCurrentStartPosition == true))
            {
                mediaPlayer.PlaybackSession.Position = CurrentStartPosition;
            }
            await UpdateControlsDisplayUpdater(CurrentTitle, CurrentMediaUrl, CurrentPosterUrl);
            UpdateControlTimeline();
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
            CurrentMediaUrl = string.Empty;
            CurrentPosterUrl = string.Empty;
            ReleaseDisplay();
            UpdateControls();
        }
        /// <summary>
        /// This method Register the Smooth Streaming component .
        /// </summary>
        public bool RegisterSmoothStreaming()
        {
            bool bResult = false;
            // Smooth Streaming initialization
            // Init SMOOTH Manager
            if(smoothStreamingManager != null)
            {
                smoothStreamingManager.ManifestReadyEvent -= SmoothStreamingManager_ManifestReadyEvent;
                smoothStreamingManager.AdaptiveSourceStatusUpdatedEvent -= SmoothStreamingManager_AdaptiveSourceStatusUpdatedEvent;
                smoothStreamingManager = null;
            }
            smoothStreamingManager = Microsoft.Media.AdaptiveStreaming.AdaptiveSourceManager.GetDefault() as Microsoft.Media.AdaptiveStreaming.AdaptiveSourceManager;
            extension = new Windows.Media.MediaExtensionManager();
            if ((smoothStreamingManager != null) &&
                (extension != null))
            {

                if (smoothStreamingManager.AdaptiveSources != null)
                {
                    if(smoothStreamingManager.AdaptiveSources.Count >0)
                    {
                        LogMessage("Bug ");
                    }
                }
                PropertySet ssps = new PropertySet();
                ssps["{A5CE1DE8-1D00-427B-ACEF-FB9A3C93DE2D}"] = smoothStreamingManager;


                extension.RegisterByteStreamHandler("Microsoft.Media.AdaptiveStreaming.SmoothByteStreamHandler", ".ism", "text/xml", ssps);
                extension.RegisterByteStreamHandler("Microsoft.Media.AdaptiveStreaming.SmoothByteStreamHandler", ".ism", "application/vnd.ms-sstr+xml", ssps);
                extension.RegisterByteStreamHandler("Microsoft.Media.AdaptiveStreaming.SmoothByteStreamHandler", ".isml", "text/xml", ssps);
                extension.RegisterByteStreamHandler("Microsoft.Media.AdaptiveStreaming.SmoothByteStreamHandler", ".isml", "application/vnd.ms-sstr+xml", ssps);


                extension.RegisterSchemeHandler("Microsoft.Media.AdaptiveStreaming.SmoothSchemeHandler", "ms-sstr:", ssps);
                extension.RegisterSchemeHandler("Microsoft.Media.AdaptiveStreaming.SmoothSchemeHandler", "ms-sstrs:", ssps);
                smoothStreamingManager.SetDownloaderPlugin(this);
                smoothStreamingManager.ManifestReadyEvent += SmoothStreamingManager_ManifestReadyEvent;
                smoothStreamingManager.AdaptiveSourceStatusUpdatedEvent += SmoothStreamingManager_AdaptiveSourceStatusUpdatedEvent;
                bResult = true;
            }
            return bResult;
        }
        /// <summary>
        /// This method Unregister the Smooth Streaming component .
        /// </summary>
        public bool UnregisterSmoothStreaming()
        {
            bool bResult = false;
            if (smoothStreamingManager != null)
            {
                smoothStreamingManager.ManifestReadyEvent -= SmoothStreamingManager_ManifestReadyEvent;
                smoothStreamingManager.AdaptiveSourceStatusUpdatedEvent -= SmoothStreamingManager_AdaptiveSourceStatusUpdatedEvent;
                smoothStreamingManager = null;
            }
            return bResult;
        }

        /// <summary>
        /// This method Register the PlayReady component .
        /// </summary>
        public bool RegisterPlayReady()
        {
            bool bResult = false;
            // PlayReady
            // Init PlayReady Protection Manager
            if(protectionManager!=null)
            {
                protectionManager.ComponentLoadFailed -= ProtectionManager_ComponentLoadFailed;
                protectionManager.ServiceRequested -= ProtectionManager_ServiceRequested;
                protectionManager = null;
            }
            protectionManager = new Windows.Media.Protection.MediaProtectionManager();
            if (protectionManager != null)
            {
                Windows.Foundation.Collections.PropertySet cpSystems = new Windows.Foundation.Collections.PropertySet();
                //Indicate to the MF pipeline to use PlayReady's TrustedInput
                cpSystems.Add("{F4637010-03C3-42CD-B932-B48ADF3A6A54}", "Windows.Media.Protection.PlayReady.PlayReadyWinRTTrustedInput");
                protectionManager.Properties.Add("Windows.Media.Protection.MediaProtectionSystemIdMapping", cpSystems);
                //Use by the media stream source about how to create ITA InitData.
                //See here for more detai: https://msdn.microsoft.com/en-us/library/windows/desktop/aa376846%28v=vs.85%29.aspx
                protectionManager.Properties.Add("Windows.Media.Protection.MediaProtectionSystemId", "{F4637010-03C3-42CD-B932-B48ADF3A6A54}");
                // Setup the container GUID that's in the PPSH box
                protectionManager.Properties.Add("Windows.Media.Protection.MediaProtectionContainerGuid", "{9A04F079-9840-4286-AB92-E65BE0885F95}");

                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                // Check if the platform does support hardware DRM 
                LogMessage((IsHardwareDRMSupported() == true ? "Hardware DRM is supported on this platform" : "Hardware DRM is not supported on this platform"));

                // Associate the MediaPlayer with the protection manager                
                mediaPlayer.ProtectionManager = protectionManager;
                mediaPlayer.ProtectionManager.ComponentLoadFailed += ProtectionManager_ComponentLoadFailed;
                mediaPlayer.ProtectionManager.ServiceRequested += ProtectionManager_ServiceRequested;
                bResult = true;
            }
            return bResult;
        }
        /// <summary>
        /// This method Unregister the PlayReady component .
        /// </summary>
        public bool UnregisterPlayReady()
        {
            bool bResult = false;
            if (protectionManager != null)
            {
                protectionManager.ComponentLoadFailed -= ProtectionManager_ComponentLoadFailed;
                protectionManager.ServiceRequested -= ProtectionManager_ServiceRequested;
                protectionManager = null;
                bResult = true;
            }
            return bResult;
        }

        /// <summary>
        /// This method is called every second.
        /// </summary>
        private void Timer_Tick(object sender, object e)
        {
            if (CurrentDuration != TimeSpan.Zero)
            {
                if (!IsPicture(CurrentMediaUrl))
                {
                    TimeSpan t = mediaPlayer.PlaybackSession.Position - CurrentStartPosition;
                    if (t > CurrentDuration)
                    {
                        if (ViewModels.StaticSettingsViewModel.AutoStart)
                        {
                            LogMessage("Skipping to next Media on timer tick...");
                            if (ViewModels.StaticSettingsViewModel.ContentLoop)
                                Loop_Click(null, null);
                            else
                                Plus_Click(null, null);
                        }
                    }
                }
                else
                {
                    TimeSpan t = DateTime.Now - StartPictureTime;
                    if (t > CurrentDuration)
                    {
                        if (ViewModels.StaticSettingsViewModel.AutoStart)
                        {
                            LogMessage("Skipping to next Media on timer tick...");
                            if (ViewModels.StaticSettingsViewModel.ContentLoop)
                                Loop_Click(null, null);
                            else
                                Plus_Click(null, null);
                        }
                    }
                }
            }
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
                playButton.Focus(FocusState.Programmatic);
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
            SaveSettings();

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
        /// This method is called when the content of the LiveOffset changed  
        /// </summary>
        void LiveOffsetTextChanged(object sender, TextChangedEventArgs e)
        {
                TextBox tb = sender as TextBox;
            if ((tb != null)&& (tb == liveOffset))
            {
                uint n;
                if (!uint.TryParse(tb.Text, out n))
                {
                    tb.Text = ViewModels.StaticSettingsViewModel.LiveOffset.ToString();
                }
                else
                {
                    ViewModels.StaticSettingsViewModel.LiveOffset = (int) n;
                }
            }
        }
        /// <summary>
        /// This method is called when the content of the minBitrate and maxBitrate TextBox changed  
        /// </summary>
        void BitrateTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                uint n;
                if (!uint.TryParse(tb.Text, out n))
                {
                    if (tb == minBitrate)
                        tb.Text = ViewModels.StaticSettingsViewModel.MinBitrate.ToString();
                    if (tb == maxBitrate)
                        tb.Text = ViewModels.StaticSettingsViewModel.MaxBitrate.ToString();
                }
                else
                {

                    if (tb == minBitrate)
                    {
                        if (CheckBitrates(tb.Text, maxBitrate.Text))
                            ViewModels.StaticSettingsViewModel.MinBitrate = (int)n;
                        else
                            tb.Text = ViewModels.StaticSettingsViewModel.MinBitrate.ToString();
                    }
                    if (tb == maxBitrate)
                    {
                        if (CheckBitrates(minBitrate.Text, tb.Text))
                            ViewModels.StaticSettingsViewModel.MaxBitrate = (int)n;
                        else
                            tb.Text = ViewModels.StaticSettingsViewModel.MaxBitrate.ToString();
                    }
                }
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

                         fullscreenButton.IsEnabled = false;
                         fullwindowButton.IsEnabled = false;

                         comboStream.IsEnabled = false;
                         mediaUri.IsEnabled = false;
                         minBitrate.IsEnabled = false;
                         maxBitrate.IsEnabled = false;
                         liveOffset.IsEnabled = false;
                     }
                     else
                     {
                         {
                             comboStream.IsEnabled = true;
                             mediaUri.IsEnabled = true;
                             minBitrate.IsEnabled = true;
                             maxBitrate.IsEnabled = true;
                             liveOffset.IsEnabled = true;

                             if ((comboStream.Items.Count > 0) || (!string.IsNullOrEmpty(mediaUri.Text)))
                             {
                                 playButton.IsEnabled = true;


                                 minusButton.IsEnabled = true;
                                 plusButton.IsEnabled = true;
                                 muteButton.IsEnabled = true;
                                 volumeDownButton.IsEnabled = true;
                                 volumeUpButton.IsEnabled = true;

                                 playPauseButton.IsEnabled = false;
                                 pausePlayButton.IsEnabled = false;
                                 stopButton.IsEnabled = false;


                                 if (IsPicture(CurrentMediaUrl))
                                 {
                                     fullscreenButton.IsEnabled = true;
                                     fullwindowButton.IsEnabled = true;
                                 }
                                 else
                                 {
                                     if (mediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Opening)
                                     {
                                         fullscreenButton.IsEnabled = true;
                                         fullwindowButton.IsEnabled = true;
                                     }
                                     else if (mediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
                                     {
                                         //if (string.Equals(mediaUri.Text, CurrentMediaUrl))
                                         //{
                                         playPauseButton.IsEnabled = false;
                                         pausePlayButton.IsEnabled = true;
                                         stopButton.IsEnabled = true;
                                         //}
                                     }
                                     else if (mediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused)
                                     {
                                         playPauseButton.IsEnabled = true;
                                         stopButton.IsEnabled = true;
                                     }
                                     else if (mediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.None)
                                     {
                                         //   mediaPlayerElement.AreTransportControlsEnabled = false;
                                         fullscreenButton.IsEnabled = false;
                                         fullwindowButton.IsEnabled = false;
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
                 });
        }

        /// <summary>
        /// Play method which plays the video with the MediaElement from position 0
        /// </summary>
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PlayCurrentUrl();
            }
            catch (Exception ex)
            {
                LogMessage("Failed to play: " + mediaUri.Text + " Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Stop method which stops the video currently played by the MediaElement
        /// </summary>
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((!string.IsNullOrEmpty(CurrentMediaUrl)) &&
                    (!string.IsNullOrEmpty(mediaUri.Text)) &&
                    (string.Equals(mediaUri.Text, CurrentMediaUrl)))
                {
                    LogMessage("Stop " + CurrentMediaUrl.ToString());
                    //mediaPlayer.Stop();
                    mediaPlayer.Source = null;
                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to stop: " + mediaUri.Text + " Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Select an Item in the Combo box
        /// </summary>
        private void select_Click(object sender, RoutedEventArgs e, int newIndex)
        {
            try
            {
                if ((newIndex >= 0) && (newIndex < comboStream.Items.Count))
                {
                    int Index = comboStream.SelectedIndex;
                    comboStream.SelectedIndex = newIndex;
                    if (Index != newIndex)
                    {
                        MediaItem ms = comboStream.SelectedItem as MediaItem;
                        mediaUri.Text = ms.Content;
                        PlayCurrentUrl();
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to to play: " + mediaUri.Text + " Exception: " + ex.Message);
            }
        }


        /// <summary>
        /// Play method which plays the video currently paused by the MediaElement
        /// </summary>
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((!string.IsNullOrEmpty(CurrentMediaUrl)) &&
                    (!string.IsNullOrEmpty(mediaUri.Text)) &&
                    (string.Equals(mediaUri.Text, CurrentMediaUrl)))
                {
                    LogMessage("Play " + CurrentMediaUrl.ToString());
                    mediaPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to play: " + mediaUri.Text + " Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Pause method which pauses the video currently played by the MediaElement
        /// </summary>
        private void PausePlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((!string.IsNullOrEmpty(CurrentMediaUrl)) &&
                    (!string.IsNullOrEmpty(mediaUri.Text)) &&
                    (string.Equals(mediaUri.Text, CurrentMediaUrl)))
                {
                    LogMessage("Play " + CurrentMediaUrl.ToString());
                    mediaPlayer.Pause();
                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to play: " + mediaUri.Text + " Exception: " + ex.Message);
            }
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
            if(file!=null)
            {
                string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);
                LogMessage("Stopping MediaElement before Loading playlist");
                try
                {
                 //   mediaPlayer.Stop();
                    mediaPlayer.Source = null;
                }
                catch (Exception)
                {

                }
                if (await LoadingData(file.Path) == false)
                    await LoadingData(string.Empty);
                //Update control and play first video
                if (ViewModels.StaticSettingsViewModel.AutoStart)
                    PlayCurrentUrl();
                UpdateControls();
            }
        }

        /// <summary>
        /// Loop method 
        /// </summary>
        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int Index = comboStream.SelectedIndex;
                if (Index >= 0)
                {
                    comboStream.SelectedIndex = Index;
                    MediaItem ms = comboStream.SelectedItem as MediaItem;
                    if (ms != null)
                    {
                        mediaUri.Text = ms.Content;
                        PlayReadyLicenseUrl = ms.PlayReadyUrl;
                        httpHeadersString = ms.HttpHeaders;
                        httpHeaders = GetHttpHeaders(httpHeadersString);
                        PlayReadyChallengeCustomData = ms.PlayReadyCustomData;
                    }
                    PlayCurrentUrl();
                }
                else
                    LogMessage("Error getting current playlis index");

            }
            catch (Exception ex)
            {
                LogMessage("Failed to to play: " + mediaUri.Text + " Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Channel up method 
        /// </summary>
        private void Plus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int Index = comboStream.SelectedIndex;
                if ((Index + 1) >= comboStream.Items.Count)
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
                    comboStream.SelectedIndex = Index;
                    MediaItem ms = comboStream.SelectedItem as MediaItem;
                    if (ms != null)
                    {
                        mediaUri.Text = ms.Content;
                        PlayReadyLicenseUrl = ms.PlayReadyUrl;
                        httpHeadersString = ms.HttpHeaders;
                        httpHeaders = GetHttpHeaders(httpHeadersString);
                        PlayReadyChallengeCustomData = ms.PlayReadyCustomData;
                    }
                    PlayCurrentUrl();
                }
                else
                    LogMessage("End of playlist loop");

            }
            catch (Exception ex)
            {
                LogMessage("Failed to to play: " + mediaUri.Text + " Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Channel down method 
        /// </summary>
        private void Minus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int Index = comboStream.SelectedIndex;

                if (Index - 1 >= 0)
                    --Index;
                else
                {
                    if (ViewModels.StaticSettingsViewModel.PlaylistLoop == true)
                        Index = comboStream.Items.Count - 1;
                    else
                        Index = -1; 
                }
                if (Index >= 0)
                {
                    comboStream.SelectedIndex = Index;
                    MediaItem ms = comboStream.SelectedItem as MediaItem;
                    if(ms!=null)
                    {
                        mediaUri.Text = ms.Content;
                        PlayReadyLicenseUrl = ms.PlayReadyUrl;
                        httpHeadersString = ms.HttpHeaders;
                        httpHeaders = GetHttpHeaders(httpHeadersString);
                        PlayReadyChallengeCustomData = ms.PlayReadyCustomData;
                    }
                    PlayCurrentUrl();
                }
                else
                    LogMessage("End of playlist loop");
            }
            catch (Exception ex)
            {
                LogMessage("Failed to to play: " + mediaUri.Text + " Exception: " + ex.Message);
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
        /// This method is called when the ComboStream selection changes 
        /// </summary>
        private void ComboStream_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboStream.SelectedItem != null)
            {
                MediaItem ms = comboStream.SelectedItem as MediaItem;
                mediaUri.Text = ms.Content;

                try
                {
                    streamLogo.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(ms.ImagePath));
                }
                catch
                {
                }

                // Update PlayReady URLs and Custom Data
                PlayReadyLicenseUrl = ms.PlayReadyUrl;
                PlayReadyChallengeCustomData = ms.PlayReadyCustomData;
                httpHeadersString = ms.HttpHeaders;
                httpHeaders = GetHttpHeaders(httpHeadersString);
                if (!string.Equals(CurrentMediaUrl, mediaUri.Text))
//                    mediaPlayer.Stop();
                mediaPlayer.Source = null;
                UpdateControls();
            }

        }
        #endregion

        #region WindowMode
        /// <summary>
        /// IsFullScreen
        /// This method return true if the application is running in Full Screen mode
        /// </summary>
        bool IsFullScreen()
        {
            var view = ApplicationView.GetForCurrentView();
            if (((picturePopup != null) && (picturePopup.IsOpen == true)) ||
                ((mediaPlayerElement.IsFullWindow) &&
                (mediaPlayerElement.AreTransportControlsEnabled == true)) )
                // XBox
                //||
                //((view != null) && (view.IsFullScreenMode)))
                return true;
            return false;
        }
        /// <summary>
        /// IsFullScreen
        /// This method return true if the application is running in Full Window mode
        /// </summary>
        bool IsFullWindow()
        {
            if ((mediaPlayerElement.IsFullWindow) &&
                (mediaPlayerElement.AreTransportControlsEnabled == false))
                return true;
            return false;
        }
        /// <summary>
        /// IsFullWindowChanged
        /// This method is called when MediaElement property IsFullWindow changed 
        /// </summary>
        private void IsFullWindowChanged(DependencyObject obj, DependencyProperty prop)
        {
            if (mediaPlayerElement.IsFullWindow)
            {
                if (mediaPlayerElement.AreTransportControlsEnabled == true)
                {
                    WindowState = WindowMediaState.FullScreen;
                    LogMessage("Media is in Full Screen mode");
                }
                else
                {
                    WindowState = WindowMediaState.FullWindow;
                    LogMessage("Media is in Full Window mode");
                }
            }
            else
            {
                mediaPlayerElement.AreTransportControlsEnabled = false;
                WindowState = WindowMediaState.WindowMode;
                SetWindowMode(WindowState);
                LogMessage("Media is in Window mode");
            }
        }


        /// <summary>
        /// Full screen method 
        /// </summary>
        private void Fullscreen_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Switch to fullscreen");
            SetWindowMode(WindowMediaState.FullScreen);
        }
        /// <summary>
        /// Create picturePopup
        /// ¨Popup used to display picture in fullscreen
        /// </summary>
        bool CreatePicturePopup()
        {
            picturePopup = new Popup()
            {
                Child = new StackPanel
                {
                    Background = new SolidColorBrush(Windows.UI.Colors.Black),
                }
            };
            if (picturePopup != null)
            {
                StackPanel c = picturePopup.Child as StackPanel;
                if (c != null)
                {
                    c.DoubleTapped += doubleTapped;
                    c.Children.Add(
                        new Image
                        {
                            Stretch = Stretch.Uniform,
                        }
                        );
                    return true;
                };
            }
            return false;
        }
        /// <summary>
        /// Remove picturePopup
        /// </summary>
        bool RemovePicturePopup()
        {
            if (picturePopup != null)
            {
                StackPanel c = picturePopup.Child as StackPanel;
                if (c != null)
                {
                    c.DoubleTapped -= doubleTapped;
                    c.Children.Clear();
                };
                picturePopup.Child = null;
                picturePopup = null;
            }
            return false;
        }
        /// <summary>
        /// Display picturePopup
        /// ¨Diplay or hide picturePopup
        /// </summary>
        bool DisplayPicturePopup(bool bDisplay)
        {
            if (picturePopup != null)
            {
                picturePopup.IsOpen = bDisplay;
                return true;
            }
            return false;
        }
        /// <summary>
        /// SetFullWindow for MediaElement, PictureElement and picturePopup
        /// </summary>
        bool SetWindowMode(WindowMediaState state)
        {
            if (state == WindowMediaState.FullWindow)
            {
                ShowTitleBar(false);
                //HidePointer();
                // if playing a picture or a video or audio with poster                
                if (pictureElement.Visibility == Visibility.Visible)
                {
                    DisplayPicturePopup(true);
                    ResizePicturePopup(Window.Current.Bounds.Width, Window.Current.Bounds.Height);
                }
                else
                {
                    if (mediaPlayerElement.AreTransportControlsEnabled == true)
                        mediaPlayerElement.AreTransportControlsEnabled = false;
                    if (mediaPlayerElement.IsFullWindow == false)
                        mediaPlayerElement.IsFullWindow = true;
                    DisplayPicturePopup(false);
                }
                WindowState = WindowMediaState.FullWindow;

            }
            else if (state == WindowMediaState.FullScreen)
            {

                ShowTitleBar(true);
                //HidePointer();
                // if playing a picture or a video or audio with poster                
                if (pictureElement.Visibility == Visibility.Visible)
                {
                    var view = ApplicationView.GetForCurrentView();
                    if (!view.IsFullScreenMode)
                        view.TryEnterFullScreenMode();
                    DisplayPicturePopup(true);
                    // Hack for XBOX One
                    if(string.Equals(Information.SystemInformation.SystemFamily ,"Windows.Xbox",StringComparison.OrdinalIgnoreCase))
                        ResizePicturePopup(Window.Current.Bounds.Width, Window.Current.Bounds.Height);
                }
                else
                {
                    if (mediaPlayerElement.AreTransportControlsEnabled == false)
                        mediaPlayerElement.AreTransportControlsEnabled = true;
                    if (mediaPlayerElement.IsFullWindow == false)
                        mediaPlayerElement.IsFullWindow = true;
                    DisplayPicturePopup(false);
                }
                WindowState = WindowMediaState.FullScreen;
            }
            else
            {

                ShowTitleBar(true);
                var view = ApplicationView.GetForCurrentView();
                if ((view.IsFullScreenMode) || (view.AdjacentToLeftDisplayEdge && view.AdjacentToRightDisplayEdge))
                    view.ExitFullScreenMode();
                DisplayPicturePopup(false);
                if (mediaPlayerElement.IsFullWindow == true)
                    mediaPlayerElement.IsFullWindow = false;
                if (mediaPlayerElement.AreTransportControlsEnabled == true)
                    mediaPlayerElement.AreTransportControlsEnabled = false;

                WindowState = WindowMediaState.WindowMode;
            }
            return true;
        }

        /// <summary>
        /// Full window method 
        /// </summary>
        private void Fullwindow_Click(object sender, RoutedEventArgs e)
        {

            LogMessage("Switch to fullwindow");
            SetWindowMode(WindowMediaState.FullWindow);
        }
        /// <summary>
        /// Set the source for the picture : windows and fullscreen 
        /// </summary>
        void SetPictureSource(Windows.UI.Xaml.Media.Imaging.BitmapImage b)
        {
            // Set picture source for windows element
            pictureElement.Source = b;
            // If doesn't exist create picture popup for fullscreen display
            if (picturePopup == null)
                CreatePicturePopup();
            // Set picture source for fullscreen element
            if (picturePopup != null)
            {
                StackPanel c = picturePopup.Child as StackPanel;
                if (c != null)
                {
                    Image im = c.Children.First() as Image;
                    if (im != null)
                    {
                        im.Source = b;
                    }
                }
            }
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
        /// <summary>
        /// BackgroundVideo picture Resize Event
        /// </summary>        
        void BackgroundVideo_SizeChanged(System.Object sender, SizeChangedEventArgs e)
        {
            SetPictureElementSize();
        }


        /// <summary>
        /// Resize the picturePopup
        /// </summary>
        void ResizePicturePopup(double Width, double Height)
        {
            if (picturePopup != null)
            {
                StackPanel c = picturePopup.Child as StackPanel;
                if (c != null)
                {
                    c.Width = Width;
                    c.Height = Height;

                    Image im = c.Children.First() as Image;
                    if (im != null)
                    {
                        im.Width = Width;
                        im.Height = Height;
                    }
                }
            }

        }
        /// <summary>
        /// Windows Resize Event
        /// </summary>
        async void OnWindowResize(object sender, WindowSizeChangedEventArgs e)
        {
            // Resize the picture popup accordingly 
            ResizePicturePopup(e.Size.Width, e.Size.Height);
            // Update Controls
            UpdateControls();
            // Display Log Message
            await DisplayLogMessage();
        }

        /// <summary>
        /// KeyDown event XBOX One only
        /// </summary>
        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.GamepadA)
            {
                if (IsFullScreen())
                {
                    SetWindowMode(WindowMediaState.WindowMode);
                    fullscreenButton.Focus(FocusState.Programmatic);
                }
                else if (IsFullWindow())
                {
                    SetWindowMode(WindowMediaState.WindowMode);
                    fullwindowButton.Focus(FocusState.Programmatic);
                }
            }
        }
        /// <summary>
        /// KeyDown event to exit full screen mode
        /// </summary>
        void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                SetWindowMode(WindowMediaState.WindowMode);
            }
        }

        /// <summary>
        /// Method called when the , picturePopup stackpanel or page  is double Tapped
        /// </summary>
        private void doubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (IsFullScreen() || IsFullWindow())
                SetWindowMode(WindowMediaState.WindowMode);
        }

        #endregion

        #region Media
        // String which contains the http Headers
        private string httpHeadersString;
        // Dictionary which contains the http headers for the http request
        Dictionary<string, string> httpHeaders;
        long GetVersionNumber(int Index, string version)
        {
            long result = 0;
            char[] sep = { '.' };
            string[] res = version.Split(sep);
            if ((res != null) && (res.Count() == 4))
            {
                long.TryParse(res[Index], out result);
            }
            return result;
        }
        bool IsLocalhostConnection(string url)
        {
            if(!string.IsNullOrEmpty(url))
            {
                if (url.ToLower().StartsWith("http://localhost/") ||
                    url.ToLower().StartsWith("https://localhost/"))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// This method indicates whether the OS is at least RS4 which support natively Smooth Streaming
        /// </summary>
        bool IsRS4()
        {
            if ((GetVersionNumber(0, Information.SystemInformation.SystemVersion) * 281474976710656 +
                GetVersionNumber(1, Information.SystemInformation.SystemVersion) * 4294967296 +
                GetVersionNumber(2, Information.SystemInformation.SystemVersion) * 65536 +
                GetVersionNumber(3, Information.SystemInformation.SystemVersion)) >= (10 * 281474976710656 + 17134 * 65536))
                return true;
            return false;
        }
        /// <summary>
        /// This method return a collection of http Headers
        /// </summary>
        private Dictionary<string, string> GetHttpHeaders(string httpHeadersString)
        {            
            Dictionary<string, string> headers = null;
            if (!string.IsNullOrEmpty(httpHeadersString))
            {
                string[] keys = { ",","{","}" };
                string[] headerArray = httpHeadersString.Split(keys, StringSplitOptions.RemoveEmptyEntries);

                foreach (var h in headerArray)
                {
                    string[] hKeys = { ":" };
                    string[] res = h.Split(hKeys, StringSplitOptions.RemoveEmptyEntries);
                    if (res.Count() == 2)
                    {
                        if (headers == null)
                            headers = new Dictionary<string, string>();
                        if(headers != null)
                            headers.Add(res[0].Trim(),res[1].Trim());
                    }
                }
            }
            return headers;
        }
        /// <summary>
        /// This method set the http headers 
        /// </summary>
        private void SetHttpHeaders(Dictionary<string, string> h, Windows.Web.Http.Headers.HttpRequestHeaderCollection hc)
        {
            try
            {
                if ((h != null) && (hc != null))
                {
                    foreach (var var in h)
                    {
                        // If not a Bearer add key, value
                        if ((!string.Equals(var.Key, "Authorization", StringComparison.OrdinalIgnoreCase)) || (!var.Value.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase)))
                        {
                            LogMessage("Adding in the Http Header key: " + var.Key + " Value: " + var.Value);
                            hc.TryAppendWithoutValidation(var.Key, var.Value);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                LogMessage("Exception while setting the http headers: "+ e.Message);
            }
        }
        /// <summary>
        /// This method set the http headers 
        /// </summary>
        private void SetHttpHeaders(Dictionary<string, string> h, Windows.Web.Http.Headers.HttpContentHeaderCollection hc)
        {
            try
            {
                if ((h != null) && (hc != null))
                {
                    foreach (var var in h)
                    {
                        // If not a Bearer add key, value
                        if ((!string.Equals(var.Key, "Authorization", StringComparison.OrdinalIgnoreCase)) || (!var.Value.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase)))
                        {
                            LogMessage("Adding in the Http Header key: " + var.Key + " Value: " + var.Value);
                            hc.TryAppendWithoutValidation(var.Key, var.Value);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogMessage("Exception while setting the http headers: " + e.Message);
            }
        }        /// <summary>
                 /// This method set the http headers 
                 /// </summary>
        private void SetHttpHeaders(IReadOnlyDictionary<string, string> hsource, Dictionary<string, string> hdest)
        {
            try
            {
                if ((hsource != null) && (hdest != null))
                {
                    foreach (var var in hsource)
                    {
                        LogMessage("Adding in the Http Header key: " + var.Key + " Value: " + var.Value);
                        if(hdest.ContainsKey(var.Key))
                            hdest[var.Key] = var.Value;
                        else
                            hdest.Add(var.Key, var.Value);
                    }
                }
            }
            catch (Exception e)
            {
                LogMessage("Exception while setting the http headers: " + e.Message);
            }
        }
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
        /// This method checks if the url is a redirect encrypted url of http url
        /// </summary>
        private bool IsRedirectEncryptedUri(string url)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(url))
            {
                if ((url.ToLower().StartsWith("redirects://")))
                {
                    result = true;
                }
            }
            return result;
        }
        /// <summary>
        /// This method checks if the url is a redirect url of http url
        /// </summary>
        private bool IsRedirectUri(string url)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(url))
            {
                if ((url.ToLower().StartsWith("redirect://")) )
                {
                    result = true;
                }
            }
            return result;
        }
        /// <summary>
        /// This method checks if the url is a SMOOTH, HLS or DASH url
        /// </summary>
        private bool IsAdaptiveStreaming(string url)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(url))
            {
                if (url.ToLower().Contains("/manifest") ||
                    url.ToLower().Contains(".m3u8") ||
                    url.ToLower().Contains(".mpd")
                    )
                {
                    result = true;
                }
            }
            return result;
        }
        /// <summary>
        /// This method checks if the url is a smooth streaming url
        /// </summary>
        private bool IsSmoothStreaming(string url)
        {
            bool result = false;
            if (!string.IsNullOrEmpty(url))
            {
                if(url.ToLower().EndsWith("/manifest"))
                {
                    result = true;
                }
            }
            return result;
        }
        /// <summary>
        /// This method prepare the MediaElement to play any content (video, audio, pictures): SMOOTH, DASH, HLS, MP4, WMV, MPEG2-TS, JPG, PNG,...
        /// </summary>
        private async void PlayCurrentUrl()
        {

            MediaItem item = comboStream.SelectedItem as MediaItem;
            if (item != null)
            {
                if(string.Equals(mediaUri.Text,item.Content)!=true)
                    await StartPlay(mediaUri.Text, mediaUri.Text, null, 0, 0);
                else
                    await StartPlay(item.Title, mediaUri.Text, item.PosterContent, item.Start, item.Duration);
                UpdateControls();
            }
            else
            {
                if (!string.IsNullOrEmpty(mediaUri.Text))
                {
                    await StartPlay(mediaUri.Text, mediaUri.Text, null, 0, 0);
                    UpdateControls();
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
        async System.Threading.Tasks.Task<Windows.Storage.StorageFile> CreateTempFile(Windows.Storage.StorageFolder folder, string filename, string extension)
        {
            Windows.Storage.StorageFile file = null;
            int index = 0;
            while ((file == null)&&(index<10))
            {
                try
                {
                    file = await folder.CreateFileAsync(filename + index.ToString() + extension, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while creating file: " + ex.Message);
                }
                index++;
            }
            return file;
        }
        async System.Threading.Tasks.Task<string> DownloadRemoteFile(Windows.Storage.StorageFolder folder, string sourceUrl)
        {
            string result = null;
            string extension = GetExtension(sourceUrl);
            string rootname = "poster";
            string filename = string.Empty;
            if (!string.IsNullOrEmpty(extension))
            {

                filename = rootname + "_" ;
                try
                {
                    
                    if (folder != null)
                    {
                        Windows.Storage.StorageFile file = await CreateTempFile(folder, filename, extension);
                        //Windows.Storage.StorageFile file = await folder.CreateFileAsync(filename + extension, Windows.Storage.CreationCollisionOption.ReplaceExisting);
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
                }
            }
            return result;
        }
        /// <summary>
        /// Return Music Suffix for filename
        /// </summary>
        private async System.Threading.Tasks.Task<string> GetMusicSuffix(Windows.Storage.StorageFile file)
        {
            string result = string.Empty;
            MusicProperties musicProperties = await file.Properties.GetMusicPropertiesAsync();
            if(musicProperties!=null)
            {
                if(string.IsNullOrEmpty(musicProperties.Album) &&
                    string.IsNullOrEmpty(musicProperties.Artist))
                {
                    char[] invalidPathChars = Path.GetInvalidPathChars();
                    string input = musicProperties.Artist + "_" + musicProperties.Album;
                    char a;
                    foreach (char c in result)
                    {

                        if (invalidPathChars.Contains(c))
                            a = '_';
                        else
                            a = c;

                        result += a;
                    }
                };
            }
            return result;
        }
        /// <summary>
        /// Return Video Suffix for filename
        /// </summary>
        private async System.Threading.Tasks.Task<string> GetVideoSuffix(Windows.Storage.StorageFile file)
        {
            string result = string.Empty;
            VideoProperties videoProperties = await file.Properties.GetVideoPropertiesAsync();
            if (videoProperties != null)
            {
                if (string.IsNullOrEmpty(videoProperties.Title) &&
                    string.IsNullOrEmpty(videoProperties.Publisher))
                {
                    char[] invalidPathChars = Path.GetInvalidPathChars();
                    string input = videoProperties.Publisher + "_" + videoProperties.Title;
                    char a;
                    foreach (char c in result)
                    {

                        if (invalidPathChars.Contains(c))
                            a = '_';
                        else
                            a = c;

                        result += a;
                    }
                };
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
                    string localPath = await DownloadRemoteFile(Windows.Storage.ApplicationData.Current.LocalFolder, poster);
                    if (!string.IsNullOrEmpty(localPath))
                        poster = "file://" + localPath;
                    else
                        return null;
                }
                if (IsMusic(poster) || IsVideo(poster))
                {
                    try
                    {
                        Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(poster);
                        if (file != null)
                        {
                            string suffix = string.Empty;

                            if (IsMusic(poster))
                                suffix = await GetMusicSuffix(file);
                            else
                                suffix = await GetVideoSuffix(file);
                            string existingPoster = string.Empty;
                            if(string.IsNullOrEmpty(suffix))
                            {
                                Windows.Storage.StorageFile tf = await Helpers.StorageHelper.GetFile(Windows.Storage.ApplicationData.Current.LocalFolder,
                                    "Thumb_" + suffix + ".jpg");
                                if (tf != null)
                                    existingPoster = "file://" + tf.Path;

                            }
                            if (string.IsNullOrEmpty(existingPoster))
                            {
                                // Thumbnail
                                using (var thumbnail = await file.GetThumbnailAsync((IsMusic(poster) ? Windows.Storage.FileProperties.ThumbnailMode.MusicView : Windows.Storage.FileProperties.ThumbnailMode.VideosView),192))
                                {
                                    if (thumbnail != null &&
                                        ((thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Image) || (thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Icon)))
                                    {
                                        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                                        if (localFolder != null)
                                        {
                                            string filename = string.Empty;
                                            if (string.IsNullOrEmpty(suffix))
                                                filename = "CurrentThumb.jpg";
                                            else
                                                filename = "Thumb_" + suffix + ".jpg";
                                            string filepath = await Helpers.MediaHelper.SaveThumbnailToFileAsync(localFolder, filename, thumbnail, true);
                                            if (!string.IsNullOrEmpty(filepath))
                                            {
                                                poster = "file://" + filepath;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                                poster = existingPoster;

                        }
                    }
                    catch (Exception e)
                    {
                        LogMessage("Exception while loading poster: " + poster + " - " + e.Message);
                    }
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
        bool UpdateControlTimeline()
        {
            bool result = false;
            try
            {
                // Create our timeline properties object 
                var timelineProperties = new Windows.Media.SystemMediaTransportControlsTimelineProperties();

                // Fill in the data, using the media elements properties 
                timelineProperties.StartTime = TimeSpan.FromSeconds(0);
                timelineProperties.MinSeekTime = TimeSpan.FromSeconds(0);
                timelineProperties.Position = mediaPlayer.PlaybackSession.Position;
                timelineProperties.MaxSeekTime = mediaPlayer.PlaybackSession.NaturalDuration;
                timelineProperties.EndTime = mediaPlayer.PlaybackSession.NaturalDuration;

                // Update the System Media transport Controls 
                SystemControls.UpdateTimelineProperties(timelineProperties);
                result = true;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while updating the timeline: " + ex.Message);

            }
            return result;
        }
        /// <summary>
        /// This method Update the SystemControls Display information
        /// </summary>
        private async System.Threading.Tasks.Task<bool> UpdateControlsDisplayUpdater(string title, string content, string poster)
        {
            LogMessage("Updating SystemControls");
            if ((comboStream.Items.Count > 1)&&(ViewModels.StaticSettingsViewModel.AutoStart))
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
            return true;
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
                // Comment the line below
                //if (!IsLocalFile(PosterUrl))
                //{
                    
                //    string localPath = await DownloadRemoteFile(Windows.Storage.ApplicationData.Current.LocalFolder, PosterUrl);
                //    if (!string.IsNullOrEmpty(localPath))
                //        PosterUrl = "file://" + localPath;
                //}

                if (IsLocalFile(PosterUrl))
                {
                    try
                    {
                        Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(PosterUrl);
                        if (file != null)
                        {
                           
                            // Thumbnail
                            using (var thumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.MusicView,192,ThumbnailOptions.None))
                            {
                                if (thumbnail != null && 
                                    (thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Image))
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
                                    //Try to read with AlbumArtReader provided the size if below 10MB
                                    var audioStream = await file.OpenReadAsync();
                                    if ((audioStream != null)&&(audioStream.Size<10000000))
                                    {
                                        var albumArtReader = new AlbumArt.AlbumArtReader();
                                        albumArtReader.Initialize(audioStream);
                                        var fileStream = await albumArtReader.GetAlbumArtAsync();
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
                // Display the PlayReady expiration date for this video (if protected)
                if (PlayReadyUrlKeyIdDictionary.ContainsKey(content))
                {
                    // Get the expiration date of PlayReady license
                    DateTime d = GetLicenseExpirationDate(PlayReadyUrlKeyIdDictionary[content]);
                    if (d != DateTime.MinValue)
                    {
                        LogMessage("Video: " + content + " is protected with PlayReady and the license Expiration Date is: " + d.ToString());
                    }
                }
                // The application does restore the default DRM configuration hardware DRM or software DRM
                // before playing the asset
                // if the content to play is based on VC1 codec and the hardware DRM is enabled,
                // the software DRM will be enabled on event Manifest Ready
                LogMessage("Restoring the DRM configuration: " + (IsHardwareDRMSupported() == true? "Hardware DRM": "Software DRM"));
                EnableSoftwareDRM(!IsHardwareDRMSupported());

                // Stop the current stream
                mediaPlayer.Source = null;
                mediaPlayerElement.PosterSource = null;
                mediaPlayer.AutoPlay = true;
                CurrentMediaUrl = string.Empty;
                CurrentPosterUrl = string.Empty;
                CurrentStartPosition = new TimeSpan(0);
                SetLiveCurrentStartPosition = false;
                CurrentDuration = new TimeSpan(0);
                StartPictureTime = DateTime.MinValue;
                if (IsPicture(content))
                    result = await SetPosterUrl(content);
                else if (IsPicture(poster))
                    result = await SetPosterUrl(poster);
                else if (IsMusic(poster))
                    result = await SetPosterUrl(poster);
                // if a picture will be displayed
                // display or not popup
                if (result == true)
                {
                    pictureElement.Visibility = Visibility.Visible;
                    mediaPlayerElement.Visibility = Visibility.Collapsed;
                    StartPictureTime = DateTime.Now;
                }
                else
                {
                    SetPictureSource(null);
                    pictureElement.Visibility = Visibility.Collapsed;
                    mediaPlayerElement.Visibility = Visibility.Visible;
                }
                // Audio or video
                if (!IsPicture(content))
                {
                    if (!IsMusic(content))
                    {
                        // fix for video 
                        pictureElement.Visibility = Visibility.Collapsed;
                        mediaPlayerElement.Visibility = Visibility.Visible;
                    }
                    if (IsMusic(content) && string.IsNullOrEmpty(poster))
                    {
                        // try to get poster from Audio file
                        bool res = await SetPosterUrl(content);
                        if (res == true)
                        {
                            pictureElement.Visibility = Visibility.Visible;
                            mediaPlayerElement.Visibility = Visibility.Collapsed;
                            poster = content;
                        }
                    }
                    result = await SetAudioVideoUrl(content);
                    if (result == true)
                    {

                        mediaPlayer.Play();
                    }

                }
                if (result == true)
                {
                    CurrentStartPosition = new TimeSpan(start * 10000);
                    CurrentDuration = new TimeSpan(duration * 10000);
                    CurrentMediaUrl = content;
                    CurrentPosterUrl = poster;
                    CurrentTitle = title;
                    SystemControls.IsEnabled = true;
                    if(IsPicture(content))
                        await UpdateControlsDisplayUpdater(CurrentTitle, CurrentMediaUrl, CurrentPosterUrl);
                    // Set Window Mode
                    SetWindowMode(WindowState);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogMessage("Exception Playing: " + ex.Message.ToString());
                CurrentMediaUrl = string.Empty;
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
        /// GetRemoteEncrytedContent
        /// Downloads the remote encrypted content from content uri asynchronously.
        /// </summary>
        /// <param name="content">Uri Specifies whether to force a new download and avoid cached results.</param>
        /// <param name="forceNewDownload">Specifies whether to force a new download and avoid cached results.</param>
        /// <returns>A byte array</returns>
        public async System.Threading.Tasks.Task<string> GetRemoteEncryptedContent(string content)
        {
            string result = string.Empty;
            Uri contentUri = null;
            string method = string.Empty;
            string appName = string.Empty;
            string code = string.Empty;
            string media = string.Empty;

            try
            {
                Uri uri = new Uri(content);
                IReadOnlyDictionary<string,string> parameters = ParseQueryString(uri);
                foreach( var value in parameters)
                {
                    if(value.Key=="method")
                        method = Uri.UnescapeDataString(value.Value);
                    if (value.Key == "appName")
                        appName = Uri.UnescapeDataString(value.Value);
                    if (value.Key == "code")
                        code = Uri.UnescapeDataString(value.Value);
                }
                int pos = content.IndexOf('?');
                if (pos > 0)
                {
                    content = content.Substring(0, pos);
                    pos = content.LastIndexOf('/');
                    if (pos > 0)
                    {
                        media = content.Substring(pos + 1);
                        string url = content.Substring(0, pos + 1);
                        contentUri = new Uri(url);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("Exception while creating uri for: " + content + " exception: " + ex.Message);

            }
            LogMessage("Get Remote content from: " + contentUri.ToString());

            var client = new Windows.Web.Http.HttpClient();
            try
            {

                long l = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                string currentTime = l.ToString();
                var key = media + '-' + code + '-' + appName + '-' + code + '-' + currentTime;

                var alg = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
                IBuffer buff = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
                var hashed = alg.HashData(buff);
                var authKey = CryptographicBuffer.EncodeToHexString(hashed) + '/' + currentTime;

                string paramstring = "appName=" + appName;
                paramstring += "&mediaId=" + media;
                paramstring += "&method=" + method;
                paramstring += "&authKey=" + authKey;
              
                SetHttpHeaders(httpHeaders, client.DefaultRequestHeaders);
                client.DefaultRequestHeaders.TryAppendWithoutValidation("Accept", "*/*");
                client.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Mozilla /5.0 (Windows NT 10.0; Win64; x64; MSAppHost/3.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36 Edge/14.14393");
                client.DefaultRequestHeaders.TryAppendWithoutValidation("Accept-Language", "en -US,en;q=0.8,fr-FR;q=0.5,fr;q=0.3");
                
                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(paramstring);
                httpContent.Headers.Remove("Content-type");
                httpContent.Headers.TryAppendWithoutValidation("Content-type", "application/x-www-form-urlencoded");
                Windows.Web.Http.HttpResponseMessage response = await client.PostAsync(contentUri, httpContent);

                response.EnsureSuccessStatusCode();
                result = await response.Content.ReadAsStringAsync();
                if (result.Length > 0)
                {
                    int pos = result.IndexOf("200");
                    if (pos > 0)
                    {
                        pos = result.IndexOf("message", pos);
                        if (pos > 0)
                        {
                            pos = result.IndexOf("http", pos);
                            int end = result.IndexOf("\"", pos);
                            result = result.Substring(pos, end - pos);
                            result = result.Replace("\\","");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + e.Message);
            }

            return result;
        }
        /// <summary>
        /// GetRemoteContent
        /// Downloads the remote content from content uri asynchronously.
        /// </summary>
        /// <param name="content">Uri Specifies whether to force a new download and avoid cached results.</param>
        /// <param name="forceNewDownload">Specifies whether to force a new download and avoid cached results.</param>
        /// <returns>A byte array</returns>
        public async System.Threading.Tasks.Task<string> GetRemoteContent(string content, bool forceNewDownload)
        {
            string result = string.Empty;
            Uri contentUri = null;
            try
            {
                contentUri = new Uri(content);
            }
            catch(Exception ex)
            {
                LogMessage("Exception while creating uri for: " + content  + " exception: " + ex.Message);

            }
            LogMessage("Get Remote content from: " + contentUri.ToString());

            var client = new Windows.Web.Http.HttpClient();
            try
            {
                if (forceNewDownload)
                {
                    string modifier = contentUri.AbsoluteUri.Contains("?") ? "&" : "?";
                    string newUriString = string.Concat(contentUri.AbsoluteUri, modifier, "ignore=", Guid.NewGuid());
                    contentUri = new Uri(newUriString);
                }
                SetHttpHeaders(httpHeaders, client.DefaultRequestHeaders);
                Windows.Web.Http.HttpResponseMessage response = await client.GetAsync(contentUri, Windows.Web.Http.HttpCompletionOption.ResponseContentRead);

                response.EnsureSuccessStatusCode();
                result = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + e.Message);
            }

            return result;
        }

        bool CreateTimeTextSources(Windows.Media.Core.MediaSource source)
        {
            bool result = false;
           
            if (timedTextSourceMap != null)
            {
                foreach(var val in timedTextSourceMap)
                {
                    val.Key.Resolved -= TimedTextSource_Resolved;                    
                }
                timedTextSourceMap = null;
            }
            timedTextSourceMap = new Dictionary<Windows.Media.Core.TimedTextSource, Uri>();
            if (timedTextSourceMap != null)
            {
                // Add Subtitles
                var timedTextSourceUri_En = new Uri("https://blobstoragebackup.blob.core.windows.net/caption/caption_en.srt");
                var timedTextSource_En = Windows.Media.Core.TimedTextSource.CreateFromUri(timedTextSourceUri_En);
                timedTextSourceMap[timedTextSource_En] = timedTextSourceUri_En;
                timedTextSource_En.Resolved += TimedTextSource_Resolved;

                var timedTextSourceUri_Pt = new Uri("https://blobstoragebackup.blob.core.windows.net/caption/caption_pt.srt");
                var timedTextSource_Pt = Windows.Media.Core.TimedTextSource.CreateFromUri(timedTextSourceUri_Pt);
                timedTextSourceMap[timedTextSource_Pt] = timedTextSourceUri_Pt;
                timedTextSource_Pt.Resolved += TimedTextSource_Resolved;

                // Add the TimedTextSource to the MediaSource
                source.ExternalTimedTextSources.Add(timedTextSource_En);
                source.ExternalTimedTextSources.Add(timedTextSource_Pt);
                result = true;
            }
            return result;
        }
    private void TimedTextSource_Resolved(Windows.Media.Core.TimedTextSource sender, Windows.Media.Core.TimedTextSourceResolveResultEventArgs args)
        {
            var timedTextSourceUri = timedTextSourceMap[sender];

            if (args.Error != null)
            {
                // Show that there was an error in your UI
                LogMessage("There was an error resolving track: " + timedTextSourceUri);
                return;
            }

            // Add a label for each resolved track
            var timedTextSourceUriString = timedTextSourceUri.AbsoluteUri;
            if (timedTextSourceUriString.Contains("_en"))
            {
                args.Tracks[0].Label = "English";
            }
            else if (timedTextSourceUriString.Contains("_pt"))
            {
                args.Tracks[0].Label = "Portuguese";
            }
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
                if (IsRedirectEncryptedUri(Content))
                {
                    string localuri = Content.Replace("redirects://", "");
                    string newContent = await GetRemoteEncryptedContent(localuri);
                    if (!string.IsNullOrEmpty(newContent) && Uri.IsWellFormedUriString(newContent, UriKind.Absolute))
                    {
                        Content = newContent;
                    }
                    else
                        return false;
                }
                if (IsRedirectUri(Content))
                {
                    string localuri = Content.Replace("redirect://", "");
                    string newContent  = await GetRemoteContent(localuri,true);
                    if (!string.IsNullOrEmpty(newContent) && Uri.IsWellFormedUriString(newContent, UriKind.Absolute))
                    {
                        Content = newContent;
                    }
                    else
                        return false;
                }
                if (IsLocalFile(Content))
                {
                    Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(Content);
                    if (file != null)
                    {
                        mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);
                        /*
                        var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                        if (fileStream != null)
                        {
                            mediaPlayer.SetStreamSource(fileStream);
                            mediaPlayer.SetFileSource(file);
                            return true;
                        }*/
                        return true;
                    }
                    else
                        LogMessage("Failed to load media file: " + Content);
                }
                else if (!IsAdaptiveStreaming(Content))
                {
                    if (httpHeaders != null)
                    {
                        try
                        {
                            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

                            Windows.Web.Http.HttpRequestMessage request = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, new Uri(Content));
                            SetHttpHeaders(httpHeaders, request.Headers);
                            Windows.Web.Http.HttpResponseMessage response = await httpClient.SendRequestAsync(request, Windows.Web.Http.HttpCompletionOption.ResponseHeadersRead);
                            if (response.IsSuccessStatusCode)
                            {
                                string contentType = "video/mp4";
                                if (response.Content.Headers.ContainsKey("Content-Type"))
                                {
                                    contentType = response.Content.Headers["Content-Type"];
                                }
                                var inputStream = await response.Content.ReadAsInputStreamAsync();

                                var memStream = new MemoryStream();
                                await inputStream.AsStreamForRead().CopyToAsync(memStream);

                                mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStream(memStream.AsRandomAccessStream(), contentType);
                            }
                            else
                                LogMessage("DownloadRequested for uri: " + Content.ToString() + " error: " + response.StatusCode.ToString());
                        }
                        catch (Exception e)
                        {
                            LogMessage("DownloadRequested for uri: " + Content.ToString() + " exception: " + e.Message);

                        }
                    }
                    else
                    {
                        mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(Content));
                    }
                    return true;
                }
                else
                {

                    // If SMOOTH stream
                    if (IsSmoothStreaming(Content)&&(IsRS4()==false))
                    {
                        //   string modifier = Content.Contains("?") ? "&" : "?";
                        //   string newUriString = string.Concat(Content, modifier, "ignore=", Guid.NewGuid());
                        Windows.Media.Core.MediaSource ms = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(Content));
                        mediaPlayer.Source = ms;
                        return true;
                    }
                    else
                    {
                        // If DASH, HLS or SMOOTH (OS version >= RS4) content
                        // Create the AdaptiveMediaSource
                        Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceCreationResult result = null;
                        if (httpHeaders != null)
                        {
                            try
                            {
                                var baseFilter = new HttpBaseProtocolFilter();
                                if (IsLocalhostConnection(Content))
                                    baseFilter.UseProxy = false;
                                var SmoothFilter = new Helpers.SmoothHttpFilter(baseFilter);
                                Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient(SmoothFilter);
                                SetHttpHeaders(httpHeaders, httpClient.DefaultRequestHeaders);
                                result = await Windows.Media.Streaming.Adaptive.AdaptiveMediaSource.CreateFromUriAsync(new Uri(Content), httpClient);
                            }
                            catch (Exception e)
                            {
                                LogMessage("Exception while downloading DASH or HLS manifest: " + e.Message);
                            }
                        }
                        else
                        {
                            var baseFilter = new HttpBaseProtocolFilter();
                            if (IsLocalhostConnection(Content))
                                baseFilter.UseProxy = false;
                            var SmoothFilter = new Helpers.SmoothHttpFilter(baseFilter);
                            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient(SmoothFilter);

                            result = await Windows.Media.Streaming.Adaptive.AdaptiveMediaSource.CreateFromUriAsync(new Uri(Content), httpClient);
                        }
                        if (result.Status == Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceCreationStatus.Success)
                        {
                            if (adaptiveMediaSource != null)
                            {
                                adaptiveMediaSource.DownloadBitrateChanged -= AdaptiveMediaSource_DownloadBitrateChanged;
                                adaptiveMediaSource.DownloadCompleted -= AdaptiveMediaSource_DownloadCompleted;
                                adaptiveMediaSource.DownloadFailed -= AdaptiveMediaSource_DownloadFailed;
                                adaptiveMediaSource.DownloadRequested -= AdaptiveMediaSource_DownloadRequested;
                                adaptiveMediaSource.PlaybackBitrateChanged -= AdaptiveMediaSource_PlaybackBitrateChanged;
                            }
                            adaptiveMediaSource = result.MediaSource;
                            adaptiveMediaSource.DownloadBitrateChanged += AdaptiveMediaSource_DownloadBitrateChanged;
                            adaptiveMediaSource.DownloadCompleted += AdaptiveMediaSource_DownloadCompleted;
                            adaptiveMediaSource.DownloadFailed += AdaptiveMediaSource_DownloadFailed;
                            adaptiveMediaSource.DownloadRequested += AdaptiveMediaSource_DownloadRequested;
                            adaptiveMediaSource.PlaybackBitrateChanged += AdaptiveMediaSource_PlaybackBitrateChanged;
                            

                            LogMessage("Available bitrates: ");
                            uint startupBitrate = 0;
                            uint MaxBitRate = (uint)ViewModels.StaticSettingsViewModel.MaxBitrate;
                            uint MinBitRate = (uint)ViewModels.StaticSettingsViewModel.MinBitrate;
                            foreach (var b in adaptiveMediaSource.AvailableBitrates)
                            {
                                LogMessage("bitrate: " + b.ToString() + " b/s ");
                                if ((startupBitrate == 0) &&
                                    (b >= MinBitRate) &&
                                    (b <= MaxBitRate))
                                    startupBitrate = b;
                            }
                            // Set bitrate range for HLS and DASH
                            if(startupBitrate>0)
                                adaptiveMediaSource.InitialBitrate = startupBitrate;
                            adaptiveMediaSource.DesiredMaxBitrate = MaxBitRate;
                            adaptiveMediaSource.DesiredMinBitrate = MinBitRate;

                            // Set Live Offset for Live Stream
                            uint lo = (uint)ViewModels.StaticSettingsViewModel.LiveOffset;
                            adaptiveMediaSource.DesiredLiveOffset = TimeSpan.FromSeconds(lo);
                            LogMessage("Desired Live Offset:: " + lo.ToString());

                            Windows.Media.Core.MediaSource source = Windows.Media.Core.MediaSource.CreateFromAdaptiveMediaSource(adaptiveMediaSource);
                            if (source != null)
                            {
                                // Source
                                //if(IsSmoothStreaming(Content))
                                //{
                                //    CreateTimeTextSources(source);
                                //}
                                if (playbackItem!=null)
                                {
                                    playbackItem.TimedMetadataTracksChanged -= PlaybackItem_TimedMetadataTracksChanged;
                                    playbackItem = null;
                                }
                                playbackItem = new Windows.Media.Playback.MediaPlaybackItem(source);
                                if (playbackItem != null)
                                {
                                    if((playbackItem.TimedMetadataTracks!=null)&& (playbackItem.TimedMetadataTracks.LongCount() > 0))
                                    {
                                        LogMessage("Timed Metadata Tracks discovered while the url is opened:"); 
                                        foreach (var subtitletrack in playbackItem.TimedMetadataTracks)
                                        {
                                            LogMessage("TrackID: " + subtitletrack.Id + " Type " + subtitletrack.TrackKind.ToString() + " Lang: " + subtitletrack.Language.ToString());
                                        }
                                    }
                                    playbackItem.TimedMetadataTracksChanged += PlaybackItem_TimedMetadataTracksChanged;
                                    mediaPlayer.Source = playbackItem;
                                    return true;
                                }
                            }
                        }
                        else
                            LogMessage("Failed to create AdaptiveMediaSource: " + result.Status.ToString());

                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("Exception Playing: " + ex.Message.ToString());
                CurrentMediaUrl = string.Empty;
                CurrentPosterUrl = string.Empty;
            }
            return false;
        }

        private void PlaybackItem_TimedMetadataTracksChanged(Windows.Media.Playback.MediaPlaybackItem sender, IVectorChangedEventArgs args)
        {
            if (args.CollectionChange == CollectionChange.ItemInserted)
            {
                LogMessage("Timed Metadata Tracks updated:");
                foreach (var subtitletrack in playbackItem.TimedMetadataTracks)
                {
                    LogMessage("TrackID: " + subtitletrack.Id + " Type " + subtitletrack.TrackKind.ToString() + " Lang: " + subtitletrack.Language.ToString());
                }
                uint changedTrackIndex = args.Index;
                Windows.Media.Core.TimedMetadataTrack changedTrack = playbackItem.TimedMetadataTracks[(int)changedTrackIndex];

                if (changedTrack.Language == "en")
                {
                    playbackItem.TimedMetadataTracks.SetPresentationMode(changedTrackIndex, Windows.Media.Playback.TimedMetadataTrackPresentationMode.PlatformPresented);
                }
            }
        }
        #endregion

        #region SmoothStreaming
        static bool IsCaptionStream(Microsoft.Media.AdaptiveStreaming.IManifestStream stream)
        {
            return stream.Type == Microsoft.Media.AdaptiveStreaming.MediaStreamType.Text && (stream.SubType == "CAPT" || stream.SubType == "SUBT");
        }
        /// <summary>
        /// Called when the SMOOTH manifest has been downloaded and parsed
        /// If the asset is in the cache don't restrick track: the MediaCache will select the correct audio and video track
        /// </summary>
        private void SmoothStreamingManager_ManifestReadyEvent(Microsoft.Media.AdaptiveStreaming.AdaptiveSource sender, Microsoft.Media.AdaptiveStreaming.ManifestReadyEventArgs args)
        {
            // VC1 Codec flag: true if VC1 codec is used for this Smooth Streaming content
            bool bVC1CodecDetected = false;            
            LogMessage("Manifest Ready for uri: " + sender.Uri.ToString());
            uint MaxBitRate = (uint) ViewModels.StaticSettingsViewModel.MaxBitrate;
            uint MinBitRate = (uint) ViewModels.StaticSettingsViewModel.MinBitrate;
            // Get the list of subtitle streams
           //  List<Microsoft.Media.AdaptiveStreaming.IManifestStream> AvailableCaptionStreams = args.AdaptiveSource.Manifest.AvailableStreams.Where(IsCaptionStream).Select(s => s).ToList();

            foreach (var stream in args.AdaptiveSource.Manifest.SelectedStreams)
            {

                if (stream.Type == Microsoft.Media.AdaptiveStreaming.MediaStreamType.Video)
                {
                    
                    foreach (var track in stream.SelectedTracks)
                    {
                        LogMessage("  Bitrate: " + track.Bitrate.ToString() + " Width: " + track.MaxWidth.ToString() + " Height: " + track.MaxHeight.ToString() + " FourCC: " + track.FourCC) ;
                        if ((bVC1CodecDetected == false) && ((track.FourCC == 0x31435657/*WVC1*/) || (track.FourCC == 0x31435641 /*AVC1*/)))
                            bVC1CodecDetected = true;
                    }

                    IReadOnlyList<Microsoft.Media.AdaptiveStreaming.IManifestTrack> list = null;

                    // if asset not in the Cache the application can restrick track
                    if ((MinBitRate > 0) && (MaxBitRate > 0))
                    {
                        list = stream.AvailableTracks.Where(t => (t.Bitrate > MinBitRate) && (t.Bitrate <= MaxBitRate)).ToList();
                        if ((list != null) && (list.Count > 0))
                            stream.RestrictTracks(list);
                    }
                    else if (MinBitRate > 0)
                    {
                        list = stream.AvailableTracks.Where(t => (t.Bitrate > MinBitRate)).ToList();
                        if ((list != null) && (list.Count > 0))
                            stream.RestrictTracks(list);
                    }
                    else if (MaxBitRate > 0)
                    {
                        list = stream.AvailableTracks.Where(t => (t.Bitrate < MaxBitRate)).ToList();
                        if ((list != null) && (list.Count > 0))
                            stream.RestrictTracks(list);
                    }
                    if ((list != null) && (list.Count > 0))
                    {
                        LogMessage("Select Bitrate between: " + MinBitRate.ToString() + " and " + MaxBitRate.ToString());
                        foreach (var track in stream.SelectedTracks)
                        {
                            LogMessage("  Bitrate: " + track.Bitrate.ToString() + " Width: " + track.MaxWidth.ToString() + " Height: " + track.MaxHeight.ToString());

                        }
                    }
                }
            }
            if((args.AdaptiveSource !=null)&&(args.AdaptiveSource.Manifest != null))
            {
                if(args.AdaptiveSource.Manifest.IsLive)
                {
                    LogMessage("  Live Stream detected, DVR buffer length: " + args.AdaptiveSource.Manifest.DVRWindowLength.ToString());
                }
            }
            // if the platform does support Hardware DRM and VC1 codec is used by this content
            // the application will force the Software DRM  as current Hardware DRM implementation doesn't support VC1 codec 
            if((bVC1CodecDetected == true)&&(IsHardwareDRMEnabled()))
            {
                LogMessage("Enable Software DRM as VC1 content has been detected");
                EnableSoftwareDRM(true);
            }
        }
        /// <summary>
        /// Called when the bitrate changed for SMOOTH streams
        /// </summary>
        private void SmoothStreamingManager_AdaptiveSourceStatusUpdatedEvent(Microsoft.Media.AdaptiveStreaming.AdaptiveSource sender, Microsoft.Media.AdaptiveStreaming.AdaptiveSourceStatusUpdatedEventArgs args)
        {
            //LogMessage("AdaptiveSourceStatusUpdatedEvent for uri: " + sender.Uri.ToString());
            if (args != null)
            {
                if (args.UpdateType == Microsoft.Media.AdaptiveStreaming.AdaptiveSourceStatusUpdateType.BitrateChanged)
                {

                    LogMessage("Bitrate changed for uri: " + sender.Uri.ToString());
                    foreach (var stream in args.AdaptiveSource.Manifest.SelectedStreams)
                    {
                        if (stream.Type == Microsoft.Media.AdaptiveStreaming.MediaStreamType.Video)
                        {
                            if (!string.IsNullOrEmpty(args.AdditionalInfo))
                            {
                                int pos = args.AdditionalInfo.IndexOf(';');
                                if (pos > 0)
                                {
                                    try
                                    {

                                        var newBitrate = uint.Parse(args.AdditionalInfo.Substring(0, pos));
                                        foreach (var track in stream.SelectedTracks)
                                        {
                                            if (track.Bitrate == newBitrate)
                                            {
                                                LogMessage("  Bitrate: " + track.Bitrate.ToString() + " Width: " + track.MaxWidth.ToString() + " Height: " + track.MaxHeight.ToString());
                                                break;
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {

                                    }
                                }
                            }
                        }
                    }
                }
                else if (args.UpdateType == Microsoft.Media.AdaptiveStreaming.AdaptiveSourceStatusUpdateType.StartEndTime)
                {
                    if(LogLiveBuffer)
                         LogMessage("Smooth Streaming Time changed - Start " + (new TimeSpan(args.StartTime)).ToString() + " End: " + (new TimeSpan(Math.Max(args.EndTime, args.StartTime + args.AdaptiveSource.Manifest.Duration))).ToString() + " Live: " + (new TimeSpan(args.EndTime)).ToString() + " Position: " + mediaPlayer.PlaybackSession.Position.ToString());
                    // Set Live Offset for Live Smooth Stream                        
                    if (ViewModels.StaticSettingsViewModel.LiveOffset >= 0)
                    {
                        if (mediaPlayer.PlaybackSession.Position == TimeSpan.FromSeconds(0))
                        {
                            LogMessage("Smooth Streaming Time changed - Start " + (new TimeSpan(args.StartTime)).ToString() + " End: " + (new TimeSpan(Math.Max(args.EndTime, args.StartTime + args.AdaptiveSource.Manifest.Duration))).ToString() + " Live: " + (new TimeSpan(args.EndTime)).ToString() + " Position: " + mediaPlayer.PlaybackSession.Position.ToString());
                            CurrentStartPosition = TimeSpan.FromTicks(args.EndTime) - TimeSpan.FromSeconds(ViewModels.StaticSettingsViewModel.LiveOffset);
                            SetLiveCurrentStartPosition = true;
                            LogMessage("Changing Live Smooth Streaming Start position to: " + CurrentStartPosition.ToString());
                        }
                    }
                    try
                    {
                        // Create our timeline properties object 
                        var timelineProperties = new Windows.Media.SystemMediaTransportControlsTimelineProperties();

                        // Fill in the data, using the media elements properties 
                        timelineProperties.StartTime = new TimeSpan(args.StartTime);
                        timelineProperties.MinSeekTime = new TimeSpan(args.StartTime);
                        timelineProperties.Position = mediaPlayer.PlaybackSession.Position;
                        timelineProperties.MaxSeekTime = new TimeSpan(args.EndTime);
                        timelineProperties.EndTime = new TimeSpan(args.EndTime);

                        // Update the System Media transport Controls 
                        SystemControls.UpdateTimelineProperties(timelineProperties);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception while updating the timeline: " + ex.Message);

                    }

                }
            }
        }
        public IAsyncOperation<Microsoft.Media.AdaptiveStreaming.DownloaderResponse> RequestAsync(Microsoft.Media.AdaptiveStreaming.DownloaderRequest request)
        {
            System.Threading.Tasks.TaskCompletionSource<Microsoft.Media.AdaptiveStreaming.DownloaderResponse> resp = new System.Threading.Tasks.TaskCompletionSource<Microsoft.Media.AdaptiveStreaming.DownloaderResponse>();
            Microsoft.Media.AdaptiveStreaming.DownloaderResponse dr; 
            if (httpHeaders != null)
            {
                Dictionary<string, string> localHttpHeaders = new Dictionary<string, string>();
                SetHttpHeaders(request.Headers, localHttpHeaders);
                SetHttpHeaders(httpHeaders, localHttpHeaders);
                dr = new Microsoft.Media.AdaptiveStreaming.DownloaderResponse(request.RequestUri, localHttpHeaders, null);
            }
            else
                dr = new Microsoft.Media.AdaptiveStreaming.DownloaderResponse(request.RequestUri, null, null);
            resp.TrySetResult(dr);
            Windows.Foundation.IAsyncOperation<Microsoft.Media.AdaptiveStreaming.DownloaderResponse> respc = resp.Task.AsAsyncOperation();
            return respc;
        }
        public void ResponseData(Microsoft.Media.AdaptiveStreaming.DownloaderRequest request, Microsoft.Media.AdaptiveStreaming.DownloaderResponse response)
        {
        }
        #endregion

        #region DASH_HLS

        /// <summary>
        /// Called when the bitrate changed for DASH or HLS streams
        /// </summary>
        private void AdaptiveMediaSource_PlaybackBitrateChanged(Windows.Media.Streaming.Adaptive.AdaptiveMediaSource sender, Windows.Media.Streaming.Adaptive.AdaptiveMediaSourcePlaybackBitrateChangedEventArgs args)
        {
            LogMessage("PlaybackBitrateChanged from " + args.OldValue + " to " + args.NewValue);
        }
        public static ulong ParseTime(string s)
        {
            ulong d = 0;
            System.Text.RegularExpressions.Regex shortTime = new System.Text.RegularExpressions.Regex(@"^\s*(\d+)?:?(\d+):(\d+).(\d+)\s*$");
            //            System.Text.RegularExpressions.Regex shortTime = new System.Text.RegularExpressions.Regex(@"^\s*(\d+)?:?(\d+):([\d\.]+)\s*$");
            // System.Text.RegularExpressions.Regex longTime = new System.Text.RegularExpressions.Regex(@"^\s*(\d{2}):(\d{2}):(\d{2}):(\d{2})\s*$");
            System.Text.RegularExpressions.MatchCollection mc = shortTime.Matches(s);
            if ((mc != null) && (mc.Count == 1))
            {
                ulong hours = 0;
                ulong minutes = 0;
                ulong seconds = 0;
                ulong milliseconds = 0;
                if (mc[0].Groups.Count >= 5)
                {
                    if (ulong.TryParse(mc[0].Groups[1].Value, out hours))
                    {
                        if (ulong.TryParse(mc[0].Groups[2].Value, out minutes))
                        {
                            if (ulong.TryParse(mc[0].Groups[3].Value, out seconds))
                            {
                                if (ulong.TryParse(mc[0].Groups[4].Value, out milliseconds))
                                {
                                    d = hours * 3600 * 1000 + minutes * 60 * 1000 + seconds * 1000 + milliseconds;
                                }
                            }
                        }
                    }
                }
            }
            return d;
        }

        public static string TimeToString(ulong d)
        {
            ulong hours = d / (3600 * 1000);
            ulong minutes = (d - hours * 3600 * 1000) / (60 * 1000);
            ulong seconds = (d - hours * 3600 * 1000 - minutes * 60 * 1000) / 1000;
            ulong milliseconds = (d - hours * 3600 * 1000 - minutes * 60 * 1000 - seconds * 1000);
            //if (hours == 0)
            //    return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
            //else 
            if (hours < 100)
                return string.Format("{0:00}:{1:00}:{2:00}.{3:000}", hours, minutes, seconds, milliseconds);
            else
                return string.Format("{0}:{1:00}:{2:00}.{3:000}", hours, minutes, seconds, milliseconds);
        }
        public class SubtitleItem
        {
            public ulong startTime;
            public ulong endTime;
            public string subtitle;
            public SubtitleItem(ulong start, ulong end, string sub)
            {
                startTime = start;
                endTime = end;
                subtitle = sub;
            }
            public override string ToString()
            {
                return TimeToString(startTime) + " --> " + TimeToString(endTime) + "\r\n" + subtitle + "\r\n\r\n";
            }
        }

        private string ConvertWebVTT(string input)
        {
            string output = string.Empty;
            if(!string.IsNullOrEmpty(input))
            {
                string[] separator = { "\r\n", "\r", "\n" }; 
                string[] linesArray = input.Split(separator, StringSplitOptions.None);
                if((linesArray!=null) && (linesArray.Length>1))
                {
                    if((linesArray[0].Contains("WEBVTT"))&&
                        (linesArray[1].Contains("X-TIMESTAMP-MAP")))
                    {
                        Regex blankLine = new Regex(@"^\s*$");
                        Regex captionLine = new Regex(@"^(?:<v\s+([^>]+)>)?([^\r\n]+)$");
                        Regex timeLine = new Regex(@"^([\d\.:]+)\s+-->\s+([\d\.:]+)(?:\s.*)?$");
                        ulong startTime = ulong.MaxValue;
                        ulong endTime = ulong.MaxValue;
                        string caption = string.Empty ;
                        string args = linesArray[1].Replace("X-TIMESTAMP-MAP=", "");
                        ulong plusTime = 0;
                        ulong minusTime = 0;
                        ulong mpegtsTime = ulong.MaxValue;
                        ulong localTime = ulong.MaxValue;
                        char[] sep = { ','};
                        string[] values = args.Split(sep);
                        if((values!=null)&&(values.Length == 2))
                        {
                            for (int i = 0; i < values.Length; i++)
                            {
                                if (values[i].Trim().StartsWith("MPEGTS:"))
                                {
                                    string loc = values[i].Trim().Replace("MPEGTS:", "");
                                    if (ulong.TryParse(loc, out mpegtsTime))
                                        mpegtsTime = mpegtsTime / 90;


                                }
                                if (values[i].Trim().StartsWith("LOCAL:"))
                                {
                                    string loc = values[i].Trim().Replace("LOCAL:","");
                                    localTime = ParseTime(loc);
                                }
                            }
                        }
                        if ((localTime != ulong.MaxValue)&&(mpegtsTime != ulong.MaxValue))
                        {
                            if (localTime > mpegtsTime)
                                minusTime = localTime - mpegtsTime;
                            else
                                plusTime = mpegtsTime - localTime;
                            List<SubtitleItem> captionArray = new List<SubtitleItem>();
                            for (int i = 2; i < linesArray.Length; i++)
                            {
                                if (blankLine.IsMatch(linesArray[i]))
                                {
                                    if (!string.IsNullOrEmpty(caption) &&
                                        (startTime != ulong.MaxValue) &&
                                        (endTime != ulong.MaxValue))
                                    {
                                        SubtitleItem item = new SubtitleItem(startTime - minusTime + plusTime, endTime - minusTime + plusTime, caption);
                                        if (item != null)
                                            captionArray.Add(item);
                                    }
                                    startTime = ulong.MaxValue;
                                    endTime = ulong.MaxValue;
                                    caption = string.Empty;
                                    continue;
                                }

                                MatchCollection m = timeLine.Matches(linesArray[i]);
                                if ((m != null) && (m.Count >= 1))
                                {
                                    if (!string.IsNullOrEmpty(caption) &&
                                         (startTime != ulong.MaxValue) &&
                                         (endTime != ulong.MaxValue))
                                    {
                                        SubtitleItem item = new SubtitleItem(startTime - minusTime + plusTime, endTime - minusTime + plusTime, caption);
                                        if (item != null)
                                            captionArray.Add(item);
                                    }
                                    startTime = ulong.MaxValue;
                                    endTime = ulong.MaxValue;
                                    caption = string.Empty;

                                    GroupCollection gc = m[0].Groups;
                                    if (gc.Count == 3)
                                    {
                                        startTime = ParseTime(gc[1].Value);
                                        endTime = ParseTime(gc[2].Value);
                                    }
                                    //for (int k= 0; k< cc.Count; k++)
                                    //{
                                    //    ParseTime(cc[k].Value);
                                    //}
                                    continue;
                                }

                                m = captionLine.Matches(linesArray[i]);
                                if ((m != null) && (m.Count >= 1) && (startTime != ulong.MaxValue) && (endTime != ulong.MaxValue))
                                {
                                    GroupCollection gc = m[0].Groups;
                                    if (gc.Count == 3)
                                    {
                                        if (!string.IsNullOrEmpty(caption))
                                            caption += " " + gc[2].Value;
                                        else
                                            caption = gc[2].Value;
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(caption) &&
                                (startTime != ulong.MaxValue) &&
                                (endTime != ulong.MaxValue))
                            {
                                SubtitleItem item = new SubtitleItem(startTime - minusTime + plusTime, endTime - minusTime + plusTime, caption);
                                if (item != null)
                                    captionArray.Add(item);
                            }

                            output += "WEBVTT\r\n\r\n";
                            for (int i = 0; i < captionArray.Count; i++)
                            {
                                if (!string.IsNullOrEmpty(captionArray[i].subtitle))
                                {
                                    output += captionArray[i].ToString();
                                }
                            }

                        }
                        
                    }
                }
            }
            return output;
        }
        /// <summary>
        /// Called when the download of a DASH or HLS chunk is requested
        /// </summary>

        private async void AdaptiveMediaSource_DownloadRequested(Windows.Media.Streaming.Adaptive.AdaptiveMediaSource sender, Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceDownloadRequestedEventArgs args)
        {
            if(LogDownload)
                LogMessage("DownloadRequested for uri: " + args.ResourceUri.ToString() + " type: " + args.ResourceType.ToString());

            //
            // If the requested resource is a WEBVTT file
            // Check the WEBVTT file:
            // if the file contains X-TIMESTAMP-MAP 
            // it may be required to change the timestamps in the  WEBVTT File
            // 
            if ((args.ResourceUri.ToString().EndsWith(".vtt")) || (args.ResourceUri.ToString().EndsWith(".webvtt")))
            {
                var defer = args.GetDeferral();
                if (defer != null)
                {
                    using (var httpClient = new Windows.Web.Http.HttpClient())
                    {

                        string text = string.Empty;
                        Windows.Web.Http.HttpResponseMessage hrm = null;
                        try
                        {
                            hrm = await httpClient.GetAsync(args.ResourceUri);
                            if((hrm!=null)&&(hrm.StatusCode == Windows.Web.Http.HttpStatusCode.Ok))
                            {
                                var b = await hrm.Content.ReadAsBufferAsync();
                                text = System.Text.UTF8Encoding.UTF8.GetString(b.ToArray());
                            }
                           // text = await httpClient.GetStringAsync(args.ResourceUri);
                        }
                        catch(Exception ex)
                        {
                            text = string.Empty;
                            LogMessage("Exception while downloading subtitles: " + ex.Message);
                        }

                        if (LogSubtitle)
                            LogMessage("Input Subtitles: " + text);
                        if (!string.IsNullOrEmpty(text)&& (text.Contains("X-TIMESTAMP-MAP")))
                        {
                            // convert string to stream
                            string convertedText = ConvertWebVTT(text);
                            if (!string.IsNullOrEmpty(convertedText))
                            {
                                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(convertedText);
                                if (byteArray != null)
                                {
                                    MemoryStream stream = new MemoryStream(byteArray);
                                    if (stream != null)
                                    {
                                        args.Result.ResourceUri = args.ResourceUri;
                                        args.Result.ContentType = args.ResourceType.ToString();
                                        args.Result.InputStream = stream.AsInputStream();
                                    }

                                }
                                if (LogSubtitle)
                                    LogMessage("Converted Subtitle: " + convertedText);
                            }
                        }
                    }
                    defer.Complete();
                    return;
                }
            }

            // 
            // If no Http Headers are defined 
            // nothing to do
            // 
            if ((httpHeaders == null) || (httpHeaders.Count == 0))
                return;


            var deferral = args.GetDeferral();
            if (deferral != null)
            {

                args.Result.ResourceUri = args.ResourceUri;
                args.Result.ContentType = args.ResourceType.ToString();
                var filter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
                filter.CacheControl.WriteBehavior = Windows.Web.Http.Filters.HttpCacheWriteBehavior.NoCache;

                using (var httpClient = new Windows.Web.Http.HttpClient(filter))
                {
                    try
                    {
                        Windows.Web.Http.HttpRequestMessage request = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, args.Result.ResourceUri);
                        if (httpHeaders != null)
                        {
                            SetHttpHeaders(httpHeaders, request.Headers);
                            // Add Authorization Header if Key requested
                            if (args.ResourceType == Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceResourceType.Key)
                            {
                                if ((httpHeaders != null) && (httpHeaders.ContainsKey("Authorization")))
                                {
                                    
                                    string s = httpHeaders["Authorization"];
                                    if (s.StartsWith("bearer=", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string Token = s.Substring(7);
                                        request.Headers.Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Bearer", Token); 
                                    }
                                }
                            }
                        }
                        
                        Windows.Web.Http.HttpResponseMessage response = await httpClient.SendRequestAsync(request);
                       // args.Result.ExtendedStatus = (uint)response.StatusCode;
                        if (response.IsSuccessStatusCode)
                        {
                            //args.Result.ExtendedStatus = (uint)response.StatusCode;
                            args.Result.InputStream = await response.Content.ReadAsInputStreamAsync();

                        }
                        else
                            LogMessage("DownloadRequested for uri: " + args.ResourceUri.ToString() + " error: " + response.StatusCode.ToString());
                    }
                    catch (Exception e)
                    {
                        LogMessage("DownloadRequested for uri: " + args.ResourceUri.ToString() + " exception: " + e.Message);

                    }
//                    LogMessage("DownloadRequested for uri: " + args.ResourceUri.ToString() + " done");
                    deferral.Complete();
                }
            }
            
        }

        /// <summary>
        /// Called when the download of a DASH or HLS chunk failed
        /// </summary>
        private void AdaptiveMediaSource_DownloadFailed(Windows.Media.Streaming.Adaptive.AdaptiveMediaSource sender, Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceDownloadFailedEventArgs args)
        {
            LogMessage("DownloadRequested failed for uri: " + args.ResourceUri.ToString());
        }

        /// <summary>
        /// Called when the download is completed for a DASH or HLS chunk
        /// </summary>
        private void AdaptiveMediaSource_DownloadCompleted(Windows.Media.Streaming.Adaptive.AdaptiveMediaSource sender, Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceDownloadCompletedEventArgs args)
        {
           // LogMessage("DownloadRequested completed for uri: " + args.ResourceUri.ToString());
        }
        /// <summary>
        /// Called when the bitrate change for DASH or HLS 
        /// </summary>
        private void AdaptiveMediaSource_DownloadBitrateChanged(Windows.Media.Streaming.Adaptive.AdaptiveMediaSource sender, Windows.Media.Streaming.Adaptive.AdaptiveMediaSourceDownloadBitrateChangedEventArgs args)
        {
          //  LogMessage("DownloadBitrateChangedfrom " + args.OldValue + " to " + args.NewValue);

        }

        #endregion

        #region PLAYREADY
        // Dictionary used to store the KeyId associated with the video asset
        // This dictionary is used to retrieve the PlayReady license Expiration date from the keyId
        Dictionary<String, Guid> PlayReadyUrlKeyIdDictionary = new Dictionary<string, Guid>();
        Windows.Media.Protection.MediaProtectionManager protectionManager;
        private const int MSPR_E_CONTENT_ENABLING_ACTION_REQUIRED = -2147174251;
        public const int DRM_E_NOMORE_DATA = -2147024637; //( 0x80070103 )
        public const int MSPR_E_NEEDS_INDIVIDUALIZATION = -2147174366; // (0x8004B822)
        private string PlayReadyLicenseUrl;
        private string PlayReadyChallengeCustomData;

        /// <summary>
        /// HardwareDRMInitialized
        /// True if HardwareDRMSupported has been set following a call to MediaHelpers.PlayReadyHelper.IsHardwareDRMSupported()
        /// </summary>
        private bool HardwareDRMInitialized = false;
        /// <summary>
        /// True if hardware DRM is supported on the platform
        /// This variable is initialized when the ProtectionManager is initialized
        /// </summary>
        private bool HardwareDRMSupported = false;
        /// <summary>
        /// Return true if the Windows 10 platform does support Hardware DRM 
        /// </summary>
        bool IsHardwareDRMSupported()
        {
            if (HardwareDRMInitialized == false)
            {
                HardwareDRMSupported = MediaHelpers.PlayReadyHelper.IsHardwareDRMSupported();
                HardwareDRMInitialized = true;
            }
            return HardwareDRMSupported;
        }
        /// <summary>
        /// Return true if the application is configured to support Hardware DRM
        /// Return true if the Windows 10 platform does support Hardware DRM 
        /// </summary>
        bool IsHardwareDRMEnabled()
        {
            bool bResult = false;
            try
            {
                bResult = Windows.Media.Protection.PlayReady.PlayReadyStatics.CheckSupportedHardware(Windows.Media.Protection.PlayReady.PlayReadyHardwareDRMFeatures.HardwareDRM);
            }
            catch (Exception e)
            {
                LogMessage("Exception in IsHardwareDRMEnabled: " + e.Message);
            }
            return bResult;
        }
        /// <summary>
        /// Enable or Disable Software DRM
        /// Software DRM must be enabled when the MediaElement is playing VC1 protected content on platform supporting Hardware DRM
        /// </summary>
        bool EnableSoftwareDRM(bool bEnable)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            // Force Software DRM useful for VC1 content which doesn't support Hardware DRM
            try
            {
                if (!localSettings.Containers.ContainsKey("PlayReady"))
                    localSettings.CreateContainer("PlayReady", Windows.Storage.ApplicationDataCreateDisposition.Always);
                localSettings.Containers["PlayReady"].Values["SoftwareOverride"] = (bEnable==true ? 1: 0);
            }
            catch (Exception e)
            {
                LogMessage("Exception while forcing software DRM: " + e.Message);
            }
            //Setup Software Override based on app setting
            //By default, PlayReady uses Hardware DRM if the machine support it. However, in case the app still want
            //software behavior, they can set localSettings.Containers["PlayReady"].Values["SoftwareOverride"]=1. 
            //This code tells MF to use software override as well
            if (localSettings.Containers.ContainsKey("PlayReady") &&
                localSettings.Containers["PlayReady"].Values.ContainsKey("SoftwareOverride"))
            {
                int UseSoftwareProtectionLayer = (int)localSettings.Containers["PlayReady"].Values["SoftwareOverride"];

                if(protectionManager.Properties.ContainsKey("Windows.Media.Protection.UseSoftwareProtectionLayer"))
                    protectionManager.Properties["Windows.Media.Protection.UseSoftwareProtectionLayer"] = (UseSoftwareProtectionLayer == 1? true : false);
                else  
                    protectionManager.Properties.Add("Windows.Media.Protection.UseSoftwareProtectionLayer", (UseSoftwareProtectionLayer == 1 ? true : false));
            }
            return true;
        }
        /// <summary>
        /// Invoked when the Protection Manager can't load some components
        /// </summary>
        void ProtectionManager_ComponentLoadFailed(Windows.Media.Protection.MediaProtectionManager sender, Windows.Media.Protection.ComponentLoadFailedEventArgs e)
        {
            LogMessage("ProtectionManager ComponentLoadFailed");
            e.Completion.Complete(false);
        }
        /// <summary>
        /// Invoked to acquire the PlayReady License
        /// </summary>
        async System.Threading.Tasks.Task<bool> LicenseAcquisitionRequest(Windows.Media.Protection.PlayReady.PlayReadyLicenseAcquisitionServiceRequest licenseRequest, Windows.Media.Protection.MediaProtectionServiceCompletion CompletionNotifier, string Url, string ChallengeCustomData)
        {
            bool bResult = false;
            string ExceptionMessage = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(Url))
                {
                    LogMessage("ProtectionManager PlayReady Manual License Acquisition Service Request in progress - URL: " + Url);

                    if (!string.IsNullOrEmpty(ChallengeCustomData))
                    {
                        // disable Base64String encoding
                        //System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                        //byte[] b = encoding.GetBytes(ChallengeCustomData);
                        //licenseRequest.ChallengeCustomData = Convert.ToBase64String(b, 0, b.Length);
                        licenseRequest.ChallengeCustomData = ChallengeCustomData;
                    }

                    Windows.Media.Protection.PlayReady.PlayReadySoapMessage soapMessage = licenseRequest.GenerateManualEnablingChallenge();

                    byte[] messageBytes = soapMessage.GetMessageBody();
                    Windows.Web.Http.IHttpContent httpContent = new Windows.Web.Http.HttpBufferContent(messageBytes.AsBuffer());

                    IPropertySet propertySetHeaders = soapMessage.MessageHeaders;
                    foreach (string strHeaderName in propertySetHeaders.Keys)
                    {
                        string strHeaderValue = propertySetHeaders[strHeaderName].ToString();

                        // The Add method throws an ArgumentException try to set protected headers like "Content-Type"
                        // so set it via "ContentType" property
                        if (strHeaderName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                            httpContent.Headers.ContentType = Windows.Web.Http.Headers.HttpMediaTypeHeaderValue.Parse(strHeaderValue);
                        else
                            httpContent.Headers.TryAppendWithoutValidation(strHeaderName.ToString(), strHeaderValue);
                    }
                    // Add custom header for license acquisition
                    SetHttpHeaders(httpHeaders, httpContent.Headers);
                    Windows.Web.Http.Headers.HttpCredentialsHeaderValue Authorization = null;
                    if ((httpHeaders != null) && (httpHeaders.ContainsKey("Authorization")))
                    {

                        string s = httpHeaders["Authorization"];
                        if (s.StartsWith("bearer=", StringComparison.OrdinalIgnoreCase))
                        {
                            string Token = s.Substring(7);
                            Authorization = new Windows.Web.Http.Headers.HttpCredentialsHeaderValue("Bearer", Token);
                        }
                    }
                    CommonLicenseRequest licenseAcquision = new CommonLicenseRequest();
                    
                    Windows.Web.Http.IHttpContent responseHttpContent = await licenseAcquision.AcquireLicense(new Uri(Url), Authorization, httpContent);
                    if (responseHttpContent != null)
                    {
                        //string res = await responseHttpContent.ReadAsStringAsync();
                        var buffer = await responseHttpContent.ReadAsBufferAsync();
                        Exception exResult = licenseRequest.ProcessManualEnablingResponse(buffer.ToArray());
                        if (exResult != null)
                        {
                            throw exResult;
                        }
                        bResult = true;
                    }
                    else
                        ExceptionMessage = licenseAcquision.GetLastErrorMessage();
                }
                else
                {
                    LogMessage("ProtectionManager PlayReady License Acquisition Service Request in progress - URL: " + licenseRequest.Uri.ToString());
                  
                    await licenseRequest.BeginServiceRequest();
                    bResult = true;
                }
            }
            catch (Exception e)
            {
                ExceptionMessage = e.Message;
            }

            if (bResult == true)
                LogMessage(!string.IsNullOrEmpty(Url) ? "ProtectionManager Manual PlayReady License Acquisition Service Request successful" :
                    "ProtectionManager PlayReady License Acquisition Service Request successful");
            else
                LogMessage(!string.IsNullOrEmpty(Url) ? "ProtectionManager Manual PlayReady License Acquisition Service Request failed: " + ExceptionMessage :
                    "ProtectionManager PlayReady License Acquisition Service Request failed: " + ExceptionMessage);
            if (CompletionNotifier != null)
                CompletionNotifier.Complete(bResult);
            return bResult;
        }
        /// <summary>
        /// Proactive Individualization Request 
        /// </summary>
        async System.Threading.Tasks.Task<bool> ProActiveIndivRequest()
        {
            Windows.Media.Protection.PlayReady.PlayReadyIndividualizationServiceRequest indivRequest = new Windows.Media.Protection.PlayReady.PlayReadyIndividualizationServiceRequest();
            bool bResultIndiv = await ReactiveIndivRequest(indivRequest, null);
            return bResultIndiv;

        }
        /// <summary>
        /// Invoked to send the Individualization Request 
        /// </summary>
        async System.Threading.Tasks.Task<bool> ReactiveIndivRequest(Windows.Media.Protection.PlayReady.PlayReadyIndividualizationServiceRequest IndivRequest, Windows.Media.Protection.MediaProtectionServiceCompletion CompletionNotifier)
        {
            bool bResult = false;
            Exception exception = null;
            LogMessage("ProtectionManager PlayReady Individualization Service Request in progress...");
            try
            {
                await IndivRequest.BeginServiceRequest();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                if (exception == null)
                {
                    bResult = true;
                }
                else
                {
                    System.Runtime.InteropServices.COMException comException = exception as System.Runtime.InteropServices.COMException;
                    if (comException != null && comException.HResult == MSPR_E_CONTENT_ENABLING_ACTION_REQUIRED)
                    {
                        IndivRequest.NextServiceRequest();
                    }
                }
            }
            if (bResult == true)
                LogMessage("ProtectionManager PlayReady Individualization Service Request successful");
            else
                LogMessage("ProtectionManager PlayReady Individualization Service Request failed");
            if (CompletionNotifier != null) CompletionNotifier.Complete(bResult);
            return bResult;

        }

        /// <summary>
        /// Invoked to send a PlayReady request (Individualization or License request)
        /// </summary>
        private async void ProtectionManager_ServiceRequested(Windows.Media.Protection.MediaProtectionManager sender, Windows.Media.Protection.ServiceRequestedEventArgs e)
        {
            LogMessage("ProtectionManager ServiceRequested - Current DRM Configuration: " + (IsHardwareDRMEnabled()?"Hardware":"Software"));
            if (e.Request is Windows.Media.Protection.PlayReady.PlayReadyIndividualizationServiceRequest)
            {
                Windows.Media.Protection.PlayReady.PlayReadyIndividualizationServiceRequest IndivRequest = e.Request as Windows.Media.Protection.PlayReady.PlayReadyIndividualizationServiceRequest;
                bool bResultIndiv = await ReactiveIndivRequest(IndivRequest, e.Completion);
            }
            else if (e.Request is Windows.Media.Protection.PlayReady.PlayReadyLicenseAcquisitionServiceRequest)
            {
                Windows.Media.Protection.PlayReady.PlayReadyLicenseAcquisitionServiceRequest licenseRequest = e.Request as Windows.Media.Protection.PlayReady.PlayReadyLicenseAcquisitionServiceRequest;
                bool result = await LicenseAcquisitionRequest(licenseRequest, e.Completion, PlayReadyLicenseUrl, PlayReadyChallengeCustomData);
                if(result==true)
                {
                    // Store the keyid of the current video
                    // if the user wants to retrieve subsequently the PlayReady license Expiration date
                    if(!PlayReadyUrlKeyIdDictionary.ContainsKey(CurrentMediaUrl))
                        PlayReadyUrlKeyIdDictionary.Add(CurrentMediaUrl, licenseRequest.ContentHeader.KeyId);
                    // Get the expiration date of PlayReady license
                    DateTime d = GetLicenseExpirationDate(licenseRequest.ContentHeader.KeyId);
                    if (d != DateTime.MinValue)
                    {

                        LogMessage("PlayReady license Expiration Date: " + d.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve the PlayReady license expiration date based onthe video KeyID
        /// This method uses the Windows Runtime library MediaHelpers to get the expiration date
        /// The use of this library is a turn around to a PlayReady issue with .Net Native.
        /// </summary>
        private DateTime GetLicenseExpirationDate(Guid videoId)
        {
            
            var keyIdString = Convert.ToBase64String(videoId.ToByteArray());
            try
            {
                var contentHeader = new Windows.Media.Protection.PlayReady.PlayReadyContentHeader(
                    videoId,
                    keyIdString,
                    Windows.Media.Protection.PlayReady.PlayReadyEncryptionAlgorithm.Aes128Ctr,
                    null,
                    null,
                    string.Empty,
                    new Guid());
                Windows.Media.Protection.PlayReady.IPlayReadyLicense[] licenses = new Windows.Media.Protection.PlayReady.PlayReadyLicenseIterable(contentHeader, true).ToArray();
                foreach (var lic in licenses)
                {
                    DateTimeOffset? d = MediaHelpers.PlayReadyHelper.GetLicenseExpirationDate(lic);
                    if((d!=null)&&(d.HasValue))
                            return d.Value.DateTime;
                }
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine("GetLicenseExpirationDate Exception: " + e.Message);
                return DateTime.MinValue;
            }
            return DateTime.MinValue;
        }
        #endregion

        #region Settings



        /// <summary>
        /// Function to read all the persistent attributes
        /// </summary>
        public async System.Threading.Tasks.Task<bool> ReadSettings()
        {


            // Restore PlayList path and index in the local settings
            string s = ViewModels.StaticSettingsViewModel.CurrentPlayListPath;
            if (!string.IsNullOrEmpty(s))
            {
                LogMessage("PlayerPage Loading Data for path: " + s);
                if (await LoadingData(s) == true)
                {
                    int index = ViewModels.StaticSettingsViewModel.CurrentMediaIndex;
                    if ((index < 0) || (index > comboStream.Items.Count))
                        index = 0;
                    comboStream.SelectedIndex = index;
                    MediaItem ms = comboStream.SelectedItem as MediaItem;
                    if (ms != null)
                    {
                        mediaUri.Text = ms.Content;
                        PlayReadyLicenseUrl = ms.PlayReadyUrl;
                        httpHeadersString = ms.HttpHeaders;
                        httpHeaders = GetHttpHeaders(httpHeadersString);

                        PlayReadyChallengeCustomData = ms.PlayReadyCustomData;
                    }
                    s = ViewModels.StaticSettingsViewModel.CurrentMediaPath;
                    if (!string.IsNullOrEmpty(s))
                    {
                        mediaUri.Text = s;
                    }
                    
                        
                }
                else
                {
                    await LoadingData(string.Empty);
                    comboStream.SelectedIndex = 0;
                    MediaItem ms = comboStream.SelectedItem as MediaItem;
                    if (ms != null)
                    {
                        mediaUri.Text = ms.Content;
                        PlayReadyLicenseUrl = ms.PlayReadyUrl;
                        httpHeadersString = ms.HttpHeaders;
                        httpHeaders = GetHttpHeaders(httpHeadersString);

                        PlayReadyChallengeCustomData = ms.PlayReadyCustomData;
                    }
                }
            }
            // Restore WindowState
            SetAutoStartWindowState();
            return true;
        }
        /// <summary>
        /// Function to save all the persistent attributes
        /// </summary>
        public bool SaveSettings()
        {
            // Save PlayList path and index in the local settings
            ViewModels.StaticSettingsViewModel.CurrentPlayListPath = MediaDataSource.MediaDataPath;
            ViewModels.StaticSettingsViewModel.CurrentMediaIndex = comboStream.SelectedIndex;
            ViewModels.StaticSettingsViewModel.CurrentMediaPath = mediaUri.Text;
            return true;
        }
        /// <summary>
                 /// Function to read a setting value and clear it after reading it
                 /// </summary>
        public static object ReadSettingsValue(string key)
        {
            if (!Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                return null;
            }
            else
            {
                var value = Windows.Storage.ApplicationData.Current.LocalSettings.Values[key];
                Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove(key);
                return value;
            }
        }

        /// <summary>
        /// Save a key value pair in settings. Create if it doesn't exist
        /// </summary>
        public static void SaveSettingsValue(string key, object value)
        {
            if (!Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values.Add(key, value);
            }
            else
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values[key] = value;
            }
        }
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
                    catch(Exception ex)
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
                            localCompanionDevice = new CompanionDevice(string.Empty, false,Information.SystemInformation.DeviceName, companionConnectionManager.GetSourceIP(), Information.SystemInformation.SystemFamily);
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
                LogMessage(multicast +  "Companion Initialization Error" );
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
            catch(Exception ex)
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
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        try
                        {
                            int count = ViewModels.StaticSettingsViewModel.PlayListList.Count;
                            int index = ViewModels.StaticSettingsViewModel.CurrentPlayListIndex;
                            if (index < count-1)
                                ++index;
                            else
                                index = 0;
                            if (index >= 0)
                            {
                                string path = ViewModels.StaticSettingsViewModel.PlayListList[index].ImportedPath;
                                if (string.IsNullOrEmpty(path))
                                    path = ViewModels.StaticSettingsViewModel.PlayListList[index].Path;
                                if (!string.IsNullOrEmpty(path))
                                {
                                    int i = ViewModels.StaticSettingsViewModel.PlayListList[index].Index;
                                    await LoadingData(path);
                                    comboStream.SelectedIndex = i;
                                    MediaItem ms = comboStream.SelectedItem as MediaItem;
                                    mediaUri.Text = ms.Content;
                                    if (ViewModels.StaticSettingsViewModel.AutoStart)
                                    {
                                      //  SetAutoStartWindowState();
                                        PlayCurrentUrl();
                                    }
                                    ViewModels.StaticSettingsViewModel.CurrentPlayListIndex = index;
                                    ViewModels.StaticSettingsViewModel.CurrentPlayListPath = path;
                                    ViewModels.StaticSettingsViewModel.CurrentMediaIndex = i;
                                    UpdateControls();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage("Failed to to play: " + mediaUri.Text + " Exception: " + ex.Message);
                        }
                    });

                    break;
                case CompanionProtocol.commandMinusPlaylist:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async  () =>
                    {
                        try
                        { 
                            int count = ViewModels.StaticSettingsViewModel.PlayListList.Count;
                            int index = ViewModels.StaticSettingsViewModel.CurrentPlayListIndex;
                            if (index > 0)
                                --index;
                            else
                                index = count - 1;
                            if(index>=0)
                            {
                                string path = ViewModels.StaticSettingsViewModel.PlayListList[index].ImportedPath;
                                if (string.IsNullOrEmpty(path))
                                    path = ViewModels.StaticSettingsViewModel.PlayListList[index].Path;
                                if (!string.IsNullOrEmpty(path))
                                {
                                    int i = ViewModels.StaticSettingsViewModel.PlayListList[index].Index;
                                    await LoadingData(path);
                                    comboStream.SelectedIndex = i;
                                    MediaItem ms = comboStream.SelectedItem as MediaItem;
                                    mediaUri.Text = ms.Content;
                                    if (ViewModels.StaticSettingsViewModel.AutoStart)
                                    {
                                        //SetAutoStartWindowState();
                                        PlayCurrentUrl();
                                    }
                                    ViewModels.StaticSettingsViewModel.CurrentPlayListIndex = index;
                                    ViewModels.StaticSettingsViewModel.CurrentPlayListPath = path;
                                    ViewModels.StaticSettingsViewModel.CurrentMediaIndex = i;
                                    UpdateControls();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage("Failed to to play: " + mediaUri.Text + " Exception: " + ex.Message);
                        }
                    });

                    break;
                case CompanionProtocol.commandOpenPlaylist:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        if ((Parameters != null) && (Parameters.ContainsKey(CompanionProtocol.parameterContent)))
                        {
                            string Path = Parameters[CompanionProtocol.parameterContent];
                            if (Path != null)
                            {
                                await LoadingData(Path);
                                MediaItem ms = comboStream.SelectedItem as MediaItem;
                                mediaUri.Text = ms.Content;
                                if (ViewModels.StaticSettingsViewModel.AutoStart)
                                {
                                    SetAutoStartWindowState();
                                    PlayCurrentUrl();
                                }
                                UpdateControls();
                            }
                        }
                    });
                    break;
                case CompanionProtocol.commandOpen:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        if ((Parameters != null) && (Parameters.ContainsKey(CompanionProtocol.parameterContent)))
                        {
                            string path = Parameters[CompanionProtocol.parameterContent];
                            if (path != null)
                            {
                                string value = string.Empty;
                                Parameters.TryGetValue(CompanionProtocol.parameterPosterContent, out value);
                                string poster = value;
                                value = "0";
                                Parameters.TryGetValue(CompanionProtocol.parameterStart, out value);
                                long start = 0;
                                long.TryParse(value, out start);
                                Parameters.TryGetValue(CompanionProtocol.parameterDuration, out value);
                                long duration = 0;
                                long.TryParse(value, out duration);
                                await StartPlay("", path, poster, start, duration);

                                // Update control and play first video
                                UpdateControls();
                            }
                        }
                    });
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
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if ((IsFullWindow()) || (IsFullScreen()))
                            SetWindowMode(WindowMediaState.WindowMode);
                        else
                            Fullwindow_Click(null, null);
                    });
                    break;
                case CompanionProtocol.commandFullScreen:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if ((IsFullWindow()) || (IsFullScreen()))
                            SetWindowMode(WindowMediaState.WindowMode);
                        else
                            Fullscreen_Click(null, null);
                    });
                    break;
                case CompanionProtocol.commandWindow:
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if (mediaPlayerElement.IsFullWindow == true)
                            mediaPlayerElement.IsFullWindow = false;
                    });
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
                LogMessage("Internet Network Connection is on");
            }
            else
            {
                if (IsNetworkRequired())
                {
                    await Shell.Current.DisplayNetworkWarning(true, "The current playlist: " + ViewModels.StaticSettingsViewModel.CurrentPlayListPath + " requires an internet connection" );
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
