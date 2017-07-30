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
using Windows.Foundation;

namespace AudioVideoPlayer.Companion
{
    internal interface ICompanionConnectionManager
    {
        
        CompanionDevice LocalCompanionDevice { get; set; }
        string ApplicationUri { get; set; }
        string AppServiceName { get; set; }
        string PackageFamilyName { get; set; }



        // Summary:
        //     Initialize CompanionConnectionManager
        System.Threading.Tasks.Task<bool> Initialize(CompanionDevice LocalDevice, CompanionConnectionManagerInitializeArgs InitArgs);

        // Summary:
        //     Uninitialize CompanionConnectionManager
        bool Uninitialize();


        // Summary:
        //     Start discovery process .
        bool StartDiscovery();
        // Summary:
        //     Stop discovery process .
        bool StopDiscovery();
        
        // Summary:
        //     Indicates whether the discovery process is launched.
        bool IsDiscovering();
        
        // Summary:
        //     The event that is raised when a new Companion Device is discovered.
        event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceAdded;
        
        // Summary:
        //     The event that is raised when a previously discovered Companion Device
        //     is no longer visible.
        event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceRemoved;
        
        
        // Summary:
        //     Raised when a previously discovered Companion Device changes from proximally
        //     connected to cloud connected, or vice versa.
        event TypedEventHandler<CompanionConnectionManager, CompanionDevice> CompanionDeviceUpdated;

        // Summary:
        //     Check if the device is connected, if not send a request to launch the application on the remote device
        System.Threading.Tasks.Task<bool> CheckCompanionDeviceConnected(CompanionDevice cd);

        // Summary:
        //     Check if the device is connected (a ping have been successful)
        bool IsCompanionDeviceConnected(CompanionDevice cd);


        // Summary:
        //     Open a uri on the emote device
        System.Threading.Tasks.Task<bool> CompanionDeviceOpenUri(CompanionDevice cd, string inputUri);

        
        // Summary:
        //     Send a message to a Companion  Device
        System.Threading.Tasks.Task<bool> Send(CompanionDevice cd, string Message);

        
        // Summary:
        //     Raised when a Message is received from a Companion Device
        event TypedEventHandler<CompanionDevice, String> MessageReceived;


    }
}
