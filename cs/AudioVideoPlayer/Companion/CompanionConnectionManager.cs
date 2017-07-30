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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.System;
using Windows.System.RemoteSystems;
using CompanionService;

namespace AudioVideoPlayer.Companion
{


    class CompanionConnectionManager
    {
        public CompanionDevice LocalCompanionDevice { get; set; }

        public string ApplicationUri { get; set; }
        public string AppServiceName { get; set; }
        public string PackageFamilyName { get; set; }



        // Summary:
        //     CompanionConnectionManager constructor
        public CompanionConnectionManager()
        {
            LocalCompanionDevice = null;
            ApplicationUri = string.Empty;
            AppServiceName = string.Empty;
            PackageFamilyName = string.Empty;
        }
        // Summary:
        //     Initialize CompanionConnectionManager
        public virtual async System.Threading.Tasks.Task<bool> Initialize(CompanionDevice LocalDevice, CompanionConnectionManagerInitializeArgs InitArgs)
        {
            
            LocalCompanionDevice = LocalDevice;
            ApplicationUri = InitArgs.ApplicationUri;
            AppServiceName = InitArgs.AppServiceName;
            PackageFamilyName = InitArgs.PackageFamilyName;
            if (listCompanionDevices == null)
                listCompanionDevices = new Dictionary<string, CompanionDevice>();
            if (listRemoteSystems == null)
                listRemoteSystems = new Dictionary<string, RemoteSystem>();
            //Set up a new app service connection
            if (receiveConnection != null)
            {
                receiveConnection.ServiceClosed -= ReceiveConnection_ServiceClosed;
                receiveConnection.RequestReceived -= ReceiveConnection_RequestReceived;
                receiveConnection = null;
            }
            receiveConnection = new AppServiceConnection();
            if (receiveConnection != null)
            {
                receiveConnection.AppServiceName = AppServiceName;
                receiveConnection.PackageFamilyName = PackageFamilyName;
                receiveConnection.RequestReceived += ReceiveConnection_RequestReceived;
                receiveConnection.ServiceClosed += ReceiveConnection_ServiceClosed;
                Windows.ApplicationModel.AppService.AppServiceConnectionStatus status = await receiveConnection.OpenAsync();
                if (status == Windows.ApplicationModel.AppService.AppServiceConnectionStatus.Success)
                {
                    // Connection established with the background task
                    var inputs = new ValueSet();
                    inputs.Add(CompanionServiceMessage.ATT_TYPE, CompanionServiceMessage.TYPE_INIT);
                    AppServiceResponse response = await receiveConnection.SendMessageAsync(inputs);
                    if ((response != null) && (response.Status == AppServiceResponseStatus.Success))
                    {
                        if ((response.Message != null) && (response.Message.ContainsKey(CompanionServiceMessage.ATT_TYPE)))
                        {
                            string s = (string)response.Message[CompanionServiceMessage.ATT_TYPE];
                            if (string.Equals(s, CompanionServiceMessage.TYPE_RESULT))
                                return true;
                        }
                    }
                }
            }
            return false;
        }
        // Summary:
        //     Uninitialize CompanionConnectionManager
        public virtual bool Uninitialize()
        {

            //Set up a new app service connection
            if (receiveConnection != null)
            {
                receiveConnection.ServiceClosed -= ReceiveConnection_ServiceClosed;
                receiveConnection.RequestReceived -= ReceiveConnection_RequestReceived;
                receiveConnection = null;
            }
            return true;
        }


