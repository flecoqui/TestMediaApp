﻿using System;
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
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;

namespace AudioVideoPlayer.Companion
{

    class MulticastCompanionConnectionManager : CompanionConnectionManager
    {
        public string MulticastIPAddress { get; set; }
        public  int MulticastUDPPort { get; set; }
        public  int UnicastUDPPort { get; set; }
        public bool MulticastDiscovery { get; set; }
        public bool UDPTransport { get; set; }
        public string SendInterfaceAddress { get; set; }
        public MulticastCompanionConnectionManager()
        {            
            MulticastIPAddress = "239.11.11.11";
            MulticastUDPPort = 1919;
            UnicastUDPPort = 1918;
            msocketSend = null;
            msocketRecv = null;
            usocketRecv = null;

        }
        public override bool Uninitialize()
        {
            base.Uninitialize();
            StopRecv();
            StopSend();
            StopDiscovery();
            return true;
        }


        public async System.Threading.Tasks.Task<bool> Initialize(CompanionDevice LocalDevice, MulticastCompanionConnectionManagerInitializeArgs InitArgs)
        {
            await base.Initialize(LocalDevice, InitArgs);
            MulticastIPAddress = InitArgs.MulticastIPAddress;
            MulticastUDPPort = InitArgs.MulticastUDPPort;
            UnicastUDPPort = InitArgs.UnicastUDPPort;
            UDPTransport = InitArgs.UDPTransport;
            MulticastDiscovery = InitArgs.MulticastDiscovery;
            SendInterfaceAddress = InitArgs.SendInterfaceAddress;
            if(await InitializeMulticastRecv())
            {
                if (await InitializeUnicastRecv())
                {
                    if (await InitializeSend())
                        return true;
                }
            }
            return false;
        }
        // Summary:
        //     Launch the Discovery Thread.
        public override async System.Threading.Tasks.Task<bool> StartDiscovery()
        {
            if(MulticastDiscovery==false)
                return await base.StartDiscovery();
            else
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                if (parameters != null)
                {
                    parameters.Add(CompanionProtocol.parameterID, LocalCompanionDevice.Id);
                    parameters.Add(CompanionProtocol.parameterIPAddress, LocalCompanionDevice.IPAddress);
                    parameters.Add(CompanionProtocol.parameterKind, LocalCompanionDevice.Kind);
                    parameters.Add(CompanionProtocol.parameterName, LocalCompanionDevice.Name);
                    string message = CompanionProtocol.CreateCommand(CompanionProtocol.commandPing, parameters);
                    return await Send(MulticastIPAddress, message);
                }
            }
            return false;
        }
        // Summary:
        //     Stop the Discovery Thread.
        public override  bool StopDiscovery()
        {
            if (MulticastDiscovery == false)
                return base.StopDiscovery();
            else
            {
                return true;
            }
        }
        //
        // Summary:
        //     Stop the Discovery Thread.
        public override bool IsDiscovering()
        {
            if (MulticastDiscovery == false)
                return base.IsDiscovering();
            else
            {
                return true;
            }
        }
        public override  async System.Threading.Tasks.Task<bool> Send(CompanionDevice cd, string Message)
        {
            if (cd != null)
            {
                bool bMulticast = false;
                if ((!string.IsNullOrEmpty(cd.IPAddress)) && string.Equals(cd.IPAddress, MulticastIPAddress))
                    bMulticast = true;
                if ((UDPTransport == false)&&(bMulticast==false))
                    return await base.Send(cd, Message);
                else
                {
                    if (!string.IsNullOrEmpty(cd.IPAddress))
                        return await Send(cd.IPAddress, Message);
                }
            }
            return false;
        }

        //
        // Summary:
        //     The event that is raised when a new Companion Device is discovered.
        public  override event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceAdded;
        //
        // Summary:
        //     The event that is raised when a previously discovered Companion Device
        //     is no longer visible.
        public override event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceRemoved;
        //
        // Summary:
        //     Raised when a previously discovered Companion Device changes from proximally
        //     connected to cloud connected, or vice versa.
        public override event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceUpdated;
        //
        // Summary:
        //     Raised when a Message is received from a Companion Device
        public override event TypedEventHandler<CompanionDevice, String> MessageReceived;

