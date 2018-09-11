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
using System.Threading.Tasks;

namespace AudioVideoPlayer.Helpers.TTMLHelper
{
    public class SubtitleItem
    {
        public ulong startTime;
        public ulong endTime;
        public string subtitle;
        public SubtitleItem(ulong start, ulong end, string sub)
        {
            startTime = start;
            endTime = end;
            subtitle = sub;
        }
        public override string ToString()
        {
            return TimeToString(startTime) + " --> " + TimeToString(endTime) + "\r\n" + subtitle + "\r\n\r\n";
        }
        public static ulong ParseTime(string s)
        {
            ulong d = 0;
            if (s.EndsWith("s"))
            {
                s = s.Substring(0, s.Length - 1);
                double dd = 0;
                if (double.TryParse(s, out dd))
                {
                    d = (ulong)(dd * 1000);
                    return d;
                }
                else
                    return 0;

            }
            System.Text.RegularExpressions.Regex shortTime = new System.Text.RegularExpressions.Regex(@"^\s*(\d+)?:?(\d+):(\d+).(\d+)\s*$");
            //            System.Text.RegularExpressions.Regex shortTime = new System.Text.RegularExpressions.Regex(@"^\s*(\d+)?:?(\d+):([\d\.]+)\s*$");
            // System.Text.RegularExpressions.Regex longTime = new System.Text.RegularExpressions.Regex(@"^\s*(\d{2}):(\d{2}):(\d{2}):(\d{2})\s*$");
            System.Text.RegularExpressions.MatchCollection mc = shortTime.Matches(s);
            if ((mc != null) && (mc.Count == 1))
            {
                ulong hours = 0;
                ulong minutes = 0;
                ulong seconds = 0;
                ulong milliseconds = 0;
                if (mc[0].Groups.Count >= 5)
                {
                    if (ulong.TryParse(mc[0].Groups[1].Value, out hours))
                    {
                        if (ulong.TryParse(mc[0].Groups[2].Value, out minutes))
                        {
                            if (ulong.TryParse(mc[0].Groups[3].Value, out seconds))
                            {
                                if (ulong.TryParse(mc[0].Groups[4].Value, out milliseconds))
                                {
                                    d = hours * 3600 * 1000 + minutes * 60 * 1000 + seconds * 1000 + milliseconds;
                                }
                            }
                        }
                    }
                }
            }
            return d;
        }

        public static string TimeToString(ulong d)
        {
            ulong hours = d / (3600 * 1000);
            ulong minutes = (d - hours * 3600 * 1000) / (60 * 1000);
            ulong seconds = (d - hours * 3600 * 1000 - minutes * 60 * 1000) / 1000;
            ulong milliseconds = (d - hours * 3600 * 1000 - minutes * 60 * 1000 - seconds * 1000);
            //if (hours == 0)
            //    return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
            //else 
            if (hours < 100)
                return string.Format("{0:00}:{1:00}:{2:00}.{3:000}", hours, minutes, seconds, milliseconds);
            else
                return string.Format("{0}:{1:00}:{2:00}.{3:000}", hours, minutes, seconds, milliseconds);
        }
    }
}
