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
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.System.Threading;
using System.Net.Http.Headers;
using Windows.Web.Http.Filters;

namespace AudioVideoPlayer.DLNA
{


    class DLNADeviceConnectionManager
    {
        const string cMulticastAddress = "239.255.255.250";
        const string cPort = "1900";


        public DatagramSocket mlistener;
        public DatagramSocket mtransmitter;

        Dictionary<string, DLNADevice>  listDLNADevices;
        ThreadPoolTimer DiscoveryTimer;

        public DLNADeviceConnectionManager()
        {

        }
        public async System.Threading.Tasks.Task<string> GetDeviceInformation(string url)
        {
            string result = string.Empty;
            Uri contentUri = null;
            try
            {
                contentUri = new Uri(url);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while creating uri for: " + url + " exception: " + ex.Message);
                return result;
            }

            HttpBaseProtocolFilter RootFilter = new HttpBaseProtocolFilter();

            RootFilter.CacheControl.ReadBehavior = Windows.Web.Http.Filters.HttpCacheReadBehavior.MostRecent;
            RootFilter.CacheControl.WriteBehavior = Windows.Web.Http.Filters.HttpCacheWriteBehavior.NoCache;
            var client = new Windows.Web.Http.HttpClient(RootFilter);
            try
            {
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
        public string GetParameter(string content, string Name)
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
        public async System.Threading.Tasks.Task<bool> ParseMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                string[] sep = { "\r\n" };
                string[] array = message.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                if(array!=null)
                {
                    string Id = string.Empty ;
                    string Location = string.Empty;
                    string Version = string.Empty;
                    string IpCache = string.Empty;
                    string Server = string.Empty;
                    string St = string.Empty;
                    string Usn = string.Empty;
                    string Ip = string.Empty;
                    string friendlyName = string.Empty;
                    string manufacturer = string.Empty;
                    string modelName = string.Empty;
                    string modelNumber = string.Empty;

                    for (int i = 0; i < array.Length;i++)
                    {
                        if (array[i].StartsWith("LOCATION: "))
                        {
                            Location = array[i].Substring(10);
                            if(!string.IsNullOrEmpty(Location))
                            {
                                if(Location.StartsWith("http://"))
                                {
                                    int pos = Location.IndexOf(':',7);
                                    if(pos>0)
                                    {
                                        Ip = Location.Substring(7, pos - 7);
                                    }
                                    string HEOSXmlContent = await GetDeviceInformation(Location);
                                    if(!string.IsNullOrEmpty(HEOSXmlContent))
                                    {
                                        friendlyName = GetParameter(HEOSXmlContent, "friendlyName");
                                        manufacturer = GetParameter(HEOSXmlContent, "manufacturer");
                                        modelName = GetParameter(HEOSXmlContent, "modelName");
                                        modelNumber = GetParameter(HEOSXmlContent, "modelNumber");
                                    }
                                }
                            }
                        }
                        else if (array[i].StartsWith("VERSIONS.UPNP.HEOS.COM: "))
                        {
                            Version = array[i].Substring(24);
                        }
                        else if (array[i].StartsWith("IPCACHE.URL.UPNP.HEOS.COM: "))
                        {
                            IpCache = array[i].Substring(27);
                        }
                        else if (array[i].StartsWith("SERVER: "))
                        {
                            Server = array[i].Substring(8);
                        }
                        else if (array[i].StartsWith("ST: "))
                        {
                            St = array[i].Substring(4);
                        }
                        else if (array[i].StartsWith("USN: "))
                        {
                            Usn = array[i].Substring(5);
                            if(!string.IsNullOrEmpty(Usn))
                            {
                                if(Usn.StartsWith("uuid:"))
                                {
                                    int pos = Usn.IndexOf(':', 5);
                                    if(pos>0)
                                    {
                                        Id = Usn.Substring(6, pos - 6);
                                    }
                                }
                            }
                        }
                    }
                    if ((!string.IsNullOrEmpty(Id))&&
                        (!string.IsNullOrEmpty(friendlyName)))
                    {
                        DLNADevice hs = new DLNADevice(Id, Location, Version, IpCache, Server, St, Usn, Ip, friendlyName, manufacturer, modelName, modelNumber);
                        if (hs != null)
                        {
                            await hs.GetDNLAServices();
                            if (GetUniqueDeviceByID(Id) != null)
                            {
                                listDLNADevices[Id] = hs;
                                OnDLNADeviceUpdated(this, hs);
                            }
                            else
                            {

                                listDLNADevices.Add(Id, hs);
                                OnDLNADeviceAdded(this, hs);
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        async void UDPMulticastMessageReceived(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs eventArguments)
        {
            try
            {
                /*
                using (var writer = new DataWriter(await socket.GetOutputStreamAsync(address, port)))
                {
                    writer.WriteBytes(info.ToByteArray());
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                }*/
                uint stringLength = eventArguments.GetDataReader().UnconsumedBufferLength;
                string message = eventArguments.GetDataReader().ReadString(stringLength);
                await ParseMessage(message);
            }
            catch (Exception exception)
            {
                SocketErrorStatus socketError = SocketError.GetStatus(exception.HResult);
                if (socketError == SocketErrorStatus.ConnectionResetByPeer)
                {
                }
                else if (socketError != SocketErrorStatus.Unknown)
                {
                }
                else
                {
                    throw;
                }
            }
        }
        private async Task<bool> SendMulticast(string buffer)
        {
            try
            {
                HostName mcast = new HostName(cMulticastAddress);
                DataWriter writer = new DataWriter(await mtransmitter.GetOutputStreamAsync(mcast, cPort));

                if (writer != null)
                {
                    writer.WriteString(buffer);
                    uint result = await writer.StoreAsync();
                    bool bresult = await writer.FlushAsync();
                    writer.DetachStream();
                    writer.Dispose();
                    return true;
                }
                /*
                // Connect to the server (in our case the listener we created in previous step).
                IOutputStream os = await mtransmitter.GetOutputStreamAsync(mcast, cPort);
                if (os != null)
                {                    
                    mwriter.WriteString(buffer);
                    uint result = await mwriter.StoreAsync();
                    if (_reportResult != null) _reportResult("Sent Discovery Message: " + buffer);
                    return true;
                }*/

            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    return false;
                }
                return false;
            }
            return false;

        }

        async void UDPMessageReceived(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs eventArguments)
        {
            try
            {
                uint stringLength = eventArguments.GetDataReader().UnconsumedBufferLength;
                string message = eventArguments.GetDataReader().ReadString(stringLength);
                await ParseMessage(message);
            }
            catch (Exception exception)
            {
                SocketErrorStatus socketError = SocketError.GetStatus(exception.HResult);
                if (socketError == SocketErrorStatus.ConnectionResetByPeer)
                {
                }
                else if (socketError != SocketErrorStatus.Unknown)
                {
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Exception while receiving UDP packet:" + exception.Message);
                }
            }
        }

        // Summary:
        //     Initialize DLNADeviceConnectionManager
        public virtual bool Initialize()
        {
            bool result = false;
            try
            {
                if (listDLNADevices == null)
                    listDLNADevices = new Dictionary<string, DLNADevice>();
                mlistener = new DatagramSocket();
                mlistener.MessageReceived += UDPMulticastMessageReceived;

                mtransmitter = new DatagramSocket();
                mtransmitter.MessageReceived += UDPMessageReceived;


                result = true;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while initialising the DLNADeviceConnectionManager: " + ex.Message);
            }
            return result;
        }
        // Summary:
        //     Uninitialize DLNADeviceConnectionManager
        public virtual bool Uninitialize()
        {
            try
            { 

                if (mlistener != null)
                {
                    mlistener.MessageReceived -= UDPMulticastMessageReceived;
                    mlistener = null;
                }
                if (mtransmitter != null)
                {
                    mtransmitter.MessageReceived -= UDPMessageReceived;
                    mtransmitter = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while Uninitialising the DLNADeviceConnectionManager: " + ex.Message);
            }
            return true;
        }


        //
        // Summary:
        //     Launch the Discovery Thread.
        public virtual async System.Threading.Tasks.Task<bool> StartDiscovery()
        {
            bool bResult = false;
            try
            {
                bResult = await SendMulticast("M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nST:urn:schemas-upnp-org:device:MediaRenderer:1\r\nMAN:\"ssdp:discover\"\r\nMX:3\r\n\r\n");

                TimeSpan period = TimeSpan.FromSeconds(5);
                DiscoveryTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        async () =>
                        {
                            await SendMulticast("M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nST:urn:schemas-upnp-org:device:MediaRenderer:1\r\nMAN:\"ssdp:discover\"\r\nMX:3\r\n\r\n");

                        }); ;

                },
                period);
                if ((bResult == true)&&(DiscoveryTimer!=null))
                { 
                    // Clear the current list of devices
                    if (listDLNADevices != null)
                            listDLNADevices.Clear();
                }

            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while starting discovery: " + ex.Message);
            }
            return bResult;
        }
        //
        // Summary:
        //     Stop the Discovery Thread.
        public virtual bool StopDiscovery()
        {
            try
            {
                if (DiscoveryTimer != null)
                {
                    DiscoveryTimer.Cancel();
                    DiscoveryTimer = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while starting discovery: " + ex.Message);
            }

            return false;
        }
        //
        // Summary:
        //     Stop the Discovery Thread.
        public virtual bool IsDiscovering()
        {
            if (DiscoveryTimer != null)
                return true;
            return false;
        }
        protected virtual void OnDLNADeviceAdded(DLNADeviceConnectionManager m, DLNADevice d)
        {
            if (DLNADeviceAdded != null)
                DLNADeviceAdded(m, d);
        }
        //
        // Summary:
        //     The event that is raised when a new Companion Device is discovered.
        public event TypedEventHandler<DLNADeviceConnectionManager, DLNADevice> DLNADeviceAdded;
        protected virtual void OnDLNADeviceRemoved(DLNADeviceConnectionManager m, DLNADevice d)
        {
            if (DLNADeviceRemoved != null)
                DLNADeviceRemoved(m, d);
        }
        //
        // Summary:
        //     The event that is raised when a previously discovered Companion Device
        //     is no longer visible.
        public event TypedEventHandler<DLNADeviceConnectionManager, DLNADevice> DLNADeviceRemoved;
        protected virtual void OnDLNADeviceUpdated(DLNADeviceConnectionManager m, DLNADevice d)
        {
            if (DLNADeviceUpdated != null)
                DLNADeviceUpdated(m, d);
        }
        //
        // Summary:
        //     Raised when a previously discovered Companion Device changes from proximally
        //     connected to cloud connected, or vice versa.
        public event TypedEventHandler<DLNADeviceConnectionManager, DLNADevice> DLNADeviceUpdated;


        // Summary:
        //     Check if the device is connected (a ping have been successful)
        public virtual bool IsDLNADeviceConnected(DLNADevice cd)
        {
            if (listDLNADevices.ContainsKey(cd.Id))
            {
                return (listDLNADevices[cd.Id].Status == DLNADeviceStatus.Connected);
            }
            return false;
        }




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

        #region private


        protected DLNADevice GetUniqueDeviceByName(string Name)
        {
            DLNADevice device = null;
            foreach (var d in listDLNADevices)
            {
                if (string.Equals(d.Value.FriendlyName, Name))
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
        protected DLNADevice GetUniqueDeviceByID(string id)
        {
            DLNADevice device = null;
            foreach (var d in listDLNADevices)
            {
                if (string.Equals(d.Value.Id, id))
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

        #endregion private

    }
}
