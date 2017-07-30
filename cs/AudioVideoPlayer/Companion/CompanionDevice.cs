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

namespace AudioVideoPlayer.Companion
{
    public enum CompanionDeviceStatus
    {
        //
        // Summary:
        //     The CompanionDevice is unavailable.
        Unavailable = 0,
        //
        // Summary:
        //     The CompanionDevice is available.
        Available = 1,
        //
        // Summary:
        //     The CompanionDevice is connected.
        Connected = 2,
        //
        // Summary:
        //     The availability of the CompanionDevice is unknown.
        Unknown = 3
    }
    public class CompanionDevice
    {
        public string Id { get; set; }
        public string Name {get;set; }
        public string IPAddress { get;set; }
        public string Kind { get; set; }
        public bool IsAvailableBySpatialProximity { get; set; }
        public bool IsAvailableByProximity { get; set; }
        public CompanionDeviceStatus Status { get; set; }
        public bool IsMulticast { get; set; }
        public CompanionDevice()
        {
            Id = string.Empty;
            Name = string.Empty;
            IPAddress = string.Empty;
            Kind = string.Empty;
            IsAvailableByProximity = false;
            IsAvailableBySpatialProximity = false;
            Status = CompanionDeviceStatus.Unknown;
            IsMulticast = false;
        }
        public CompanionDevice(string id, string name, string ipAddress, string kind)
        {
            Id = id;
            Name = name;
            IPAddress = ipAddress;
            Kind = kind;
            IsAvailableByProximity = false;
            IsAvailableBySpatialProximity = false;
            Status = CompanionDeviceStatus.Unknown;
            IsMulticast = false;
        }

    }
}
