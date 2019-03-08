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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;
using AudioVideoPlayer.DataModel;
using Windows.Foundation;
using Windows.Web.Http.Filters;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.UI.Core;

namespace AudioVideoPlayer.DLNA
{
    
    static class ExtendedExecutionHelper
    {
        private static ExtendedExecutionSession session = null;
        private static int taskCount = 0;

        public static bool IsRunning
        {
            get
            {
                if (session != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static async Task<ExtendedExecutionResult> RequestSessionAsync(ExtendedExecutionReason reason, TypedEventHandler<object, ExtendedExecutionRevokedEventArgs> revoked, String description)
        {
            // The previous Extended Execution must be closed before a new one can be requested.       
            ClearSession();

            var newSession = new ExtendedExecutionSession();
            newSession.Reason = reason;
            newSession.Description = description;
            newSession.Revoked += SessionRevoked;

            // Add a revoked handler provided by the app in order to clean up an operation that had to be halted prematurely
            if (revoked != null)
            {
                newSession.Revoked += revoked;
            }

            ExtendedExecutionResult result = await newSession.RequestExtensionAsync();

            switch (result)
            {
                case ExtendedExecutionResult.Allowed:
                    session = newSession;
                    break;
                default:
                case ExtendedExecutionResult.Denied:
                    newSession.Dispose();
                    break;
            }
            return result;
        }

        public static void ClearSession()
        {
            if (session != null)
            {
                session.Dispose();
                session = null;
            }

            taskCount = 0;
        }

        public static Deferral GetExecutionDeferral()
        {
            if (session == null)
            {
                throw new InvalidOperationException("No extended execution session is active");
            }

            taskCount++;
            return new Deferral(OnTaskCompleted);
        }

        private static void OnTaskCompleted()
        {
            if (taskCount > 0)
            {
                taskCount--;
            }

            //If there are no more running tasks than end the extended lifetime by clearing the session
            if (taskCount == 0 && session != null)
            {
                ClearSession();
            }
        }

        private static void SessionRevoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            //The session has been prematurely revoked due to system constraints, ensure the session is disposed
            if (session != null)
            {
                session.Dispose();
                session = null;
            }

            taskCount = 0;
        }
    }

    public enum DLNADeviceStatus
    {
        //
        // Summary:
        //     The DLNADevice is unavailable.
        Unavailable = 0,
        //
        // Summary:
        //     The DLNADevice is available.
        Available = 1,
        //
        // Summary:
        //     The DLNADevice is connected.
        Connected = 2,
        //
        // Summary:
        //     The availability of the DLNADevice is unknown.
        Unknown = 3
    }
    public enum DLNADevicePlayMode
    {
        Normal = 0,
        Shuffle = 1,
        RepeatOne = 2,
        RepeatAll = 3
    }
    public enum DLNADeviceThreadSessionEvent
    {
        TheadStarted = 0,
        ThreadStoppedBeforeStarting = 1,
        ThreadStoppedByUser = 2,
        ThreadAutoStoppedInBackground = 3,
        ThreadStoppedWhenFreed = 4,
        ThreadRevoked = 5,
        ExtendedExecutionSessionCreated = 6,
        ExtendedExecutionSessionRemoved = 7,
        ExtendedExecutionSessionDenied = 8,
        ThreadAttachedToSession = 9,
        ThreadDetachedFromSession = 10
    }

    public class DLNADeviceConnectionLossInBackgroundException : Exception
    {
        public DLNADeviceConnectionLossInBackgroundException()
        {
        }

        public DLNADeviceConnectionLossInBackgroundException(string message)
            : base(message)
        {
        }

        public DLNADeviceConnectionLossInBackgroundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    public class DLNADeviceEndOfLoopInBackgroundException : Exception
    {
        public DLNADeviceEndOfLoopInBackgroundException()
        {
        }

        public DLNADeviceEndOfLoopInBackgroundException(string message)
            : base(message)
        {
        }

        public DLNADeviceEndOfLoopInBackgroundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    public class DLNADevice : IDisposable
    {
        private const string tcpPort = "1255";
        public string Id { get; set; }
        public string Location { get; set; }
        public string Version { get; set; }
        public string IpCache { get; set; }
        public string Server { get; set; }
        public string St { get; set; }
        public string Usn { get; set; }
        public string Ip { get; set; }
        public string FriendlyName { get; set; }
        public string Manufacturer { get; set; }
        public string ModelName { get; set; }
        public string ModelNumber { get; set; }
        public DLNADeviceStatus Status { get; set; }
        public string PlayerId { get; set; }
        public DLNADevicePlayMode PlayMode { get; set; }
        public bool ShuffleMode { get; set; }

        private DateTime LatestConnectionTime = DateTime.MinValue;
        private DateTime LatestConnectionCheckTime = DateTime.MinValue;
        public List<DLNAService> ListDLNAServices { get; set; }

        public MediaDataGroup ListMediaItem { get; set; }


        public static string TRANSPORT_STATE_PLAYING = "PLAYING";
        public static string TRANSPORT_STATE_STOPPED = "STOPPED";
        public static string TRANSPORT_STATE_TRANSITIONING = "TRANSITIONING";
        public static string TRANSPORT_STATE_PAUSED_PLAYBACK = "PAUSED_PLAYBACK";
        public static string TRANSPORT_STATE_PAUSED_RECORDING = "PAUSED_RECORDING";
        public static string TRANSPORT_STATE_RECORDING = "RECORDING";
        public static string TRANSPORT_STATE_NO_MEDIA_PRESENT = "NO_MEDIA_PRESENT";

        public static string TRANSPORT_STATUS_OK = "OK";
        public static string TRANSPORT_STATUS_ERROR_OCCURRED = "ERROR_OCCURRED";

        public static string PLAY_MODE_NORMAL = "NORMAL";
        public static string PLAY_MODE_SHUFFLE = "SHUFFLE";
        public static string PLAY_MODE_REPEAT_ONE = "REPEAT_ONE";
        public static string PLAY_MODE_REPEAT_ALL = "REPEAT_ALL";
        public static string PLAY_MODE_RANDOM = "RANDOM";
        public static string PLAY_MODE_INTRO = "INTRO";

        private string latest_transport_state;
        private string latest_transport_status;
        private int latest_transport_speed;
        private string latest_play_mode;
        private string latest_currentUri;

        System.Threading.Tasks.Task DevicePlaybackMonitorTask;
        System.Threading.Tasks.Task DevicePlayerStateMonitorTask;
        System.Threading.Tasks.Task DevicePlayerModeMonitorTask;

        private bool bDevicePlaybackMonitorTaskRunning;
        private bool bDevicePlaybackMonitorTaskStopped;
        private bool bDevicePlayerStateMonitorTaskRunning;
        private bool bDevicePlayerStateMonitorTaskStopped;
        private bool bDevicePlayerModeMonitorTaskRunning;
        private bool bDevicePlayerModeMonitorTaskStopped;
        private static ExtendedExecutionSession session = null;
        private bool bSessionAvailable;
        private static int NumberOfDeviceUsingSession = 0;
        private int taskCount = 0;
        private CancellationTokenSource monitorTokenSource;
        private CancellationToken monitorCancellationToken;
        private bool IsInBackgroundMode;

        TimeSpan periodDevicePlaybackMonitorTask = TimeSpan.FromMilliseconds(1000);
        TimeSpan periodDevicePlayerStateMonitorTask = TimeSpan.FromMilliseconds(1000);
        TimeSpan periodDevicePlayerModeMonitorTask = TimeSpan.FromMilliseconds(1000);
        TimeSpan periodConnectionCheck = TimeSpan.FromMilliseconds(3000);
        TimeSpan periodConnectionLoss = TimeSpan.FromMilliseconds(30000);
        TimeSpan periodWaitTaskCompletion = TimeSpan.FromMilliseconds(1000);


        public bool IsShuffleMode()
        {
            return ShuffleMode;
        }
        protected virtual void OnDeviceMediaInformationUpdated(DLNADevice d, DLNAMediaInformation info)
        {
            if (DeviceMediaInformationUpdated != null)
                DeviceMediaInformationUpdated(d, info);
        }
        public event TypedEventHandler<DLNADevice, DLNAMediaInformation> DeviceMediaInformationUpdated;


        protected virtual void OnDeviceMediaPositionUpdated(DLNADevice d, DLNAMediaPosition info)
        {
            if (DeviceMediaPositionUpdated != null)
                DeviceMediaPositionUpdated(d, info);
        }
        public event TypedEventHandler<DLNADevice, DLNAMediaPosition> DeviceMediaPositionUpdated;


        protected virtual void OnDeviceMediaTransportInformationUpdated(DLNADevice d, DLNAMediaTransportInformation info)
        {
            if (DeviceMediaTransportInformationUpdated != null)
                DeviceMediaTransportInformationUpdated(d, info);
        }
        public event TypedEventHandler<DLNADevice, DLNAMediaTransportInformation> DeviceMediaTransportInformationUpdated;

        protected virtual void OnDeviceMediaTransportSettingsUpdated(DLNADevice d, DLNAMediaTransportSettings info)
        {
            if (DeviceMediaTransportSettingsUpdated != null)
                DeviceMediaTransportSettingsUpdated(d, info);
        }
        public event TypedEventHandler<DLNADevice, DLNAMediaTransportSettings> DeviceMediaTransportSettingsUpdated;

        protected virtual void OnDeviceThreadSessionStateChanged(DLNADevice d, DLNADeviceThreadSessionEvent e)
        {
            if (DeviceThreadSessionStateChanged != null)
                DeviceThreadSessionStateChanged(d, e);
        }
        public event TypedEventHandler<DLNADevice, DLNADeviceThreadSessionEvent> DeviceThreadSessionStateChanged;


        public DLNADevice()
        {
            Id = string.Empty;
            Location = string.Empty;
            Version = string.Empty;
            IpCache = string.Empty;
            Server = string.Empty;
            St = string.Empty;
            Ip = string.Empty;
            Usn = string.Empty;
            FriendlyName = string.Empty;
            Manufacturer = string.Empty;
            ModelName = string.Empty;
            ModelNumber = string.Empty;
            latest_transport_state = string.Empty;
            latest_transport_status = string.Empty;
            latest_transport_speed = -1;
            latest_play_mode = string.Empty;
            latest_currentUri = string.Empty;
            Status = DLNADeviceStatus.Unknown;
            PlayMode = DLNADevicePlayMode.Normal;
            ShuffleMode = false;

            DevicePlaybackMonitorTask = null;
            DevicePlayerStateMonitorTask = null;
            DevicePlayerModeMonitorTask = null;

            bDevicePlaybackMonitorTaskRunning = false ;
            bDevicePlaybackMonitorTaskStopped = true;
            bDevicePlayerStateMonitorTaskRunning = false;
            bDevicePlayerStateMonitorTaskStopped = true;
            bDevicePlayerModeMonitorTaskRunning = false;
            bDevicePlayerModeMonitorTaskStopped = true;
            monitorTokenSource = null;
            bSessionAvailable = false;
            IsInBackgroundMode = false;



    }
        public DLNADevice(string id, string location, string version, string ipCache, string server,
            string st, string usn, string ip, string friendlyName, string manufacturer, string modelName, string modelNumber, DLNADevicePlayMode playMode, bool bShuffleMode)
        {
            Id = id;
            Location = location;
            Version = version;
            IpCache = ipCache;
            Server = server;
            St = st;
            Usn = usn;
            Ip = ip;
            FriendlyName = friendlyName;
            Manufacturer = manufacturer;
            ModelName = modelName;
            ModelNumber = modelNumber;
            Status = DLNADeviceStatus.Unknown;
            latest_transport_state = string.Empty;
            latest_transport_status = string.Empty;
            latest_transport_speed = -1;
            //latest_play_mode = string.Empty;
            latest_currentUri = string.Empty;
            PlayMode = playMode;
            latest_play_mode = GetPlayModeString();
            ShuffleMode = bShuffleMode;
            DevicePlaybackMonitorTask = null;
            DevicePlayerStateMonitorTask = null;
            DevicePlayerModeMonitorTask = null;

            bDevicePlaybackMonitorTaskRunning = false;
            bDevicePlaybackMonitorTaskStopped = true;
            bDevicePlayerStateMonitorTaskRunning = false;
            bDevicePlayerStateMonitorTaskStopped = true;
            bDevicePlayerModeMonitorTaskRunning = false;
            bDevicePlayerModeMonitorTaskStopped = true;
            monitorTokenSource = null;
            bSessionAvailable = false;
            IsInBackgroundMode = false;

        }
        public bool IsHeosDevice()
        {
            if (!string.IsNullOrEmpty(Server))
            {
                if (Server.IndexOf("Heos") >= 0)
                    return true;
            }
            if (!string.IsNullOrEmpty(Version))
            {
                if (Version.IndexOf("HEOS") >= 0)
                    return true;
            }
            return false;
        }
        public bool IsSamsungDevice()
        {
            if (!string.IsNullOrEmpty(Server))
            {
                if (Server.IndexOf("Samsung") >= 0)
                    return true;
            }
            if (!string.IsNullOrEmpty(Version))
            {
                if (Version.IndexOf("Samsung") >= 0)
                    return true;
            }
            return false;
        }

        public string GetUniqueName()
        {
            if (string.IsNullOrEmpty(this.Ip) ||
                string.IsNullOrEmpty(this.FriendlyName))
                return string.Empty;
            return this.FriendlyName.Replace(' ', '_') + "_" + this.Ip;
        }
        /// <summary>
        /// Method LoadData which loads the Device JSON playlist file
        /// </summary>
        public async System.Threading.Tasks.Task<bool> LoadDevicePlaylist()
        {
            try
            {
                string name = GetUniqueName();
                if (string.IsNullOrEmpty(name))
                    return false;

                string path = await Helpers.MediaHelper.GetPlaylistPath(name);
                MediaDataSource.Clear();
                this.ListMediaItem = await MediaDataSource.GetGroupAsync(path, "audio_video_picture");
                if (this.ListMediaItem == null)
                {
                    await Helpers.MediaHelper.CreateEmptyPlaylist(name, path);
                    this.ListMediaItem = await MediaDataSource.GetGroupAsync(path, "audio_video_picture");
                }
                if (this.ListMediaItem != null)
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }
        /// <summary>
        /// Method SaveData which loads the Device JSON playlist file
        /// </summary>
        public async System.Threading.Tasks.Task<bool> SaveDevicePlaylist(string Path)
        {
            try
            {
                string name = GetUniqueName();
                if (string.IsNullOrEmpty(name))
                    return false;
                string path = string.Empty;
                if (string.IsNullOrEmpty(Path))
                    path = await Helpers.MediaHelper.GetPlaylistPath(name);
                else
                    path = Path;
                if (await Helpers.MediaHelper.SavePlaylist(name, path, this.ListMediaItem.Items)>=0)
                    return true;
            }
            catch (Exception)
            {
            }
            return false;
        }


        int GetNextIndex(int Index)
        {
            int newIndex = -1;
            int range = this.ListMediaItem.Items.Count;
            if (range >= 1)
            {
                if (this.PlayMode == DLNADevicePlayMode.Shuffle)
                {
                    Random rg = new Random();
                    newIndex = (int)((rg.Next() / (double)int.MaxValue) * (range - 1));
                }
                else if (this.PlayMode == DLNADevicePlayMode.RepeatOne)
                {
                    newIndex = Index;
                }
                else
                {
                    if ((Index + 1) >= range)
                    {
                        if (this.PlayMode == DLNADevicePlayMode.RepeatAll)
                            newIndex = 0;
                        else
                            newIndex = -1;
                    }
                    else
                        newIndex = Index + 1;
                }
            }
            return newIndex;
        }
        public async System.Threading.Tasks.Task<bool> UpdateNextUrl()
        {
            DLNAMediaInformation info = await this.GetMediaInformation();
            if (info != null)
            {
                return await UpdateNextUrl(info);
            }
            return false;
        }
        async System.Threading.Tasks.Task<bool> UpdateNextUrl(DLNAMediaInformation info)
        {
            if (info != null)
            {
                string url = info.CurrentUri;
                if (!string.IsNullOrEmpty(url))
                {
                    if (url.StartsWith("https"))
                        url = url.Replace("https", "");
                    else if (url.StartsWith("http"))
                        url = url.Replace("http", "");
                    int index = -1;
                    int j = 0;
                    foreach (var i in this.ListMediaItem.Items)
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
                    if (index >= 0)
                    {

                        if (this.ListMediaItem.Items.Count > 0)
                        {
                            int nextIndex = GetNextIndex(index);
                            if (nextIndex >= 0)
                            {
                                MediaItem m = this.ListMediaItem.Items[nextIndex] as MediaItem;

                                if (m != null)
                                {
                                    string nexturl = info.NextUri;
                                    if (nexturl.StartsWith("https"))
                                        nexturl = nexturl.Replace("https", "");
                                    else if (nexturl.StartsWith("http"))
                                        nexturl = nexturl.Replace("http", "");
                                    if (string.IsNullOrEmpty(nexturl) || ((m.Content.IndexOf(nexturl) < 0) && (this.PlayMode != DLNADevicePlayMode.Shuffle)))
                                    {
                                        return await this.UpdatePlaylist(null, m);
                                    }
                                    return true;
                                }
                            }
                            else
                            {
                                if (IsInBackgroundMode == true)
                                {
                                    throw new DLNADeviceEndOfLoopInBackgroundException("End of Media Loop in background mode");
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        async System.Threading.Tasks.Task<bool> PlaybackMonitorThread()
        {
            if (await this.IsConnected())
            {
                DLNAMediaTransportInformation trinfo = await this.GetTransportInformation();
                if (trinfo != null)
                {
                    if (trinfo.CurrentTransportState.ToString() == TRANSPORT_STATE_PLAYING)
                    {

                        if ((this.ListMediaItem != null) &&
                        (this.ListMediaItem.Items != null) &&
                        (this.ListMediaItem.Items.Count > 0))
                        {
                            DLNAMediaPosition posinfo = await this.GetMediaPosition();
                            if (posinfo != null)
                                OnDeviceMediaPositionUpdated(this, posinfo);

                            DLNAMediaInformation info = await this.GetMediaInformation();
                            if (info != null)
                            {
                                if (latest_currentUri != info.CurrentUri)
                                {
                                    latest_currentUri = info.CurrentUri;
                                    OnDeviceMediaInformationUpdated(this, info);
                                }
                            }
                            if (info != null)
                            {
                                if (await UpdateNextUrl(info) == false)
                                {
                                    System.Diagnostics.Debug.WriteLine("Error while setting the next url for device : " + this.FriendlyName);
                                    return false;
                                }
                                return true;
                            }
                        }
                    }
                    else if (trinfo.CurrentTransportState.ToString() == TRANSPORT_STATE_STOPPED)
                    {
                        return false;
                    }
                    else if (trinfo.CurrentTransportState.ToString() == TRANSPORT_STATE_NO_MEDIA_PRESENT)
                    {
                        return false;
                    }
                    else
                        return true;
                }
            }
            else
            {
                if(this.TimeSinceLatestConnection()> periodConnectionLoss.TotalMilliseconds)
                {
                    if (IsInBackgroundMode == true)
                    {
                        throw new DLNADeviceConnectionLossInBackgroundException("Connection with Device loss in background mode");
                    }

                }
            }
            return false;
        }

        async System.Threading.Tasks.Task<bool> PlayerStateMonitorThread()
        {
            if (await this.IsConnected())
            {
                DLNAMediaTransportInformation trinfo = await this.GetTransportInformation();
                if (trinfo != null)
                {

                    bool bUpdated = false;
                    if (trinfo.CurrentTransportState != latest_transport_state)
                    {
                        latest_transport_state = trinfo.CurrentTransportState;
                        bUpdated = true;
                    }
                    if (trinfo.CurrentTransportStatus != latest_transport_status)
                    {
                        latest_transport_status = trinfo.CurrentTransportStatus;
                        bUpdated = true;
                    }
                    if (trinfo.CurrentSpeed != latest_transport_speed)
                    {
                        latest_transport_speed = trinfo.CurrentSpeed;
                        bUpdated = true;
                    }
                    if (bUpdated == true)
                    {
                        OnDeviceMediaTransportInformationUpdated(this, trinfo);
                    }
                }
                return true;
            }
            else
            {
                if (this.TimeSinceLatestConnection() > periodConnectionLoss.TotalMilliseconds)
                {
                    if (IsInBackgroundMode == true)
                    {
                        throw new DLNADeviceConnectionLossInBackgroundException("Connection with Device loss in background mode");
                    }

                }
            }
            return false;
        }


        async System.Threading.Tasks.Task<bool> PlayerModeMonitorThread()
        {
            if (await this.IsConnected())
            {
                DLNAMediaTransportSettings trsetting = await this.GetTransportSettings();
                if (trsetting != null)
                {
                    bool bUpdated = false;
                    if (trsetting.PlayMode != latest_play_mode)
                    {
                        latest_play_mode = trsetting.PlayMode;
                        bUpdated = true;
                    }
                    if (bUpdated == true)
                    {
                        OnDeviceMediaTransportSettingsUpdated(this, trsetting);
                    }
                }
                return true;
            }
            else
            {
                if (this.TimeSinceLatestConnection() > periodConnectionLoss.TotalMilliseconds)
                {
                    if (IsInBackgroundMode == true)
                    {
                        throw new DLNADeviceConnectionLossInBackgroundException("Connection with Device loss in background mode");
                    }

                }
            }
            return false;
        }

        private bool WaitEndTask(ref bool bStopped, int TimeOutMs)
        {
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;
            while ((bStopped!=true)&&((end - start).TotalMilliseconds<TimeOutMs))
            {
                // Sleep 10 ms
                Task.Delay(10).Wait();
                end = DateTime.Now;
            }
            if(bStopped == true)
                return true;
            return false;
        }
        public bool StopMonitoringDevice(DLNADeviceThreadSessionEvent e)
        {
            if((this.bDevicePlaybackMonitorTaskRunning == false) &&
             (this.bDevicePlayerStateMonitorTaskRunning == false)&&
             (this.bDevicePlayerModeMonitorTaskRunning == false)&&
             (this.bDevicePlaybackMonitorTaskStopped == true)&&
             (this.bDevicePlayerStateMonitorTaskStopped == true)&&
             (this.bDevicePlayerModeMonitorTaskStopped == true)&&
             (this.DevicePlaybackMonitorTask == null)&&
             (this.DevicePlayerStateMonitorTask == null)&&
             (this.DevicePlayerModeMonitorTask == null))
                return false;

            try
            {

              //  App.Current.EnteredBackground -= Current_EnteredBackground;
              //  App.Current.LeavingBackground -= Current_LeavingBackground;

                ClearExtendedExecution();


                this.bDevicePlaybackMonitorTaskRunning = false;
                this.bDevicePlayerStateMonitorTaskRunning = false;
                this.bDevicePlayerModeMonitorTaskRunning = false;


                WaitEndTask(ref this.bDevicePlaybackMonitorTaskStopped, (int) periodWaitTaskCompletion.TotalMilliseconds);
                WaitEndTask(ref this.bDevicePlayerStateMonitorTaskStopped, (int)periodWaitTaskCompletion.TotalMilliseconds);
                WaitEndTask(ref this.bDevicePlayerModeMonitorTaskStopped, (int)periodWaitTaskCompletion.TotalMilliseconds);

                if (this.DevicePlaybackMonitorTask != null)
                {
                    this.DevicePlaybackMonitorTask.Wait((int)periodWaitTaskCompletion.TotalMilliseconds);
                    this.DevicePlaybackMonitorTask = null;
                }
                if (this.DevicePlayerStateMonitorTask != null)
                {
                    this.DevicePlayerStateMonitorTask.Wait((int)periodWaitTaskCompletion.TotalMilliseconds);
                    this.DevicePlayerStateMonitorTask = null;
                }
                if (this.DevicePlayerModeMonitorTask != null)
                {
                    this.DevicePlayerModeMonitorTask.Wait((int)periodWaitTaskCompletion.TotalMilliseconds);
                    this.DevicePlayerModeMonitorTask = null;
                }

                this.bDevicePlaybackMonitorTaskStopped = true;
                this.bDevicePlayerStateMonitorTaskStopped = true;
                this.bDevicePlayerModeMonitorTaskStopped = true;

                OnDeviceThreadSessionStateChanged(this, e);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while stopping monitoring threads: " + ex.Message);
            }
            return true;
        }

        private void SessionRevoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            switch (args.Reason)
            {
                case ExtendedExecutionRevokedReason.Resumed:
                    System.Diagnostics.Debug.WriteLine("Extended execution revoked due to returning to foreground.");
                    break;

                case ExtendedExecutionRevokedReason.SystemPolicy:
                    System.Diagnostics.Debug.WriteLine("Extended execution revoked due to system policy." );
                    StopMonitoringDevice(DLNADeviceThreadSessionEvent.ThreadRevoked);
                    break;
            }
        }
        void ClearExtendedExecution()
        {
            // Cancel any outstanding tasks.
            if (monitorTokenSource != null)
            {
                // Save a copy of cancellationTokenSource because the call
                // to cancellationTokenSource.Cancel() will cause other code
                // to run, which might in turn call ClearExtendedExecution.
                var localCancellationTokenSource = monitorTokenSource;
                monitorTokenSource = null;

                localCancellationTokenSource.Cancel();
                localCancellationTokenSource.Dispose();
            }
            if (bSessionAvailable == true)
            {
                if (NumberOfDeviceUsingSession > 0)
                    NumberOfDeviceUsingSession--;
                bSessionAvailable = false;
                OnDeviceThreadSessionStateChanged(this, DLNADeviceThreadSessionEvent.ThreadDetachedFromSession);
                // Dispose any outstanding session.
                if (session != null)
                {
                    session.Revoked -= SessionRevoked;
                }
                if (NumberOfDeviceUsingSession == 0)
                {
                    if (session != null)
                    {
                        session.Dispose();
                        session = null;
                        OnDeviceThreadSessionStateChanged(this, DLNADeviceThreadSessionEvent.ExtendedExecutionSessionRemoved);
                    }
                }
            }

        }

        private void OnTaskCompleted()
        {
            taskCount--;
            if (taskCount == 0 && session != null)
            {

                System.Diagnostics.Debug.WriteLine("All Tasks Completed, ending Extended Execution.");
            }
        }
        private Deferral GetExecutionDeferral()
        {
            if (session == null)
            {
                System.Diagnostics.Debug.WriteLine("Extended execution session null while creating new task.");
                return null;
                //throw new InvalidOperationException("No extended execution session is active");
            }

            taskCount++;
            return new Deferral(OnTaskCompleted);
        }
        private async System.Threading.Tasks.Task<bool> BeginExtendedExecution()
        {
            if (session != null)
            {
                bSessionAvailable = true;
                NumberOfDeviceUsingSession++;
                session.Revoked += SessionRevoked;
                OnDeviceThreadSessionStateChanged(this, DLNADeviceThreadSessionEvent.ThreadAttachedToSession);
                return true;
            }

            // The previous Extended Execution must be closed before a new one can be requested.
            // This code is redundant here because the sample doesn't allow a new extended
            // execution to begin until the previous one ends, but we leave it here for illustration.
            ClearExtendedExecution();

            if (session == null)
            {
                var newSession = new ExtendedExecutionSession();
                // Select Location Tracking to be sure to run in background
                newSession.Reason = ExtendedExecutionReason.LocationTracking;
                newSession.Description = "Running multiple tasks";
                newSession.Revoked += SessionRevoked;
                ExtendedExecutionResult result = await newSession.RequestExtensionAsync();

                switch (result)
                {
                    case ExtendedExecutionResult.Allowed:
                        System.Diagnostics.Debug.WriteLine("Extended execution allowed.");
                        session = newSession;
                        bSessionAvailable = true;
                        NumberOfDeviceUsingSession++;
                        OnDeviceThreadSessionStateChanged(this, DLNADeviceThreadSessionEvent.ThreadAttachedToSession);
                        OnDeviceThreadSessionStateChanged(this, DLNADeviceThreadSessionEvent.ExtendedExecutionSessionCreated);
                        break;

                    default:
                    case ExtendedExecutionResult.Denied:
                        System.Diagnostics.Debug.WriteLine("Extended execution denied.");
                        newSession.Dispose();
                        bSessionAvailable = false;
                        OnDeviceThreadSessionStateChanged(this, DLNADeviceThreadSessionEvent.ExtendedExecutionSessionDenied);
                        break;
                }
            }
            else
            {
                session.Revoked += SessionRevoked;
                bSessionAvailable = true;
            }
            if (bSessionAvailable == false)
                return false;
            return true;
        }
        private void Current_EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            IsInBackgroundMode = true;
        }
        private async void Current_LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            IsInBackgroundMode = false;
            // if the Monitoring Thread has been revoked in background, restart monitoring thread

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            async () =>
            {
                if (bSessionAvailable == false)
                    await StartMonitoringDevice();
            });

        }
        public async System.Threading.Tasks.Task<bool> StartMonitoringDevice()
        {
            
            App.Current.EnteredBackground += Current_EnteredBackground;
            App.Current.LeavingBackground += Current_LeavingBackground;
            string name = GetUniqueName();
            if (string.IsNullOrEmpty(name))
                return false;
            StopMonitoringDevice(DLNADeviceThreadSessionEvent.ThreadStoppedBeforeStarting);

            if ((true == await BeginExtendedExecution())&&(session != null))
            {
                this.bDevicePlaybackMonitorTaskRunning = true;
                this.bDevicePlaybackMonitorTaskStopped = false;
                this.bDevicePlayerStateMonitorTaskRunning = true;
                this.bDevicePlayerStateMonitorTaskStopped = false;
                this.bDevicePlayerModeMonitorTaskRunning = true;
                this.bDevicePlayerModeMonitorTaskStopped = false;

                monitorTokenSource = new CancellationTokenSource();
                monitorCancellationToken = monitorTokenSource.Token;

                DevicePlaybackMonitorTask = System.Threading.Tasks.Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        using (var deferral = GetExecutionDeferral())
                        {
                            if (deferral == null)
                            {
                                System.Diagnostics.Debug.WriteLine("GetExecutionDeferral return null cancel task.");
                            }
                            else
                            {
                                while (this.bDevicePlaybackMonitorTaskRunning)
                                {
                                    if (monitorCancellationToken.IsCancellationRequested)
                                        break;
                                    await Task.Delay((int)periodDevicePlaybackMonitorTask.TotalMilliseconds, monitorCancellationToken);
                                    if (monitorCancellationToken.IsCancellationRequested)
                                        break;
                                    await PlaybackMonitorThread();
                                }
                            }
                        }
                    }
                    catch(DLNADeviceEndOfLoopInBackgroundException ex)
                    {
                        System.Diagnostics.Debug.WriteLine("DLNADeviceEndOfLoopInBackgroundException in Playback Monitor Thread: " + ex.Message);
                        StopMonitoringDevice(DLNADeviceThreadSessionEvent.ThreadAutoStoppedInBackground);
                    }
                    catch (DLNADeviceConnectionLossInBackgroundException ex)
                    {
                        System.Diagnostics.Debug.WriteLine("DLNADeviceConnectionLossInBackgroundException in Playback Monitor Thread: " + ex.Message);
                        StopMonitoringDevice(DLNADeviceThreadSessionEvent.ThreadAutoStoppedInBackground);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Playback Monitor Thread: " + ex.Message);
                    }
                    finally
                    {
                        this.bDevicePlaybackMonitorTaskStopped = true;
                    }
                }
                );

                DevicePlayerStateMonitorTask = System.Threading.Tasks.Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        using (var deferral = GetExecutionDeferral())
                        {
                            if (deferral == null)
                            {
                                System.Diagnostics.Debug.WriteLine("GetExecutionDeferral return null cancel task.");
                            }
                            else
                            {
                                while (this.bDevicePlayerStateMonitorTaskRunning)
                                {
                                    if (monitorCancellationToken.IsCancellationRequested)
                                        break;
                                    await Task.Delay((int)periodDevicePlayerStateMonitorTask.TotalMilliseconds, monitorCancellationToken);
                                    if (monitorCancellationToken.IsCancellationRequested)
                                        break;
                                    await PlayerStateMonitorThread();
                                }
                            }
                        }
                    }
                    catch (DLNADeviceEndOfLoopInBackgroundException ex)
                    {
                        System.Diagnostics.Debug.WriteLine("DLNADeviceEndOfLoopInBackgroundException in Playback Monitor Thread: " + ex.Message);
                        StopMonitoringDevice(DLNADeviceThreadSessionEvent.ThreadAutoStoppedInBackground);
                    }
                    catch (DLNADeviceConnectionLossInBackgroundException ex)
                    {
                        System.Diagnostics.Debug.WriteLine("DLNADeviceConnectionLossInBackgroundException in Playback Monitor Thread: " + ex.Message);
                        StopMonitoringDevice(DLNADeviceThreadSessionEvent.ThreadAutoStoppedInBackground);
                    }

                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Player State Monitor Thread: " + ex.Message);
                    }
                    finally
                    {
                        this.bDevicePlayerStateMonitorTaskStopped = true;
                    }
                }
                );

                DevicePlayerModeMonitorTask = System.Threading.Tasks.Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        using (var deferral = GetExecutionDeferral())
                        {
                            if (deferral == null)
                            {
                                System.Diagnostics.Debug.WriteLine("GetExecutionDeferral return null cancel task.");
                            }
                            else
                            {
                                while (this.bDevicePlayerModeMonitorTaskRunning)
                                {
                                    if (monitorCancellationToken.IsCancellationRequested)
                                        break;
                                    await Task.Delay((int)periodDevicePlayerModeMonitorTask.TotalMilliseconds, monitorCancellationToken);
                                    if (monitorCancellationToken.IsCancellationRequested)
                                        break;
                                    await PlayerModeMonitorThread();
                                }
                            }
                        }
                    }
                    catch (DLNADeviceEndOfLoopInBackgroundException ex)
                    {
                        System.Diagnostics.Debug.WriteLine("DLNADeviceEndOfLoopInBackgroundException in Playback Monitor Thread: " + ex.Message);
                        StopMonitoringDevice(DLNADeviceThreadSessionEvent.ThreadAutoStoppedInBackground);
                    }
                    catch (DLNADeviceConnectionLossInBackgroundException ex)
                    {
                        System.Diagnostics.Debug.WriteLine("DLNADeviceConnectionLossInBackgroundException in Playback Monitor Thread: " + ex.Message);
                        StopMonitoringDevice(DLNADeviceThreadSessionEvent.ThreadAutoStoppedInBackground);
                    }

                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in Player Mode Monitor Thread: " + ex.Message);
                    }
                    finally
                    {
                        this.bDevicePlayerModeMonitorTaskStopped = true;
                    }
                }
                );
            }
            return true;
        }



        public void Dispose()
        {
            StopMonitoringDevice(DLNADeviceThreadSessionEvent.ThreadStoppedWhenFreed);
        }
        /// <summary>
        /// This method checks if the url is a music url 
        /// </summary>
        private static bool IsAudio(string url)
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
        private static string GetCodec(string url)
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
        async System.Threading.Tasks.Task<bool> UpdatePlaylist(MediaItem item1, MediaItem item2)
        {
            bool result = false;
            if (item1 != null)
            {
                string ContentUrl = item1.Content;
                string AlbumUrl = item1.PosterContent;
                string Title = item1.Title;
                string Codec = GetCodec(ContentUrl);
                if (this.IsSamsungDevice())
                {
                    ContentUrl = ContentUrl.Replace("https://", "http://");
                    AlbumUrl = AlbumUrl.Replace("https://", "http://");
                }
                result = await this.PlayUrl(IsAudio(ContentUrl), ContentUrl, AlbumUrl, Title, Codec);
            }
            if (item2 != null)
            {
                string ContentUrl = item2.Content;
                string AlbumUrl = item2.PosterContent;
                string Title = item2.Title;
                string Codec = GetCodec(ContentUrl);
                if (this.IsSamsungDevice())
                {
                    ContentUrl = ContentUrl.Replace("https://", "http://");
                    AlbumUrl = AlbumUrl.Replace("https://", "http://");
                }
                result = await this.AddNextUrl(IsAudio(ContentUrl), ContentUrl, AlbumUrl, Title, Codec);
            }
            return result;
        }
        public async System.Threading.Tasks.Task<List<DLNAService>> GetDNLAServices()
        {
            if ((ListDLNAServices == null) ||
                (ListDLNAServices.Count == 0))
            {
                string content = await this.GetLocationContent();
                if (!string.IsNullOrEmpty(content))
                {
                    ListDLNAServices = DLNAService.CreateDLNAServiceList(content);
                }
            }
            return ListDLNAServices;
        }
        public async System.Threading.Tasks.Task<string> GetLocationContent()
        {
            string result = string.Empty;
            try
            {
                int timeout = 1000;
                using (var cts = new CancellationTokenSource(timeout))
                {
                    cts.CancelAfter(timeout);

                    HttpBaseProtocolFilter RootFilter = new HttpBaseProtocolFilter();

                    RootFilter.CacheControl.ReadBehavior = Windows.Web.Http.Filters.HttpCacheReadBehavior.MostRecent;
                    RootFilter.CacheControl.WriteBehavior = Windows.Web.Http.Filters.HttpCacheWriteBehavior.NoCache;
                    var client = new HttpClient(RootFilter);
                    if (client != null)
                    {
                        result = await client.GetStringAsync(new Uri(this.Location)).AsTask(cts.Token);
                    }
                }
            }
            catch (Exception)
            {
                result = string.Empty;
            }

            return result;
        }
        public async System.Threading.Tasks.Task<bool> IsConnected()
        {
            string name = GetUniqueName();
            if (string.IsNullOrEmpty(name))
                return false;
            DateTime d = DateTime.Now;
            if((d-LatestConnectionTime).TotalSeconds>3)
            {
                string s = await GetLocationContent();
                if(!string.IsNullOrEmpty(s))
                {
                    LatestConnectionTime = d;
                    return true;
                }
                return false;
            }
            return true;
        }
        public int TimeSinceLatestConnection()
        {
            DateTime begin ;
            DateTime end = DateTime.Now;
            if (LatestConnectionTime != DateTime.MinValue)
            {
                begin = LatestConnectionTime;
            }
            else
            {
                if (LatestConnectionCheckTime == DateTime.MinValue)
                    LatestConnectionCheckTime = DateTime.Now;
                begin = LatestConnectionCheckTime;
            }
            return (int) (end - begin).TotalMilliseconds;
        }
        private async System.Threading.Tasks.Task<string> SendTelnetCommand(string cmd)
        {
            string result = string.Empty;
            try
            {
                var client = new TcpClient();
                if (client != null)
                {
                    await client.ConnectAsync(Ip, int.Parse(tcpPort));
                    var data = Encoding.UTF8.GetBytes(cmd + "\r\r\n");
                    var stm = client.GetStream();
                    stm.Write(data, 0, data.Length);
                    byte[] resp = new byte[65536];
                    var memStream = new MemoryStream();

                    int bytes = 0;

                    bytes = 0;
                    int duration = 0;
                    while ((!stm.DataAvailable) && (duration++>100))
                        await System.Threading.Tasks.Task.Delay(20); // some delay
                    if (duration <= 100)
                    {
                        bytes = await stm.ReadAsync(resp, 0, resp.Length);
                        memStream.Write(resp, 0, bytes);
                        result = Encoding.UTF8.GetString(memStream.ToArray());
                    }
                }
            }
            catch (Exception )
            {
                result = string.Empty;
            }

            return result;
        }
        public async System.Threading.Tasks.Task<string> OldSendTelnetCommand(string cmd)
        {
            string result = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(Ip))
                {
                    Windows.Networking.Sockets.StreamSocket telnetClient = new Windows.Networking.Sockets.StreamSocket();
                    if (telnetClient != null)
                    {
                        var hostName = new Windows.Networking.HostName(Ip);
                        await telnetClient.ConnectAsync(hostName, tcpPort).AsTask(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);

                        using (Stream outputStream = telnetClient.OutputStream.AsStreamForWrite())
                        {
                            var streamWriter = new StreamWriter(outputStream);
                            await streamWriter.WriteLineAsync(cmd);
                            await streamWriter.FlushAsync();

                            using (Stream inputStream = telnetClient.InputStream.AsStreamForRead())
                            {
                                StreamReader streamReader = new StreamReader(inputStream);
                                result = await streamReader.ReadLineAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.Write("Exception while sending Telnet Command: " + Ex.Message);
            }

            return result;
        }
        private string GetPidValue(string text)
        {
            string pid = string.Empty;
            if (!string.IsNullOrEmpty(text))
            {
                string start = "\"name\": \"" + FriendlyName + "\"";
                string end = "\"ip\": \"" + Ip + "\"";
                int startPos = text.IndexOf(start);
                int endPos = text.IndexOf(end);
                if ((startPos > 0) &&
                    (endPos > 0) &&
                    (endPos > startPos))
                {
                    int pidPos = text.IndexOf("\"pid\": ", startPos);
                    if (pidPos > 0)
                    {
                        int commaPos = text.IndexOf(",", pidPos);
                        if (commaPos > 0)
                        {
                            pid = text.Substring(pidPos + 7, commaPos - pidPos - 7);
                        }
                    }
                }
            }
            return pid;
        }
        public async System.Threading.Tasks.Task<bool> GetPlayerId()
        {
            bool result = false;
            string response = await SendTelnetCommand("heos://player/get_players");
            if(!string.IsNullOrEmpty(response))
            {
                PlayerId = GetPidValue(response);
                if (!string.IsNullOrEmpty(PlayerId))
                    result = true;
            }
            return result;
        }
        bool IsCommandSuccessful(string cmd, string response)
        {
            bool result = false;
            if(!string.IsNullOrEmpty(response))
            {
                int pos = response.IndexOf("\"command\": \"" + cmd +"\"");
                if(pos>0)
                {
                    int lastPos = response.IndexOf("\"result\": \"success\"",pos);
                    if (lastPos > 0)
                        result = true;
                }
            }
            return result;
        }
        int GetVolumeLevel(string cmd, string response)
        {
            int result = 0;
            if (!string.IsNullOrEmpty(response))
            {
                int pos = response.IndexOf("\"command\": \"" + cmd + "\"");
                if (pos > 0)
                {
                    int lastPos = response.IndexOf("\"result\": \"success\"", pos);
                    if (lastPos > 0)
                    {
                        int volPos = response.IndexOf("&level='", lastPos);
                        if (volPos > 0)
                        {
                            int endvolPos = response.IndexOf("'", volPos+8);
                            if(endvolPos>0)
                            {
                                string s = response.Substring(volPos + 8, endvolPos - volPos - 8);
                                if (!string.IsNullOrEmpty(s))
                                    int.TryParse(s, out result);
                            }
                        }
                    }
                }
            }
            return result;
        }
        private string XMLHead = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">\r\n<s:Body>\r\n" ;
        private string XMLFoot = "</s:Body>\r\n</s:Envelope>\r\n" ;

        private string GetHttpPrefix(string url)
        {
            string result = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(url))
                {
                    int pos = url.IndexOf("://");
                    if (pos > 0)
                    {
                        pos = url.IndexOf("/", pos + 3);
                        if (pos > 0)
                        {
                            result = url.Substring(0, pos);
                        }
                    }
                }
            }
            catch(Exception)
            {
                result = string.Empty;
            }
            return result;
        }
        string GetMetadataString(bool bAudioOnly, string UrlToPlay, string AlbumArtUrl, string Title, string codec)
        {
            //Description
            StringBuilder db = new StringBuilder(1024);

            db.Append("<DIDL-Lite xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" >\r\n");
            db.Append("<item>\r\n");
            db.Append("<dc:title>" + Title + "</dc:title>\r\n");
            db.Append("<dc:description></dc:description>");
            if (bAudioOnly)
            {
                db.Append("<res protocolInfo=\"http-get:*:audio/" + codec + ":DLNA.ORG_PN=MP3;DLNA.ORG_FLAGS=01500000000000000000000000000000;DLNA.ORG_OP=01;DLNA.ORG_CI=0\">" + UrlToPlay + "</res>\r\n");
                db.Append("<upnp:albumArtURI>" + AlbumArtUrl + "</upnp:albumArtURI>\r\n");
                db.Append("<upnp:class>object.item.audioItem</upnp:class>\r\n");
            }
            else
            {
                db.Append("<res protocolInfo=\"http-get:*:video/" + codec + ":DLNA.ORG_PN=MP3;DLNA.ORG_FLAGS=01500000000000000000000000000000;DLNA.ORG_OP=01;DLNA.ORG_CI=0\">" + UrlToPlay + "</res>\r\n");
                db.Append("<upnp:class>object.item.videoItem</upnp:class>\r\n");
            }
            db.Append("</item>\r\n");
            db.Append("</DIDL-Lite>\r\n");
            //End Description
            return System.Net.WebUtility.HtmlEncode(db.ToString());
        }
        public static string GetParameter(string content, string Name)
        {
            string result = string.Empty;
            string open = "<" + Name + ">";
            string close = "</" + Name + ">";
            int posOpen = content.IndexOf(open);
            if (posOpen > 0)
            {
                int posClose = content.IndexOf(close, posOpen);
                if (posClose > 0)
                {
                    result = content.Substring(posOpen + open.Length, posClose - posOpen - open.Length);
                }
            }
            return result;
        }
        public static string GetTitleFromMetadataString(string metadata)
        {
            string data = System.Net.WebUtility.HtmlDecode(metadata);
            if (!string.IsNullOrEmpty(data))
            {
                string param = GetParameter(data, "dc:title");
                if (param != "&quot;&quot;")
                    return param;
            }
            return string.Empty;
        }

        public static string GetAlbumArtUriFromMetadataString(string metadata)
        {
            string data = System.Net.WebUtility.HtmlDecode(metadata);
            if (!string.IsNullOrEmpty(data))
            {
                string param = GetParameter(data, "upnp:albumArtURI");
                if (param != "&quot;&quot;")
                    return param;
            }
            return string.Empty;
        }

        private async System.Threading.Tasks.Task<bool> PreparePlayTo(string ControlURL, bool bAudioOnly, string UrlToPlay, string AlbumArtUrl, string Title, string codec, int Index)
        {
            bool result = false;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);

                sb.Append("<u:SetAVTransportURI xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">\r\n");
                sb.Append("<InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID>");
                sb.Append("<CurrentURI>");

                    sb.Append(UrlToPlay);
                sb.Append("</CurrentURI>\r\n");
                
                sb.Append("<CurrentURIMetaData>");



                string desc = GetMetadataString(bAudioOnly, UrlToPlay,  AlbumArtUrl, Title, codec);
                sb.Append(desc);


                sb.Append("</CurrentURIMetaData>\r\n");
                
                sb.Append("</u:SetAVTransportURI>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#SetAVTransportURI\"");






                string scontent = sb.ToString();
                scontent = scontent.Replace("%C3%A9", "é");
                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(scontent);
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (Response.Length > 0)
                    {
                        result = true;
                    }
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while preparing PlayTo: " + ex.Message);
                result = false;
            }
            return result;
        }
        private async System.Threading.Tasks.Task<bool> PrepareNextPlayTo(string ControlURL, bool bAudioOnly, string UrlToPlay, string AlbumArtUrl, string Title, string codec, int Index)
        {
            bool result = false;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);

                sb.Append("<u:SetNextAVTransportURI xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">\r\n");
                sb.Append("<InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID>");
                sb.Append("<NextURI>");
                

                    sb.Append(UrlToPlay);
                sb.Append("</NextURI>\r\n");

                sb.Append("<NextURIMetaData>");



                if (!string.IsNullOrEmpty(UrlToPlay))
                {
                    string desc = GetMetadataString(bAudioOnly, UrlToPlay, AlbumArtUrl, Title, codec);
                    sb.Append(desc);
                }
                else
                    sb.Append(string.Empty);

                sb.Append("</NextURIMetaData>\r\n");

                sb.Append("</u:SetNextAVTransportURI>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#SetNextAVTransportURI\"");







                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {

                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (Response.Length > 0)
                    {
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while preparing PlayTo: " + ex.Message);
                result = false;
            }
            return result;
        }
        private async System.Threading.Tasks.Task<bool> Play(string ControlURL, int Index)
        {
            bool result = false;
            try
            { 
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);
                sb.Append("<u:Play xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID><Speed>1</Speed></u:Play>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#Play\"");

                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (Response.Length > 0)
                    {
                        if(Response.IndexOf("PlayResponse")>0)
                            result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending PlayTo: " + ex.Message);
                result = false;
            }

            return result;
        }
        private async System.Threading.Tasks.Task<bool> SetPlayMode(string ControlURL, int Index, string PlayMode)
        {
            bool result = false;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);
                sb.Append("<u:SetPlayMode xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID>");
                sb.Append("<NewPlayMode>");
                sb.Append(PlayMode.ToString());
                sb.Append("</NewPlayMode>");
                sb.Append("</u:SetPlayMode>\r\n");


                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
                //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#SetPlayMode\"");
                // httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");

                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                // httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (Response.Length > 0)
                    {
                        if (Response.IndexOf("SetPlayModeResponse") > 0)
                            result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending PlayTo: " + ex.Message);
                result = false;
            }

            return result;
        }

        private async System.Threading.Tasks.Task<bool> Pause(string ControlURL, int Index)
        {
            bool result = false;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);
                sb.Append("<u:Pause xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID></u:Pause>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
                //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#Pause\"");
                // httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");

                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                // httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (Response.Length > 0)
                    {
                        if (Response.IndexOf("PauseResponse") > 0)
                            result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending PlayTo: " + ex.Message);
                result = false;
            }

            return result;
        }
        private async System.Threading.Tasks.Task<bool> Stop(string ControlURL, int Index)
        {
            bool result = false;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);
                sb.Append("<u:Stop xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID></u:Stop>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
                //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#Stop\"");
                // httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");

                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                // httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (Response.Length > 0)
                    {
                        if (Response.IndexOf("StopResponse") > 0)
                            result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending PlayTo: " + ex.Message);
                result = false;
            }

            return result;
        }
        private async System.Threading.Tasks.Task<bool> Next(string ControlURL, int Index)
        {
            bool result = false;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);
                sb.Append("<u:Next xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID></u:Next>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
                //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#Next\"");
                // httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");

                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                // httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (Response.Length > 0)
                    {
                        if (Response.IndexOf("NextResponse") > 0)
                            result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending PlayTo: " + ex.Message);
                result = false;
            }

            return result;
        }

        private async System.Threading.Tasks.Task<bool> Previous(string ControlURL, int Index)
        {
            bool result = false;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);
                sb.Append("<u:Previous xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID></u:Previous>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
                //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#Previous\"");
                // httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");

                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                // httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (Response.Length > 0)
                    {
                        if (Response.IndexOf("PreviousResponse") > 0)
                            result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending PlayTo: " + ex.Message);
                result = false;
            }

            return result;
        }
        private async System.Threading.Tasks.Task<DLNAMediaTransportSettings> GetTransportSettings(string ControlURL, int Index)
        {
            DLNAMediaTransportSettings result = null;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);
                sb.Append("<u:GetTransportSettings xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID></u:GetTransportSettings>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
                // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#GetTransportSettings\"");
                //httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");

                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                //httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(Response))
                    {
                        result = new DLNAMediaTransportSettings();
                        if(result != null)
                            result.PlayMode = DLNAService.GetXMLContent(Response, "PlayMode");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending GetTransportSettings: " + ex.Message);
                result = null;
            }

            return result;
        }
        private async System.Threading.Tasks.Task<DLNAMediaTransportInformation> GetTransportInformation(string ControlURL, int Index)
        {
            DLNAMediaTransportInformation result = null;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);
                sb.Append("<u:GetTransportInfo xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID></u:GetTransportInfo>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
                // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#GetTransportInfo\"");
                //httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");

                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                //httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(Response))
                    {
                        result = new DLNAMediaTransportInformation();
                        if (result != null)
                        {
                            result.CurrentTransportState = DLNAService.GetXMLContent(Response, "CurrentTransportState");
                            result.CurrentTransportStatus = DLNAService.GetXMLContent(Response, "CurrentTransportStatus");
                            string s = DLNAService.GetXMLContent(Response, "CurrentSpeed");
                            if (!string.IsNullOrEmpty(s))
                            {
                                int i;
                                if (int.TryParse(s, out i))
                                    result.CurrentSpeed = i;
                                else
                                    result.CurrentSpeed = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending GetTransportInfo: " + ex.Message);
                result = null;
            }

            return result;
        }

        private async System.Threading.Tasks.Task<DLNAMediaPosition> GetMediaPosition(string ControlURL, int Index)
        {
            DLNAMediaPosition result = null;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);
                sb.Append("<u:GetPositionInfo xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID></u:GetPositionInfo>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
               // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
               // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#GetPositionInfo\"");
                //httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");

                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                //httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(Response))
                    {
                        result = new DLNAMediaPosition();
                        if (result != null)
                        {
                            int Track = 0;
                            TimeSpan Duration;
                            TimeSpan Position;
                            string Loc = DLNAService.GetXMLContent(Response, "Track");
                            if (!string.IsNullOrEmpty(Loc))
                                int.TryParse(Loc, out Track);
                            result.Track = Track;
                            Loc = DLNAService.GetXMLContent(Response, "TrackDuration");
                            if (!string.IsNullOrEmpty(Loc))
                                TimeSpan.TryParse(Loc, out Duration);
                            result.TrackDuration = Duration;
                            result.TrackUri = DLNAService.GetXMLContent(Response, "TrackURI");
                            Loc = DLNAService.GetXMLContent(Response, "RelTime");
                            if (!string.IsNullOrEmpty(Loc))
                                TimeSpan.TryParse(Loc, out Position);
                            result.RelTime = Position;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending GetPositionInfo: " + ex.Message);
                result = null;
            }

            return result;
        }
        private async System.Threading.Tasks.Task<bool> GetDeviceCapabilities(string ControlURL, int Index)
        {
            bool result = false;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);
                sb.Append("<u:GetDeviceCapabilities xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID></u:GetDeviceCapabilities>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
                // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#GetDeviceCapabilities\"");
                //httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");

                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                //httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(Response))
                    {

                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending PlayTo: " + ex.Message);
                result = false;
            }

            return result;
        }
        private async System.Threading.Tasks.Task<DLNAMediaInformation> GetMediaInformation(string ControlURL, int Index)
        {
            DLNAMediaInformation result = null;
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                sb.Append(XMLHead);
                sb.Append("<u:GetMediaInfo xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>");
                sb.Append(Index.ToString());
                sb.Append("</InstanceID></u:GetMediaInfo>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
                // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#GetMediaInfo\"");
                //httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");

                Windows.Web.Http.HttpStringContent httpContent = new Windows.Web.Http.HttpStringContent(sb.ToString());
                httpContent.Headers.Remove("Content-Type");
                httpContent.Headers.TryAppendWithoutValidation("Content-Type", "text/xml; charset=utf-8");
                //httpContent.Headers.Remove("Accept-Encoding");

                string prefix = GetHttpPrefix(this.Location);
                if (!string.IsNullOrEmpty(prefix))
                {
                    Windows.Web.Http.HttpResponseMessage response = await httpClient.PostAsync(new Uri(prefix + ControlURL), httpContent);
                    response.EnsureSuccessStatusCode();
                    string Response = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(Response))
                    {
                        result = new DLNAMediaInformation();
                        if (result != null)
                        {
                            int NumberTrack = 0;
                            TimeSpan Duration;
                            string Loc = DLNAService.GetXMLContent(Response, "NrTracks");
                            if (!string.IsNullOrEmpty(Loc))
                                int.TryParse(Loc, out NumberTrack);
                            result.NrTrack = NumberTrack;
                            Loc = DLNAService.GetXMLContent(Response, "MediaDuration");
                            if (!string.IsNullOrEmpty(Loc))
                                TimeSpan.TryParse(Loc, out Duration);
                            result.MediaDuration = Duration;
                            result.CurrentUri = DLNAService.GetXMLContent(Response, "CurrentURI");
                            result.NextUri = DLNAService.GetXMLContent(Response, "NextURI");
                            result.CurrentUriMetaData = DLNAService.GetXMLContent(Response, "CurrentURIMetaData");
                            result.NextUriMetaData = DLNAService.GetXMLContent(Response, "NextURIMetaData");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending GetMediaInfo: " + ex.Message);
                result = null;
            }

            return result;
        }
        public async System.Threading.Tasks.Task<DLNAService> GetDLNAService()
        {
            DLNAService result = null;
            if ((ListDLNAServices == null) ||
                (ListDLNAServices.Count == 0))
            {
                await GetDNLAServices();
            }
            if ((ListDLNAServices != null) &&
                (ListDLNAServices.Count > 0))
            {
                foreach (DLNAService ds in this.ListDLNAServices)
                {
                    if (ds.ServiceType.ToLower().IndexOf("avtransport:1") > 0)
                    {
                        result = ds;
                        break;
                    }
                }
            }
            return result;
        }

        public async System.Threading.Tasks.Task<bool> PlayUrl(bool bAudioOnly, string UrlToPlay, string AlbumArtUrl, string Title, string codec)
        {
            bool result = false;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await PreparePlayTo(ds.ControlURL, bAudioOnly, UrlToPlay, AlbumArtUrl, Title, codec, 0);
                if (result == true)
                    result = await Play(ds.ControlURL, 0);
            }
            /*
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://browse/play_stream?pid=" + PlayerId + "&url=" + url);
                if (IsCommandSuccessful("browse/play_stream", response))
                {
                    result = true;
                }
            }
            */
            return result;
        }
        public async System.Threading.Tasks.Task<bool> PlayInput(string Input)
        {
            bool result = false;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://browse/play_input?pid=" + PlayerId + "&input=" + Input);
                if (IsCommandSuccessful("browse/play_input", response))
                {
                    result = true;
                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> AddNextUrl(bool bAudioOnly, string UrlToPlay, string AlbumArtUrl, string Title, string codec)
        {
            bool result = false;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await PrepareNextPlayTo(ds.ControlURL, bAudioOnly, UrlToPlay, AlbumArtUrl, Title, codec, 0);
            }

            return result;
        }
        DLNADevicePlayMode GetPlayModeEnum(string PlayMode)
        {
            DLNADevicePlayMode result;
            if(PlayMode == DLNADevice.PLAY_MODE_NORMAL)
                result = DLNADevicePlayMode.Normal;
            else if (PlayMode == DLNADevice.PLAY_MODE_SHUFFLE)
                result = DLNADevicePlayMode.Shuffle;
            else if (PlayMode == DLNADevice.PLAY_MODE_REPEAT_ONE)
                result = DLNADevicePlayMode.RepeatOne;
            else if (PlayMode == DLNADevice.PLAY_MODE_REPEAT_ALL)
                result = DLNADevicePlayMode.RepeatAll;
            else
                result = DLNADevicePlayMode.Normal;
            return result;
        }
        string GetPlayModeString(DLNADevicePlayMode PlayMode)
        {
            string result;
            if (PlayMode == DLNADevicePlayMode.Normal)
                result = DLNADevice.PLAY_MODE_NORMAL;
            else if (PlayMode == DLNADevicePlayMode.Shuffle)
                result = DLNADevice.PLAY_MODE_SHUFFLE;
            else if (PlayMode == DLNADevicePlayMode.RepeatOne)
                result = DLNADevice.PLAY_MODE_REPEAT_ONE;
            else if (PlayMode == DLNADevicePlayMode.RepeatAll)
                result = DLNADevice.PLAY_MODE_REPEAT_ALL;
            else
                result = DLNADevice.PLAY_MODE_NORMAL;
            return result;
        }
        public async System.Threading.Tasks.Task<bool> SetPlayMode(string PlayMode)
        {
            bool result = false;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                this.PlayMode = GetPlayModeEnum(PlayMode);
                if ((this.PlayMode == DLNADevicePlayMode.RepeatOne) ||
                    (this.PlayMode == DLNADevicePlayMode.Normal))
                    result = await SetPlayMode(ds.ControlURL, 0, PlayMode);
                else
                    result = true;
                if (this.PlayMode == DLNADevicePlayMode.Shuffle)
                    ShuffleMode = true;
                else
                    ShuffleMode = false;
            }
            return result;
        }
        public string GetPlayModeString()
        {
            return GetPlayModeString(this.PlayMode);
        }
        public DLNADevicePlayMode GetPlayMode()
        {
            return this.PlayMode;
        }
        public async System.Threading.Tasks.Task<bool> Play()
        {
            bool result = false;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await Play(ds.ControlURL, 0);
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> Pause()
        {
            bool result = false;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await Pause(ds.ControlURL, 0);
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> Stop()
        {
            bool result = false;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await Stop(ds.ControlURL, 0);
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> Next()
        {
            bool result = false;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await Next(ds.ControlURL, 0);
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> Previous()
        {
            bool result = false;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await Previous(ds.ControlURL, 0);
            }
            return result;
        }
        public async System.Threading.Tasks.Task<DLNAMediaInformation> GetMediaInformation()
        {
            DLNAMediaInformation result = null;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await GetMediaInformation(ds.ControlURL, 0);
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> IsContentReady()
        {
            DLNAMediaInformation result = null;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await GetMediaInformation(ds.ControlURL, 0);
                if(result!=null)
                {
                    if (!string.IsNullOrEmpty(result.CurrentUri))
                        return true;
                }
            }
            return false;
        }
        public async System.Threading.Tasks.Task<string> GetContentUrl()
        {

            DLNAMediaInformation result = null;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await GetMediaInformation(ds.ControlURL, 0);
                if (result != null)
                    return result.CurrentUri;
            }
            return string.Empty;
        }
        public async System.Threading.Tasks.Task<string> GetNextContentUrl()
        {

            DLNAMediaInformation result = null;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await GetMediaInformation(ds.ControlURL, 0);
                if (result != null)
                    return result.NextUri;
            }
            return string.Empty;
        }
        public async System.Threading.Tasks.Task<bool> GetDeviceCapabilities()
        {
            bool result = false;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await GetDeviceCapabilities(ds.ControlURL, 0);
            }
            return result;
        }
        public async System.Threading.Tasks.Task<DLNAMediaPosition> GetMediaPosition()
        {
            DLNAMediaPosition result = null;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await GetMediaPosition(ds.ControlURL, 0);
            }
            return result;
        }
        public async System.Threading.Tasks.Task<DLNAMediaTransportInformation> GetTransportInformation()
        {
            DLNAMediaTransportInformation result = null;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await GetTransportInformation(ds.ControlURL, 0);
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> IsPlaying()
        {
            DLNAMediaTransportInformation result = null;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await GetTransportInformation(ds.ControlURL, 0);
                if (result != null)
                    if (result.CurrentTransportState.ToString() == "PLAYING")
                        return true;
            }
            return false;
        }
        public async System.Threading.Tasks.Task<bool> IsPaused()
        {
            DLNAMediaTransportInformation result = null;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await GetTransportInformation(ds.ControlURL, 0);
                if (result != null)
                    if (result.CurrentTransportState.ToString() == "PAUSED_PLAYBACK")
                        return true;
            }
            return false;
        }
        public async System.Threading.Tasks.Task<bool> IsStopped()
        {
            DLNAMediaTransportInformation result = null;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await GetTransportInformation(ds.ControlURL, 0);
                if (result != null)
                    if (result.CurrentTransportState.ToString() == "STOPPED")
                        return true;
            }
            return false;
        }
        string PreparePlayModeResult(string playMode)
        {
            string result = string.Empty;
            if(this.PlayMode == DLNADevicePlayMode.Normal)
            {
                if (playMode == DLNADevice.PLAY_MODE_NORMAL)
                    result = playMode;
                else
                {
                    this.PlayMode = GetPlayModeEnum(playMode);
                    if (this.PlayMode == DLNADevicePlayMode.Shuffle)
                        ShuffleMode = true;
                    else
                        ShuffleMode = false;

                    result = playMode;
                }
            }
            else if (this.PlayMode == DLNADevicePlayMode.RepeatOne)
            {
                if (playMode == DLNADevice.PLAY_MODE_REPEAT_ONE)
                    result = playMode;
                else
                {
                    this.PlayMode = GetPlayModeEnum(playMode);
                    if (this.PlayMode == DLNADevicePlayMode.Shuffle)
                        ShuffleMode = true;
                    else
                        ShuffleMode = false;

                    result = playMode;

                }

            }
            else if (this.PlayMode == DLNADevicePlayMode.RepeatAll)
            {
                result = DLNADevice.PLAY_MODE_REPEAT_ALL;
            }
            else if (this.PlayMode == DLNADevicePlayMode.Shuffle)
            {
                result = DLNADevice.PLAY_MODE_SHUFFLE;
            }

            return result;
        }
        public async System.Threading.Tasks.Task<DLNAMediaTransportSettings> GetTransportSettings()
        {
            DLNAMediaTransportSettings result = null;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await GetTransportSettings(ds.ControlURL, 0);
                if(result !=null)
                    result.PlayMode = PreparePlayModeResult(result.PlayMode);
            }
            return result;
        }
        public async System.Threading.Tasks.Task<string> GetPlayerState()
        {
            string result = string.Empty;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/get_play_state?pid=" + PlayerId );
                if (IsCommandSuccessful("player/get_play_state", response))
                {
                    result = GetState(response);
                }
            }
            return result;
        }
        int GetVolume(string response)
        {
            int result = 0;
            try
            {
                if (!string.IsNullOrEmpty(response))
                {
                    int pos = response.IndexOf("level=");
                    if (pos > 0)
                    {
                        int endpos = response.IndexOf("\"", pos);
                        if (endpos > 0)
                        {
                            string res = response.Substring(pos + 6, endpos - pos - 6);
                            int.TryParse(res, out result);
                        }
                    }
                }
            }
            catch (Exception)
            {
                result = -1;
            }
            return result;
        }
        string GetState(string response)
        {
            string result = "stop";
            try
            {
                if (!string.IsNullOrEmpty(response))
                {
                    int pos = response.IndexOf("state=");
                    if (pos > 0)
                    {
                        int endpos = response.IndexOf("\"", pos);
                        if (endpos > 0)
                        {
                            result = response.Substring(pos + 6, endpos - pos - 6);
                        }
                    }
                }
            }
            catch (Exception)
            {
                result = "stop";
            }
            return result;
        }
        public async System.Threading.Tasks.Task<int> GetPlayerVolume()
        {
            int result = -1;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/get_volume?pid=" + PlayerId);
                if (IsCommandSuccessful("player/get_volume", response))
                {
                    result = GetVolume(response);
                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> GetPlayerMute()
        {
            bool result = false;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/get_mute?pid=" + PlayerId);
                if (IsCommandSuccessful("player/get_mute", response))
                {
                    if(response.IndexOf("state=on")>0)
                        result = true;
                }
            }
            return result;
        }
        int GetCount(string response)
        {
            int result = 0;
            try
            {
                if (!string.IsNullOrEmpty(response))
                {
                    int pos = response.IndexOf("&count=");
                    if (pos > 0)
                    {
                        int endpos = response.IndexOf("\"", pos);
                        if (endpos > 0)
                        {
                            string res = response.Substring(pos + 7, endpos - pos - 7);
                            int.TryParse(res, out result);
                        }
                    }
                }
            }
            catch (Exception)
            {
                result = -1;
            }
            return result;
        }
        public async System.Threading.Tasks.Task<int> GetPlayerQueueCount(/*int start, int end*/)
        {
            int result = 0;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            try
            {
                if (!string.IsNullOrEmpty(PlayerId))
                {
                    //string response = await SendTelnetCommand("heos://player/get_queue?pid=" + PlayerId + "&range=" + start.ToString() + "," + end.ToString());
                    string response = await SendTelnetCommand("heos://player/get_queue?pid=" + PlayerId);
                    if (IsCommandSuccessful("player/get_queue", response))
                    {
                        result = GetCount(response);
                        /*
                        string[] sep = { "},"};
                        string[] res = response.Split(sep,StringSplitOptions.None);
                        if(res!=null)
                        {
                            if(res.Count()>0)
                            {
                                result = res.Count() - 1;
                            }
                        }
                        */
                    }
                }
            }
            catch(Exception)
            {
                result = 0;
            }
            return result;
        }
        public async System.Threading.Tasks.Task<int> GetPlayerQueue()
        {
            int result = 0;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                result = await GetPlayerQueueCount();
                /*
                int start = 0;
                int end = start + 99;
                int count = 0;
                do
                {
                    count = await GetPlayerQueueCount();
                    if(count>0)
                    {
                        result += count;
                        start = end + 1;
                        end = start + 99;
                    }
                }
                while (count == 99);
                */
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> ClearPlayerQueue()
        {
            bool result = false;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/clear_queue?pid=" + PlayerId );
                if (IsCommandSuccessful("player/clear_queue", response))
                {
                    result = true;
                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> PlayerNext()
        {
            bool result = false;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/play_next?pid=" + PlayerId);
                if (IsCommandSuccessful("player/play_next", response))
                {
                    result = true;
                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> PlayerPrevious()
        {
            bool result = false;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/play_previous?pid=" + PlayerId);
                if (IsCommandSuccessful("player/play_previous", response))
                {
                    result = true;
                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> PlayerVolumeUp()
        {
            bool result = false;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/volume_up?pid=" + PlayerId);
                if (IsCommandSuccessful("player/volume_up", response))
                {
                    result = true;
                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> PlayerVolumeDown()
        {
            bool result = false;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/volume_down?pid=" + PlayerId);
                if (IsCommandSuccessful("player/volume_down", response))
                {
                    result = true;
                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> PlayerVolume()
        {
            bool result = false;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/get_volume?pid=" + PlayerId);
                if (IsCommandSuccessful("player/get_volume", response))
                {
                    result = true;
                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> PlayerSetMute(bool on)
        {
            bool result = false;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/set_mute?pid=" + PlayerId + "&state=" + (on==true?"on":"off"));
                if (IsCommandSuccessful("player/set_mute", response))
                {
                    result = true;
                }
            }
            return result;
        }
        public async System.Threading.Tasks.Task<bool> PlayerSetState(string action)
        {
            bool result = false;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/set_play_state?pid=" + PlayerId + "&state=" + action);
                if (IsCommandSuccessful("player/set_play_state", response))
                {
                    result = true;
                }
            }
            return result;
        }
    }
}
