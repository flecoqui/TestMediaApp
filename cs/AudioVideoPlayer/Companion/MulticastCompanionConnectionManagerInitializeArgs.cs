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

namespace AudioVideoPlayer.Companion
{
    public class MulticastCompanionConnectionManagerInitializeArgs : CompanionConnectionManagerInitializeArgs
    {
        public string MulticastIPAddress { get; set; }
        public int MulticastUDPPort { get; set; }
        public int UnicastUDPPort { get; set; }

        public bool MulticastDiscovery { get; set; }
        public bool UDPTransport { get; set; }
        public string SendInterfaceAddress { get; set; }

    }
}
 