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
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;
using Windows.System.Threading;

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


        public override async System.Threading.Tasks.Task<bool> Initialize(CompanionDevice LocalDevice, CompanionConnectionManagerInitializeArgs InitArgs)
        {
            await base.Initialize(LocalDevice, InitArgs);
            MulticastCompanionConnectionManagerInitializeArgs mInitArgs = InitArgs as MulticastCompanionConnectionManagerInitializeArgs;
            if (mInitArgs != null)
            {
                MulticastIPAddress = mInitArgs.MulticastIPAddress;
                MulticastUDPPort = mInitArgs.MulticastUDPPort;
                UnicastUDPPort = mInitArgs.UnicastUDPPort;
                UDPTransport = mInitArgs.UDPTransport;
                MulticastDiscovery = mInitArgs.MulticastDiscovery;
                SendInterfaceAddress = mInitArgs.SendInterfaceAddress;
                if (await InitializeMulticastRecv())
                {
                    if (await InitializeUnicastRecv())
                    {
                        if (await InitializeSend())
                        {
                            var searchDevice = listCompanionDevices.FirstOrDefault(device => string.Equals(device.Value.IPAddress, MulticastIPAddress));
                            if (searchDevice.Value == null)
                            {
                                CompanionDevice d = new CompanionDevice();
                                d.Id = "0";
                                d.IsRemoteSystemDevice = false;
                                d.IPAddress = MulticastIPAddress;
                                d.Name = CompanionProtocol.MulticastDeviceName;
                                d.Kind = CompanionProtocol.MulticastDeviceKind;
                                d.IsMulticast = true;
                                if (listCompanionDevices.ContainsKey(d.Id))
                                    listCompanionDevices.Remove(d.Id);
                                listCompanionDevices.Add(d.Id, d);
                                OnDeviceAdded(this, d);

                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        // Summary:
        //     Send Multicast ping
        async System.Threading.Tasks.Task<bool> SendPing()
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
            return false;
        }
        // Summary:
        //     Launch the Discovery Thread.
        public override async System.Threading.Tasks.Task<bool> StartDiscovery()
        {
            if (await base.StartDiscovery() == true)
            {
                if (MulticastDiscovery == true)
                {
                    await SendPing();
                    TimeSpan period = TimeSpan.FromSeconds(10);
                    DiscoveryTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                            async () =>
                            {
                                 await SendPing();

                            }); ;

                    },
                    period);
                    if (DiscoveryTimer != null)
                        return true;

                }
            }
            return false;
        }



        // Summary:
        //     Stop the Discovery Thread.
        public override  bool StopDiscovery()
        {
            base.StopDiscovery();
            if (MulticastDiscovery == true)
            {
                if (DiscoveryTimer!=null)
                {
                    DiscoveryTimer.Cancel();
                    DiscoveryTimer = null;
                }
            }
            return true;
        }
        //
        // Summary:
        //     Stop the Discovery Thread.
        public override bool IsDiscovering()
        {
            if (MulticastDiscovery == false)
                return base.IsDiscovering();
            else 
                return (DiscoveryTimer!=null)|| base.IsDiscovering();
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
        public static NetworkAdapter GetDefaultNetworkAdapter(string InterfaceAddress)
        {
            NetworkAdapter adapter = null;
            if (!string.IsNullOrEmpty(InterfaceAddress))
                adapter = GetNetworkAdapterByIPAddress(InterfaceAddress);
            if (adapter == null)
            {
                ConnectionProfile connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                if (connectionProfile != null)
                    adapter = connectionProfile.NetworkAdapter;
            }
            return adapter;

        }
        public static string GetNetworkAdapterIPAddress()
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) return null;

            var hostnames = NetworkInformation.GetHostNames();

            foreach (var hn in hostnames)
            {
                if ((hn.IPInformation != null) && (hn.IPInformation.NetworkAdapter != null) && (hn.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId))
                {
                    if (IsIPv4Address(hn.CanonicalName))
                        return hn.CanonicalName;
                    //                    System.Diagnostics.Debug.WriteLine("IP Address: " + hn.CanonicalName);
                }
            }
            return null;
        }
        public static NetworkAdapter GetNetworkAdapterByIPAddress(string ipAddress)
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
        //
        // Summary:
        //     The event that is raised when a new Companion Device is discovered.
        //public  override event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceAdded;
        //
        // Summary:
        //     The event that is raised when a previously discovered Companion Device
        //     is no longer visible.
        //public override event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceRemoved;
        //
        // Summary:
        //     Raised when a previously discovered Companion Device changes from proximally
        //     connected to cloud connected, or vice versa.
        //public override event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceUpdated;
        //
        // Summary:
        //     Raised when a Message is received from a Companion Device
        public override event TypedEventHandler<CompanionDevice, String> MessageReceived;

        #region private
        private DatagramSocket msocketSend;
        private DatagramSocket msocketRecv;
        private DatagramSocket usocketRecv;
        ThreadPoolTimer DiscoveryTimer;
        async Task<bool> Send(string ip, string Message)
        {
            try
            {
                if (msocketSend == null)
                    return false;

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
                                d = new CompanionDevice(Id, false,Name, IP, Kind);
                            }
                        }
                    }
                }
            }
            return d;
        }
        void UDPUnicastMessageReceived(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs eventArguments)
        {
            System.Diagnostics.Debug.WriteLine("Unicast message received from " + eventArguments.RemoteAddress);
            UDPMulticastMessageReceived(socket, eventArguments);
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
                System.Diagnostics.Debug.WriteLine("Received command: " + message);

                if (string.Equals(Command, CompanionProtocol.commandPingResponse))
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if(d!=null)
                        {
                            var searchDevice = listCompanionDevices.FirstOrDefault(device => string.Equals(device.Value.Name, d.Name) );
                            if (searchDevice.Value == null)
                            {
                                // Add Device
                                d.Status = CompanionDeviceStatus.Connected;
                                if (string.IsNullOrEmpty(d.Id))
                                    d.Id = Guid.NewGuid().ToString(); 
                                if (listCompanionDevices.ContainsKey(d.Id))
                                    listCompanionDevices.Remove(d.Id);
                                listCompanionDevices.Add(d.Id, d);
                                OnDeviceAdded(this, d);

                            }
                            else
                            {
                                if (!string.Equals(d.IPAddress, searchDevice.Value.IPAddress))
                                {
                                    d.Id = searchDevice.Value.Id;
                                    d.Status = CompanionDeviceStatus.Connected;
                                    listCompanionDevices.Remove(d.Id);
                                    listCompanionDevices.Add(d.Id, d);
                                    // Update device
                                    OnDeviceUpdated(this, d);
                                }
                            }
                        }
                    });
                }
                else if (string.Equals(Command, CompanionProtocol.commandPing))
                {
                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        if (d != null)
                        {
                            /*
                            var searchDevice = listCompanionDevices.FirstOrDefault(device => string.Equals(device.Value.Name, d.Name) );
                            if (searchDevice.Value == null)
                            {
                                d.Status = CompanionDeviceStatus.Connected;
                                listCompanionDevices.Add(d.Id, d);
                                if (CompanionDeviceAdded != null)
                                    CompanionDeviceAdded(this, d);
                            }
                            else
                            {
                                if ((!string.Equals(d.Id, searchDevice.Value.Id)) ||
                                    (!string.Equals(d.IPAddress, searchDevice.Value.IPAddress)))
                                {
                                    d.Id = searchDevice.Value.Id;
                                    d.Status = CompanionDeviceStatus.Connected;
                                    listCompanionDevices.Remove(d.Id);
                                    listCompanionDevices.Add(d.Id, d);
                                    // Update device
                                    if (CompanionDeviceUpdated != null)
                                        CompanionDeviceUpdated(this, d);
                                }
                            }
                            */
                            Dictionary<string, string> parameters = new Dictionary<string, string>();
                            if (parameters != null)
                            {
                                parameters.Add(CompanionProtocol.parameterID, LocalCompanionDevice.Id);
                                parameters.Add(CompanionProtocol.parameterIPAddress, LocalCompanionDevice.IPAddress);
                                parameters.Add(CompanionProtocol.parameterKind, LocalCompanionDevice.Kind);
                                parameters.Add(CompanionProtocol.parameterName, LocalCompanionDevice.Name);
                                string m = CompanionProtocol.CreateCommand(CompanionProtocol.commandPingResponse, parameters);
                                await Send(d.IPAddress, m);
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
                System.Diagnostics.Debug.WriteLine("Exception on receiving UDP packets: " + exception.Message);
                SocketErrorStatus socketError = SocketError.GetStatus(exception.HResult);
                if (socketError == SocketErrorStatus.ConnectionResetByPeer)
                {
                }
                else if (socketError != SocketErrorStatus.Unknown)
                {
                }
            }
        }


        async Task<bool> InitializeSend()
        {
            bool result = false;
            try
            {
                if (msocketSend != null)
                {
                    msocketSend.Dispose();
                    msocketSend = null;
                }
                msocketSend = new DatagramSocket();

                NetworkAdapter adapter = GetDefaultNetworkAdapter(SendInterfaceAddress);
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
                NetworkAdapter adapter = GetDefaultNetworkAdapter(SendInterfaceAddress);
                if (adapter != null)
                    await msocketRecv.BindServiceNameAsync(MulticastUDPPort.ToString(), adapter);
                else
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
                    usocketRecv.MessageReceived -= UDPUnicastMessageReceived;
                    usocketRecv.Dispose();
                    usocketRecv = null;
                }
                usocketRecv = new DatagramSocket();
                usocketRecv.MessageReceived += UDPUnicastMessageReceived;
              //  NetworkAdapter adapter = GetDefaultNetworkAdapter();
              //  if (adapter != null)
              //      await msocketRecv.BindServiceNameAsync(UnicastUDPPort.ToString(), adapter);
              //  else
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
                    usocketRecv.MessageReceived -= UDPUnicastMessageReceived;
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
