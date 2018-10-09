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
        public string Name {get;set; }
        public string IPAddress { get;set; }
        public string Kind { get; set; }
        // Anniversary issue

        public HeosSpeakerStatus Status { get; set; }

        public HeosSpeaker()
        {
            Id = string.Empty;
            Name = string.Empty;
            IPAddress = string.Empty;
            Kind = string.Empty;
            Status = HeosSpeakerStatus.Unknown;
        }
        public HeosSpeaker(string id, bool isRemoteSytemDevice, string name, string ipAddress, string kind)
        {
            Id = id;
            Name = name;
            IPAddress = ipAddress;
            Kind = kind;
            Status = HeosSpeakerStatus.Unknown;
        }

    }
}
