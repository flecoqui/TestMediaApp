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


    }
}
