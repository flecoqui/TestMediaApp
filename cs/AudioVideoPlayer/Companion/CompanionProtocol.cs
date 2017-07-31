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
    // This static Class implements the protocol to send command towards the devices
    // Those commands could be sent either over AppToApp Communication or over UDP (unicast or Multicast 
    public static class CompanionProtocol
    {
        public const string cMulticastAddress = "239.11.11.11";
        public const string cMulticastPort = "1919";
        public const string cUnicastPort = "1918";
        public const string cQUESTION = "??";
        public const string cEQUAL = "=";
        const string cMULTICASTCOMMAND = "M-COMMAND";
        const string cUNICASTCOMMAND = "U-COMMAND";

        public const string commandOpen = "OPENMEDIA";
        public const string commandOpenPlaylist = "OPENPLAYLIST";
        public const string commandPlay = "PLAY";
        public const string commandStop = "STOP";
        public const string commandPause = "PAUSE";
        public const string commandPlayPause = "PLAYPAUSE";
        public const string commandSelect = "SELECT";
        public const string commandPlus = "PLUS";
        public const string commandMinus = "MINUS";
        public const string commandPlusPlaylist = "PLUSPLAYLIST";
        public const string commandMinusPlayist = "MINUSPLAYLIST";
        public const string commandFullWindow = "FULLWINDOW";
        public const string commandFullScreen = "FULLSCREEN";
        public const string commandWindow = "WINDOW";
        public const string commandMute = "MUTE";
        public const string commandVolumeUp = "VOLUMEUP";
        public const string commandVolumeDown = "VOLUMEDOWN";
        public const string commandUp = "UP";
        public const string commandDown = "DOWN";
        public const string commandLeft = "LEFT";
        public const string commandRight = "RIGHT";
        public const string commandEnter = "ENTER";
        public const string commandPing = "PING";
        public const string commandPingResponse = "PINGRESPONSE";


        public const string parameterID = "ID";
        public const string parameterContent = "CONTENT";
        public const string parameterPosterContent = "POSTERCONTENT";
        public const string parameterStart = "START";
        public const string parameterDuration = "DURATION";
        public const string parameterIndex = "INDEX";
        public const string parameterName = "NAME";
        public const string parameterIPAddress = "IP";
        public const string parameterKind = "KIND";

        public const string MulticastDeviceName = "Multicast";
        public const string MulticastDeviceKind = "Virtual";
        public static string CreateCommand(string command, Dictionary<string, string> parameters)
        {
            string s = cMULTICASTCOMMAND + cQUESTION + command ;
            if ((parameters != null) &&
                (parameters.Count > 0))
            {
                foreach (var value in parameters)
                {
                    s += cQUESTION + value.Key + cEQUAL + value.Value;
                }
            }
            return s;
        }
        public static Dictionary<string, string> GetParametersFromMessage(string input)
        {
            Dictionary<string, string> parameters = null;
            if (!string.IsNullOrEmpty(input))
            {
                string[] separator = new string[1];
                separator[0] = cQUESTION;
                string[] array = input.Split(separator, StringSplitOptions.None);
                foreach (var s in array)
                {
                    int index = s.IndexOf(cEQUAL);
                    if (index > 0)
                    {
                        string key = s.Substring(0, index);
                        string value = s.Substring(index + 1);
                        if (parameters == null)
                            parameters = new Dictionary<string, string>();
                        parameters.Add(key, value);
                    }
                }
                return parameters;
            }
            return null;
        }
        public static string GetCommandFromMessage(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                int pos = input.IndexOf(cMULTICASTCOMMAND + cQUESTION);
                string header = cMULTICASTCOMMAND + cQUESTION;
                string command = input.Substring(pos + header.Length);
                if(!string.IsNullOrEmpty(command))
                {
                    string[] separator = new string[1];
                    separator[0] = cQUESTION;
                    string[] array = command.Split(separator, StringSplitOptions.None);
                    if ((array != null) && (array.Length > 0))
                        return array[0];
                }
            }
            return null;
        }
    }
}
