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

namespace AudioVideoPlayer.DLNA
{
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
    public class DLNADevice
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

        public List<DLNAService> ListDLNAServices { get; set; }

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
            Status = DLNADeviceStatus.Unknown;
        }
        public DLNADevice(string id, string location, string version, string ipCache, string server,
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
            Status = DLNADeviceStatus.Unknown;
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
                var client = new HttpClient();
                if (client != null)
                {
                    result = await client.GetStringAsync(new Uri(this.Location));
                }
            }
            catch (Exception)
            {
                result = string.Empty;
            }

            return result;
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


                //End Description
                string desc = GetMetadataString(bAudioOnly, UrlToPlay,  AlbumArtUrl, Title, codec);
                sb.Append(desc);


                sb.Append("</CurrentURIMetaData>\r\n");
                
                sb.Append("</u:SetAVTransportURI>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
               // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
               // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#SetAVTransportURI\"");
               // httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");

                
                




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


                //End Description
                string desc = GetMetadataString(bAudioOnly, UrlToPlay, AlbumArtUrl, Title, codec);
                sb.Append(desc);


                sb.Append("</NextURIMetaData>\r\n");

                sb.Append("</u:SetNextAVTransportURI>\r\n");
                sb.Append(XMLFoot);

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Cache-Control", "no-cache");
                // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
                // httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#SetAVTransportURI\"");
                // httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");







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
              //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Connection", "Close");
              //  httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("FriendlyName.DLNA.ORG", AudioVideoPlayer.Information.SystemInformation.DeviceName);
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("Content-Type", "text/xml; charset=\"utf-8\"");
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#Play\"");
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
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#Play\"");
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
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#Play\"");
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
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#Play\"");
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
                httpClient.DefaultRequestHeaders.TryAppendWithoutValidation("SOAPAction", "\"urn:schemas-upnp-org:service:AVTransport:1#Play\"");
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

        private async System.Threading.Tasks.Task<bool> GetPosition(string ControlURL, int Index)
        {
            bool result = false;
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
                        if(Response.IndexOf("u:GetPositionInfoResponse")>0)
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
        private async System.Threading.Tasks.Task<bool> GetMediaInfo(string ControlURL, int Index)
        {
            bool result = false;
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
                        if (Response.IndexOf("u:GetPositionInfoResponse") > 0)
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
        public async System.Threading.Tasks.Task<bool> AddUrl(bool bAudioOnly, string UrlToPlay, string AlbumArtUrl, string Title, string codec)
        {
            bool result = false;
            DLNAService ds = await GetDLNAService();
            if (ds != null)
            {
                result = await PrepareNextPlayTo(ds.ControlURL, bAudioOnly, UrlToPlay, AlbumArtUrl, Title, codec, 0);
                if (result == true)
                    result = await Play(ds.ControlURL, 0);
            }

            return result;
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
        public async System.Threading.Tasks.Task<bool> PlayerSetMute(bool on)
        {
            bool result = false;
            if (string.IsNullOrEmpty(PlayerId))
                await GetPlayerId();
            if (!string.IsNullOrEmpty(PlayerId))
            {
                string response = await SendTelnetCommand("heos://player/volume_down?pid=" + PlayerId + "&state=" + (on==true?"on":"off"));
                if (IsCommandSuccessful("player/volume_down", response))
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
