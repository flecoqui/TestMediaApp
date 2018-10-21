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

namespace AudioVideoPlayer.Heos
{
    public enum HeosSpeakerStatus
    {
        //
        // Summary:
        //     The HeosSpeaker is unavailable.
        Unavailable = 0,
        //
        // Summary:
        //     The HeosSpeaker is available.
        Available = 1,
        //
        // Summary:
        //     The HeosSpeaker is connected.
        Connected = 2,
        //
        // Summary:
        //     The availability of the HeosSpeaker is unknown.
        Unknown = 3
    }
    public class HeosSpeaker
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
        public HeosSpeakerStatus Status { get; set; }
        public string PlayerId { get; set; }

        public HeosSpeaker()
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
            Status = HeosSpeakerStatus.Unknown;
        }
        public HeosSpeaker(string id, string location, string version, string ipCache, string server,
            string st, string usn, string ip, string friendlyName, string manufacturer, string modelName, string modelNumber)
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
            Status = HeosSpeakerStatus.Unknown;
        }
        public bool IsHeosDevice()
        {
            if(!string.IsNullOrEmpty(Server))
            {
                if(Server.IndexOf("Heos") > 0 )
                    return  true ;
            }
            if (!string.IsNullOrEmpty(Version))
            {
                if (Version.IndexOf("HEOS") > 0)
                    return true;
            }
            return false;
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
                    var data = Encoding.UTF8.GetBytes(cmd + "\r\n");
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
        public async System.Threading.Tasks.Task<bool> PlayUrl(string url)
        {
            bool result = false;
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
    }
}
