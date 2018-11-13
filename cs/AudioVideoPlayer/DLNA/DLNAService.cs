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
    public class DLNAService
    {
        public string ControlURL { get; set; }
        public string Scpdurl { get; set; }
        public string EventSubURL { get; set; }
        public string ServiceType { get; set; }
        public string ServiceID { get; set; }

        public DLNAService()
        {
            ControlURL = string.Empty;
            Scpdurl = string.Empty;
            EventSubURL = string.Empty;
            ServiceType = string.Empty;
            ServiceID = string.Empty;
        }
        public DLNAService(string controlURL, string scpdurl, string eventSubURL, string serviceType, string serviceID)
        {
            ControlURL = controlURL;
            Scpdurl = scpdurl;
            EventSubURL = eventSubURL;
            ServiceType = serviceType;
            ServiceID = serviceID;
        }
        public static string GetXMLContent(string Content, string XMLTag)
        {
            string result = string.Empty;
            if(!string.IsNullOrEmpty(XMLTag) &&
                !string.IsNullOrEmpty(Content))
            {
                string OpenTag = "<" + XMLTag + ">";
                string CloseTag = "</" + XMLTag + ">";

                int OpenPos = Content.IndexOf(OpenTag);
                if(OpenPos >= 0)
                {
                    int ClosePos = Content.IndexOf(CloseTag, OpenPos + OpenTag.Length);
                    if(ClosePos> OpenPos + OpenTag.Length)
                    {
                        result = Content.Substring(OpenPos + OpenTag.Length, ClosePos - OpenPos - OpenTag.Length);
                    } 
                }
            }
            return result;
        }
        public static DLNAService CreateDLNAService(string Content)
        {
            DLNAService result = null;
            try
            {
                if (!string.IsNullOrEmpty(Content))
                {
                    string servicetype = GetXMLContent(Content, "serviceType");
                    string serviceid = GetXMLContent(Content, "serviceId");
                    string controlurl = GetXMLContent(Content, "controlURL");
                    string scpdurl = GetXMLContent(Content, "SCPDURL");
                    string eventsuburl = GetXMLContent(Content, "eventSubURL");
                    result = new DLNAService(controlurl, scpdurl, eventsuburl, servicetype, serviceid);
                }
            }
            catch (Exception)
            {
                result = null;
            }

            return result;
        }
        public static List<DLNAService> CreateDLNAServiceList(string Content)
        {
            List<DLNAService> result = new List<DLNAService>();
            try
            {
                if (!string.IsNullOrEmpty(Content))
                {

                    string servicelist = GetXMLContent(Content, "serviceList");

                    string[] sep = { "</service>" };
                    string[] sl = servicelist.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                    foreach ( string service in sl)
                    {
                        DLNAService ds = CreateDLNAService(service);
                        if(ds!=null)
                        {
                            result.Add(ds);
                        }
                    }
                }
            }
            catch (Exception)
            {
                result = null;
            }

            return result;
        }
    }
}