        #region private
        private DatagramSocket msocketSend;
        private DatagramSocket msocketRecv;
        private DatagramSocket usocketRecv;
        async Task<bool> Send(string ip, string Message)
        {
            try
            {
                if (msocketSend == null)
                    await InitializeSend();

                // Add Device Information
                string command = CompanionProtocol.GetCommandFromMessage(Message);
                Dictionary<string,string> parameters = CompanionProtocol.GetParametersFromMessage(Message);
                if (!string.IsNullOrEmpty(command))
                {
                    if (parameters == null)
                        parameters = new Dictionary<string, string>();
                    if ((parameters != null) && (!parameters.ContainsKey(CompanionProtocol.parameterIPAddress))
                        && (!parameters.ContainsKey(CompanionProtocol.parameterName))
                        && (!parameters.ContainsKey(CompanionProtocol.parameterKind))
                        && (!parameters.ContainsKey(CompanionProtocol.parameterID)))
                    {
                        parameters.Add(CompanionProtocol.parameterID, LocalCompanionDevice.Id);
                        parameters.Add(CompanionProtocol.parameterIPAddress, LocalCompanionDevice.IPAddress);
                        parameters.Add(CompanionProtocol.parameterKind, LocalCompanionDevice.Kind);
                        parameters.Add(CompanionProtocol.parameterName, LocalCompanionDevice.Name);

                        Message = CompanionProtocol.CreateCommand(command, parameters);
                    }
                }
                HostName mcast;
                string port;
                if ((string.IsNullOrEmpty(ip)) || (string.Equals(ip, MulticastIPAddress)))
                {
                    port = MulticastUDPPort.ToString();
                    mcast = new HostName(MulticastIPAddress);
                }
                else
                {
                    port = UnicastUDPPort.ToString();
                    mcast = new HostName(ip);
                }
                DataWriter writer = new DataWriter(await msocketSend.GetOutputStreamAsync(mcast, port));

                if (writer != null)
                {
                    writer.WriteString(Message);
                    uint result = await writer.StoreAsync();
                    bool bresult = await writer.FlushAsync();
                    writer.DetachStream();
                    writer.Dispose();
                    System.Diagnostics.Debug.WriteLine("Message Sent to: " + mcast.CanonicalName + ":" + port + " content: " + Message);
                    return true;
                }
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
        CompanionDevice GetCompanionDeviceFromParameters(Dictionary<string, string> Parameters)
        {
            CompanionDevice d = null;
            if ((Parameters != null) && (Parameters.ContainsKey(CompanionProtocol.parameterName)) &&
                (Parameters.ContainsKey(CompanionProtocol.parameterIPAddress)) &&
                (Parameters.ContainsKey(CompanionProtocol.parameterKind)) &&
                (Parameters.ContainsKey(CompanionProtocol.parameterID)) 
                )
            {
                string Name = Parameters[CompanionProtocol.parameterName];
                if (Name != null)
                {
                    string IP = Parameters[CompanionProtocol.parameterIPAddress];
                    if (IP != null)
                    {
                        string Kind = Parameters[CompanionProtocol.parameterKind];
                        if (Kind != null)
                        {
                            string Id = Parameters[CompanionProtocol.parameterID];
                            if (Id != null)
                            {
                                d = new CompanionDevice(Id, Name, IP, Kind);
                            }
                        }
                    }
                }
            }
            return d;
        }
        async void UDPMulticastMessageReceived(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs eventArguments)
        {
            try
            {

                uint stringLength = eventArguments.GetDataReader().UnconsumedBufferLength;
                string message = eventArguments.GetDataReader().ReadString(stringLength);
                string Command = CompanionProtocol.GetCommandFromMessage(message);
                Dictionary<string, string> Parameters = CompanionProtocol.GetParametersFromMessage(message);
                CompanionDevice d = GetCompanionDeviceFromParameters(Parameters);

                if (string.Equals(Command, CompanionClient.commandPingResponse))
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if(d!=null)
                        {

                            var searchDevice = listCompanionDevices.FirstOrDefault(device => string.Equals(device.Value.Name, d.Name) && string.Equals(device.Value.IPAddress, d.IPAddress));
                            if (searchDevice.Value == null)
                            {
                                // Add Device
                                d.Status = CompanionDeviceStatus.Connected;
                                listCompanionDevices.Add(d.Id, d);
                                if (CompanionDeviceAdded != null)
                                    CompanionDeviceAdded(this, d);

                            }
                            else
                            {
                                d.Id  = searchDevice.Value.Id;
                                d.Status = CompanionDeviceStatus.Connected;
                                listCompanionDevices.Remove(d.Id);
                                listCompanionDevices.Add(d.Id, d);
                                // Update device
                                if (CompanionDeviceUpdated != null)
                                    CompanionDeviceUpdated(this, d);
                            }
                        }
                    });
                }
                else if (string.Equals(Command, CompanionClient.commandPing))
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if (d != null)
                        {

                            var searchDevice = listCompanionDevices.FirstOrDefault(device => string.Equals(device.Value.Name, d.Name) && string.Equals(device.Value.IPAddress, d.IPAddress));
                            if (searchDevice.Value == null)
                            {
                                d.Status = CompanionDeviceStatus.Connected;
                                listCompanionDevices.Add(d.Id, d);
                            }
                            else
                            {
                                // Update device

                            }
                        }
                    });
                }
                else
                {
                    // Forward the Message
                    if (MessageReceived!=null)
                        MessageReceived(d, message);
                }


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
        NetworkAdapter GetDefaultNetworkAdapter()
        {
            NetworkAdapter adapter = null;
            if (!string.IsNullOrEmpty(SendInterfaceAddress))
                adapter = GetNetworkAdapterByIPAddress(SendInterfaceAddress);
            if (adapter == null)
            {
                ConnectionProfile connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                if (connectionProfile != null)
                    adapter = connectionProfile.NetworkAdapter;
            }
            return adapter;

        }
        static NetworkAdapter GetNetworkAdapterByIPAddress(string ipAddress)
        {

            NetworkAdapter adapter = null;
            var hostnames = NetworkInformation.GetHostNames();

            foreach (var hn in hostnames)
            {

                if (hn.IPInformation != null && hn.DisplayName == ipAddress)
                {
                    adapter = hn.IPInformation.NetworkAdapter;
                    break;
                }
            }
            return adapter;
        }

        async Task<bool> InitializeSend()
        {
            bool result = false;
            try
            {
                if (msocketSend != null)
                {
                    //    msocketSend.MessageReceived -= UDPMulticastMessageReceived;
                    msocketSend.Dispose();
                    msocketSend = null;
                }
                msocketSend = new DatagramSocket();
                //  msocketSend.MessageReceived += UDPMulticastMessageReceived;

                NetworkAdapter adapter = GetDefaultNetworkAdapter();
                if (adapter != null)
                    await msocketSend.BindServiceNameAsync("", adapter);
                result = true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception while initializing: " + e.Message);
                result = false;
            }
            return result;

        }

        async Task<bool> InitializeMulticastRecv()
        {

            try
            {
                if (msocketRecv != null)
                {
                    msocketRecv.MessageReceived -= UDPMulticastMessageReceived;
                    msocketRecv.Dispose();
                    msocketRecv = null;
                }
                msocketRecv = new DatagramSocket();
                msocketRecv.Control.MulticastOnly = true;
                msocketRecv.MessageReceived += UDPMulticastMessageReceived;
                await msocketRecv.BindServiceNameAsync(MulticastUDPPort.ToString());
                HostName mcast = new HostName(MulticastIPAddress);
                msocketRecv.JoinMulticastGroup(mcast);
                return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception while listening: " + e.Message);
            }
            return false;
        }
        bool IsMulticastReceiving()
        {
            return (usocketRecv != null);
        }
        async Task<bool> InitializeUnicastRecv()
        {
            try
            {

                if (usocketRecv != null)
                {
                    usocketRecv.MessageReceived -= UDPMulticastMessageReceived;
                    usocketRecv.Dispose();
                    usocketRecv = null;
                }
                usocketRecv = new DatagramSocket();
                usocketRecv.MessageReceived += UDPMulticastMessageReceived;
                await usocketRecv.BindServiceNameAsync(UnicastUDPPort.ToString());
                return true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception while listening: " + e.Message);
            }
            return false;
        }
        bool IsUnicastReceiving()
        {
            return (usocketRecv != null);
        }
        void StopRecv()
        {
            try
            {
                if (msocketRecv != null)
                {
                    msocketRecv.MessageReceived -= UDPMulticastMessageReceived;
                    msocketRecv.Dispose();
                    msocketRecv = null;
                }
                if (usocketRecv != null)
                {
                    usocketRecv.MessageReceived -= UDPMulticastMessageReceived;
                    usocketRecv.Dispose();
                    usocketRecv = null;
                }

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception while stopping listening: " + e.Message);
            }
        }
        void StopSend()
        {
            try
            {
                if (msocketSend != null)
                {
                    //    msocketSend.MessageReceived -= UDPMulticastMessageReceived;
                    msocketSend.Dispose();
                    msocketSend = null;
                }

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception while stopping listening: " + e.Message);
            }
        }
        #endregion private

    }
}