        private async void ReceiveConnection_RequestReceived(Windows.ApplicationModel.AppService.AppServiceConnection sender, Windows.ApplicationModel.AppService.AppServiceRequestReceivedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("ReceiveConnection_RequestReceived");
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                var messageDeferral = args.GetDeferral();
                try
                {
                    if ((args.Request != null) && (args.Request.Message != null))
                    {
                        var inputs = args.Request.Message;
                        if (inputs.ContainsKey(CompanionServiceMessage.ATT_TYPE))
                        {
                            string s = (string)inputs[CompanionServiceMessage.ATT_TYPE];
                            if (string.Equals(s, CompanionServiceMessage.TYPE_DATA))
                            {
                                if ((inputs.ContainsKey(CompanionServiceMessage.ATT_SOURCEID)) &&
                                (inputs.ContainsKey(CompanionServiceMessage.ATT_SOURCEID)) &&
                                (inputs.ContainsKey(CompanionServiceMessage.ATT_SOURCEIP)) &&
                                (inputs.ContainsKey(CompanionServiceMessage.ATT_SOURCEKIND)) &&
                                (inputs.ContainsKey(CompanionServiceMessage.ATT_SOURCENAME)) &&
                                (inputs.ContainsKey(CompanionServiceMessage.ATT_MESSAGE)))
                                {
                                    string id = (string)inputs[CompanionServiceMessage.ATT_SOURCEID];
                                    string name = (string)inputs[CompanionServiceMessage.ATT_SOURCENAME];
                                    string ip = (string)inputs[CompanionServiceMessage.ATT_SOURCEIP];
                                    string kind = (string)inputs[CompanionServiceMessage.ATT_SOURCEKIND];
                                    string message = (string)inputs[CompanionServiceMessage.ATT_MESSAGE];
                                    CompanionDevice d = new CompanionDevice(id, name, ip, kind);
                                    if(!string.IsNullOrEmpty(ip))
                                    {
                                        CompanionDevice a = GetUniqueDeviceByName(name);
                                        if (a != null)
                                        {
                                            if (!string.Equals(a.IPAddress, ip))
                                            {
                                                a.IPAddress = ip;
                                                listCompanionDevices.Remove(a.Id);
                                                listCompanionDevices.Add(id, a);
                                                if (CompanionDeviceUpdated != null)
                                                    CompanionDeviceUpdated(this, a);
                                            }
                                        }
                                    }
                                    if (MessageReceived != null)
                                        MessageReceived(d, message);
                                }
                            }
                            else if (string.Equals(s, CompanionServiceMessage.TYPE_INIT))
                            {
                                // Background task started
                            }
                        }
                    }
                }
                finally
                {
                    //Complete the message deferral so the platform knows we're done responding
                    messageDeferral.Complete();
                }
            });
        }
        private async void ReceiveConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //Dispose the connection reference we're holding
                if (receiveConnection != null)
                {
                    receiveConnection.ServiceClosed -= ReceiveConnection_ServiceClosed;
                    receiveConnection.RequestReceived -= ReceiveConnection_RequestReceived;
                    receiveConnection.Dispose();
                    receiveConnection = null;
                }
            });
        }
        //
        // Summary:
        //     Launch the Discovery Thread.
        public virtual async System.Threading.Tasks.Task<bool> StartDiscovery()
        {
            // Verify access for Remote Systems. 
            // Note: RequestAccessAsync needs to called from the UI thread.
            RemoteSystemAccessStatus accessStatus = await RemoteSystem.RequestAccessAsync();

            if (accessStatus == RemoteSystemAccessStatus.Allowed)
            {
                // Stop Discovery service if it was running
                StopDiscovery();
                // Clear the current list of devices
                if(listCompanionDevices!=null)listCompanionDevices.Clear();
                if(listRemoteSystems!=null) listRemoteSystems.Clear();

                // Build a watcher to continuously monitor for all remote systems.
                remoteSystemWatcher = RemoteSystem.CreateWatcher();
                if (remoteSystemWatcher != null)
                {
                    // Subscribing to the event that will be raised when a new remote system is found by the watcher.
                    remoteSystemWatcher.RemoteSystemAdded += RemoteSystemWatcher_RemoteSystemAdded;

                    // Subscribing to the event that will be raised when a previously found remote system is no longer available.
                    remoteSystemWatcher.RemoteSystemRemoved += RemoteSystemWatcher_RemoteSystemRemoved;

                    // Subscribing to the event that will be raised when a previously found remote system is updated.
                    remoteSystemWatcher.RemoteSystemUpdated += RemoteSystemWatcher_RemoteSystemUpdated;

                    // Start the watcher.
                    remoteSystemWatcher.Start();
                    return true;
                }
            }
            return false;
        }
        private void RemoteSystemWatcher_RemoteSystemUpdated(RemoteSystemWatcher sender, RemoteSystemUpdatedEventArgs args)
        {
            CompanionDevice d = new CompanionDevice(args.RemoteSystem.Id, args.RemoteSystem.DisplayName,string.Empty, args.RemoteSystem.Kind);
            if ((d != null)&&(listCompanionDevices !=null )&&(listRemoteSystems!=null))
            {
                d.IsAvailableByProximity = args.RemoteSystem.IsAvailableByProximity;
                d.IsAvailableBySpatialProximity = args.RemoteSystem.IsAvailableBySpatialProximity;
                d.Status = CompanionDeviceStatus.Available;
                if (listCompanionDevices.ContainsKey(args.RemoteSystem.Id))
                    listCompanionDevices.Remove(args.RemoteSystem.Id);
                listCompanionDevices.Add(args.RemoteSystem.Id,d);
                if (CompanionDeviceUpdated != null)
                    CompanionDeviceUpdated(this,d);

                if (listRemoteSystems.ContainsKey(args.RemoteSystem.Id))
                    listRemoteSystems.Remove(args.RemoteSystem.Id);
                listRemoteSystems.Add(args.RemoteSystem.Id, args.RemoteSystem);

            }
        }

        private void RemoteSystemWatcher_RemoteSystemRemoved(RemoteSystemWatcher sender, RemoteSystemRemovedEventArgs args)
        {
            if (listCompanionDevices != null)
            {
                if (listCompanionDevices.ContainsKey(args.RemoteSystemId))
                {
                    CompanionDevice d = listCompanionDevices[args.RemoteSystemId];
                    if (CompanionDeviceRemoved != null)
                        CompanionDeviceRemoved(this, d);
                    listCompanionDevices.Remove(args.RemoteSystemId);
                }
            }
            if (listRemoteSystems != null)
            {
                if (listRemoteSystems.ContainsKey(args.RemoteSystemId))
                {
                    listRemoteSystems.Remove(args.RemoteSystemId);
                }
            }
        }

        private void RemoteSystemWatcher_RemoteSystemAdded(RemoteSystemWatcher sender, RemoteSystemAddedEventArgs args)
        {
            CompanionDevice d = new CompanionDevice(args.RemoteSystem.Id, args.RemoteSystem.DisplayName, string.Empty, args.RemoteSystem.Kind);
            if ((d != null) && (listCompanionDevices != null) && (listRemoteSystems != null))
            {
                d.IsAvailableByProximity = args.RemoteSystem.IsAvailableByProximity;
                d.IsAvailableBySpatialProximity = args.RemoteSystem.IsAvailableBySpatialProximity;
                d.Status = CompanionDeviceStatus.Available;
                if (listCompanionDevices.ContainsKey(args.RemoteSystem.Id))
                    listCompanionDevices.Remove(args.RemoteSystem.Id);
                listCompanionDevices.Add(args.RemoteSystem.Id, d);
                if (CompanionDeviceAdded != null)
                    CompanionDeviceAdded(this, d);

                if (listRemoteSystems.ContainsKey(args.RemoteSystem.Id))
                    listRemoteSystems.Remove(args.RemoteSystem.Id);
                listRemoteSystems.Add(args.RemoteSystem.Id, args.RemoteSystem);
            }
        }

        //
        // Summary:
        //     Stop the Discovery Thread.
        public virtual bool StopDiscovery()
        {
            if(remoteSystemWatcher!=null)
            {
                remoteSystemWatcher.Stop();
                remoteSystemWatcher.RemoteSystemAdded -= RemoteSystemWatcher_RemoteSystemAdded;
                remoteSystemWatcher.RemoteSystemRemoved -= RemoteSystemWatcher_RemoteSystemRemoved;
                remoteSystemWatcher.RemoteSystemUpdated -= RemoteSystemWatcher_RemoteSystemUpdated;
                remoteSystemWatcher = null;
            }
            return true;
        }
        //
        // Summary:
        //     Stop the Discovery Thread.
        public virtual bool IsDiscovering()
        {
            return (remoteSystemWatcher != null);
        }
        //
        // Summary:
        //     The event that is raised when a new Companion Device is discovered.
        public virtual event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceAdded;
        //
        // Summary:
        //     The event that is raised when a previously discovered Companion Device
        //     is no longer visible.
        public virtual event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceRemoved;
        //
        // Summary:
        //     Raised when a previously discovered Companion Device changes from proximally
        //     connected to cloud connected, or vice versa.
        public virtual event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceUpdated;

        // Summary:
        //     Check if the device is connected, if not send a request to launch the application on the remote device
        public virtual async System.Threading.Tasks.Task<bool> CheckCompanionDeviceConnected(CompanionDevice cd)
        {
            if ((listCompanionDevices.ContainsKey(cd.Id)) && (listRemoteSystems.ContainsKey(cd.Id)))
            {
                if (listCompanionDevices[cd.Id].Status != CompanionDeviceStatus.Connected)
                {
                    RemoteSystemConnectionRequest rscr = new RemoteSystemConnectionRequest(listRemoteSystems[cd.Id]);
                    if (rscr != null)
                    {
                        Uri uri;
                        if (Uri.TryCreate(ApplicationUri, UriKind.Absolute, out uri))
                        {
                            RemoteLaunchUriStatus launchUriStatus = await Windows.System.RemoteLauncher.LaunchUriAsync(rscr, uri);
                            if (launchUriStatus == RemoteLaunchUriStatus.Success)
                                listCompanionDevices[cd.Id].Status = CompanionDeviceStatus.Connected;
                        }
                    }
                }
                if (listCompanionDevices[cd.Id].Status == CompanionDeviceStatus.Connected)
                    return true; 
            }
            return false;
        }

        // Summary:
        //     Check if the device is connected (a ping have been successful)
        public virtual bool IsCompanionDeviceConnected(CompanionDevice cd)
        {
            if ((listCompanionDevices.ContainsKey(cd.Id)) && (listRemoteSystems.ContainsKey(cd.Id)))
            {
                return (listCompanionDevices[cd.Id].Status == CompanionDeviceStatus.Connected);
            }
            return false;
        }
        // Summary:
        //     Open a uri on the emote device

        public virtual async System.Threading.Tasks.Task<bool> CompanionDeviceOpenUri(CompanionDevice cd, string inputUri)
        {
            if ((listCompanionDevices.ContainsKey(cd.Id)) && (listRemoteSystems.ContainsKey(cd.Id)))
            {
                RemoteSystemConnectionRequest rscr = new RemoteSystemConnectionRequest(listRemoteSystems[cd.Id]);
                if (rscr != null)
                {
                    Uri uri;
                    if (Uri.TryCreate(inputUri, UriKind.Absolute, out uri))
                    {
                        RemoteLaunchUriStatus launchUriStatus = await Windows.System.RemoteLauncher.LaunchUriAsync(rscr, uri);
                        if (launchUriStatus == RemoteLaunchUriStatus.Success)
                        {
                            listCompanionDevices[cd.Id].Status = CompanionDeviceStatus.Connected;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public virtual async System.Threading.Tasks.Task<bool> Send(CompanionDevice cd, string Message)
        {
            if (cd != null)
            {

                if (await CheckCompanionDeviceConnected(cd))
                {
                    if (listRemoteSystems.ContainsKey(cd.Id))
                    {
                        RemoteSystemConnectionRequest rscr = new RemoteSystemConnectionRequest(listRemoteSystems[cd.Id]);
                        if (rscr != null)
                        { 
                            using (var connection = new AppServiceConnection())
                            {
                                //Set up a new app service connection
                                connection.AppServiceName = AppServiceName;
                                connection.PackageFamilyName = PackageFamilyName;


                                AppServiceConnectionStatus status = await connection.OpenRemoteAsync(rscr);
                                if (status == AppServiceConnectionStatus.Success)
                                {
                                    //Set up the inputs and send a message to the service
                                    var inputs = new ValueSet();
                                    inputs.Add(CompanionServiceMessage.ATT_TYPE, CompanionServiceMessage.TYPE_DATA);
                                    inputs.Add(CompanionServiceMessage.ATT_SOURCEID, GetSourceId());
                                    inputs.Add(CompanionServiceMessage.ATT_SOURCENAME, GetSourceName());
                                    inputs.Add(CompanionServiceMessage.ATT_SOURCEIP, GetSourceIP());
                                    inputs.Add(CompanionServiceMessage.ATT_SOURCEKIND, GetSourceKind());
                                    inputs.Add(CompanionServiceMessage.ATT_MESSAGE, Message);
                                    AppServiceResponse response = await connection.SendMessageAsync(inputs);
                                    if ((response!=null) &&(response.Status == AppServiceResponseStatus.Success))
                                    {
                                        if ((response.Message != null) && (response.Message.ContainsKey(CompanionServiceMessage.ATT_TYPE)))
                                        {
                                            string s = (string) response.Message[CompanionServiceMessage.ATT_TYPE];
                                            if(string.Equals(s, CompanionServiceMessage.TYPE_RESULT))
                                                return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        //
        // Summary:
        //     Raised when a Message is received from a Companion Device
        public virtual event TypedEventHandler<CompanionDevice, String> MessageReceived;


        public static bool IsIPv4Address(string s)
        {
            try
            {
                if (!string.IsNullOrEmpty(s))
                {
                    char[] sep = { '.' };
                    string[] arrayIP = s.Split(sep);
                    if (arrayIP.Count() == 4)
                    {
                        for (int i = 0; i < arrayIP.Count(); i++)
                        {
                            int digit = int.Parse(arrayIP[i]);
                            if ((digit < 0) || (digit > 255))
                                return false;
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception checking IP Address: " + ex.Message);
            }
            return false;
        }
        public string GetSourceIP()
        {
            if (LocalCompanionDevice != null)
            {
                if (!string.IsNullOrEmpty(LocalCompanionDevice.IPAddress))
                    return LocalCompanionDevice.IPAddress;
                else
                {
                    var icp = NetworkInformation.GetInternetConnectionProfile();

                    if (icp?.NetworkAdapter == null) return null;

                    var hostnames = NetworkInformation.GetHostNames();

                    foreach (var hn in hostnames)
                    {
                        if ((hn.IPInformation != null) && (hn.IPInformation.NetworkAdapter != null) && (hn.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId))
                        {
                            if (IsIPv4Address(hn.CanonicalName))
                            {
                                LocalCompanionDevice.IPAddress = hn.CanonicalName;
                                return hn.CanonicalName;
                            }
                        }
                    }
                }
            }
            return null;
        }

        #region private
        protected RemoteSystemWatcher remoteSystemWatcher;
        protected AppServiceConnection receiveConnection;

        protected Dictionary<string, CompanionDevice> listCompanionDevices;
        protected Dictionary<string, RemoteSystem> listRemoteSystems;

        protected CompanionDevice GetUniqueDeviceByName(string Name)
        {
            CompanionDevice device = null;
            foreach (var d in listCompanionDevices)
            {
                if (string.Equals(d.Value.Name, Name))
                {
                    if (device == null)
                        device = d.Value;
                    else
                        // not unique
                        return null;
                }
            }
            return device;
        }
        protected string GetSourceId()
        {
            if (LocalCompanionDevice != null)
            {
                if (!string.IsNullOrEmpty(LocalCompanionDevice.Id))
                    return LocalCompanionDevice.Id;
                else
                {
                    foreach (var d in listRemoteSystems)
                    {
                        if (d.Value.DisplayName == LocalCompanionDevice.Name)
                        {
                            LocalCompanionDevice.Id = d.Key;
                            return d.Key;
                        }
                    }
                }
            }
            return string.Empty;
        }
        protected string GetSourceName()
        {
            if (LocalCompanionDevice != null)
            {
                if (!string.IsNullOrEmpty(LocalCompanionDevice.Name))
                    return LocalCompanionDevice.Name;
            }
            return string.Empty;
        }
        protected string GetSourceKind()
        {
            if (LocalCompanionDevice != null)
            {
                if (!string.IsNullOrEmpty(LocalCompanionDevice.Kind))
                    return LocalCompanionDevice.Kind;
            }
            return string.Empty;
        }

        #endregion private

    }
}
