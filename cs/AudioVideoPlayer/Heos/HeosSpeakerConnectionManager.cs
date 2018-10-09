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
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.System;
using Windows.System.RemoteSystems;
using CompanionService;

namespace AudioVideoPlayer.Heos
{


    class HeosSpeakerConnectionManager
    {


        Dictionary<string, HeosSpeaker>  listHeosSpeakers;

        public HeosSpeakerConnectionManager()
        {
        }
        // Summary:
        //     Initialize HeosSpeakerConnectionManager
        public virtual async System.Threading.Tasks.Task<bool> Initialize()
        {
            
            if (listHeosSpeakers == null)
                listHeosSpeakers = new Dictionary<string, HeosSpeaker>();
            { 
            }
            return false;
        }
        // Summary:
        //     Uninitialize HeosSpeakerConnectionManager
        public virtual bool Uninitialize()
        {

            return true;
        }


        //
        // Summary:
        //     Launch the Discovery Thread.
        public virtual async System.Threading.Tasks.Task<bool> StartDiscovery()
        {
            // Verify access for Remote Systems. 
            // Note: RequestAccessAsync needs to called from the UI thread.
            try
            {
                RemoteSystemAccessStatus accessStatus = await RemoteSystem.RequestAccessAsync();

                if (accessStatus == RemoteSystemAccessStatus.Allowed)
                {
                    // Stop Discovery service if it was running
                    StopDiscovery();
                    // Clear the current list of devices
                    if (listHeosSpeakers != null) listHeosSpeakers.Clear();

                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while starting discovery: " + ex.Message);
            }
            return false;
        }
        //
        // Summary:
        //     Stop the Discovery Thread.
        public virtual bool StopDiscovery()
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while starting discovery: " + ex.Message);
            }

            return false;
        }
        //
        // Summary:
        //     Stop the Discovery Thread.
        public virtual bool IsDiscovering()
        {
            return false;
        }
        protected virtual void OnHeosSpeakerAdded(HeosSpeakerConnectionManager m, HeosSpeaker d)
        {
            if (HeosSpeakerAdded != null)
                HeosSpeakerAdded(m, d);
        }
        //
        // Summary:
        //     The event that is raised when a new Companion Device is discovered.
        public event TypedEventHandler<HeosSpeakerConnectionManager, HeosSpeaker> HeosSpeakerAdded;
        protected virtual void OnHeosSpeakerRemoved(HeosSpeakerConnectionManager m, HeosSpeaker d)
        {
            if (HeosSpeakerRemoved != null)
                HeosSpeakerRemoved(m, d);
        }
        //
        // Summary:
        //     The event that is raised when a previously discovered Companion Device
        //     is no longer visible.
        public event TypedEventHandler<HeosSpeakerConnectionManager, HeosSpeaker> HeosSpeakerRemoved;
        protected virtual void OnHeosSpeakerUpdated(HeosSpeakerConnectionManager m, HeosSpeaker d)
        {
            if (HeosSpeakerUpdated != null)
                HeosSpeakerUpdated(m, d);
        }
        //
        // Summary:
        //     Raised when a previously discovered Companion Device changes from proximally
        //     connected to cloud connected, or vice versa.
        public event TypedEventHandler<HeosSpeakerConnectionManager, HeosSpeaker> HeosSpeakerUpdated;


        // Summary:
        //     Check if the device is connected (a ping have been successful)
        public virtual bool IsHeosSpeakerConnected(HeosSpeaker cd)
        {
            if (listHeosSpeakers.ContainsKey(cd.Id))
            {
                return (listHeosSpeakers[cd.Id].Status == HeosSpeakerStatus.Connected);
            }
            return false;
        }
        public virtual async System.Threading.Tasks.Task<bool> Send(HeosSpeaker cd, string Message)
        {
            if (cd != null)
            {

                //if (await CheckCompanionDeviceConnected(cd))
                {
                }
            }
            return false;
        }

        //
        // Summary:
        //     Raised when a Message is received from a Companion Device
        public virtual event TypedEventHandler<HeosSpeaker, String> MessageReceived;


        public static bool IsIPv4Address(string s)
        {
            try
            {
                if (!string.IsNullOrEmpty(s))
                {
                    char[] sep = { '.' };
                    string[] arrayIP = s.Split(sep);
                    if (arrayIP.Count() == 4)
                    {
                        for (int i = 0; i < arrayIP.Count(); i++)
                        {
                            int digit = int.Parse(arrayIP[i]);
                            if ((digit < 0) || (digit > 255))
                                return false;
                        }
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception checking IP Address: " + ex.Message);
            }
            return false;
        }

        #region private


        protected HeosSpeaker GetUniqueDeviceByName(string Name)
        {
            HeosSpeaker device = null;
            foreach (var d in listHeosSpeakers)
            {
                if (string.Equals(d.Value.Name, Name))
                {
                    if (device == null)
                        device = d.Value;
                    else
                        // not unique
                        return null;
                }
            }
            return device;
        }

        #endregion private

    }
}
