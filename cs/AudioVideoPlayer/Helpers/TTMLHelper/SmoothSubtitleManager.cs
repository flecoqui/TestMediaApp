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

namespace AudioVideoPlayer.Helpers.TTMLHelper
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    public enum SmoothAssetStatus
    {
        Init = 0,
        ManifestLoaded,
        SubtitlesAvailable,
        SubtitlesNotAvailable,
        SubtitlesLoaded

    };
    class SmoothSubtitleTrack
    {
        public string Name { get; set; }
        public string Lang { get; set; }
        public int Bitrate { get; set; }
        public int PeriodMs { get; set; }
        public List<SubtitleItem> Subtitles { get; set; }
        public SmoothSubtitleTrack(string name, string lang, int bitrate, int period)
        {
            Name = name;
            Lang = lang;
            Bitrate = bitrate;
            PeriodMs = period;
            Subtitles = new List<SubtitleItem>();
        }
    }
    class SmoothSubtitleManager
    {
        public SmoothAssetStatus Status { get; set; }
        public string RootUri { get; set; }

        public Dictionary<string, SubtitleTrackDescription> SubtitleTrackList { get; set; }
        public bool IsLive()
        {
            if (SmoothManifestManager != null)
                return SmoothManifestManager.IsLive;
            return false;
        }
        ManifestManager SmoothManifestManager;
        System.Threading.Tasks.Task SubtitleTask = null;
        public ulong SubtitleLiveOffset { get; set; }
        public int ManifestUpdatePeriod { get; set; }
        bool downloadManifestTaskRunning = false;
        public SmoothSubtitleManager()
        {
            RootUri = string.Empty;
            SubtitleLiveOffset = 0;
            ManifestUpdatePeriod = 0;
            Status = SmoothAssetStatus.Init;
            SubtitleTrackList = new Dictionary<string, SubtitleTrackDescription>();
        }

        /// <summary>
        /// Defines the TTMLSubtitles element.
        /// </summary>
        private const string TTMLSubtitlesElement = "tt";

        /// <summary>
        /// Defines the TTMLSubtitles head.
        /// </summary>
        private const string TTMLSubtitlesHead = "head";

        /// <summary>
        /// Defines the TTMLSubtitles body.
        /// </summary>
        private const string TTMLSubtitlesBody = "body";

        /// <summary>
        /// Defines the TTMLSubtitles div
        /// </summary>
        private const string TTMLSubtitlesDiv = "div";

        /// <summary>
        /// Defines the TTMLSubtitles p.
        /// </summary>
        private const string TTMLSubtitlesP = "p";
        string GetMultiLineText(XmlReader reader)
        {
            string Text = string.Empty;
            while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                reader.Read();
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.Name == "span")
                {

                    reader.Read();
                    while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                        reader.Read();
                    if (reader.NodeType == XmlNodeType.Text)
                    {
                        Text = reader.Value;
                    }
                    reader.Read();
                    while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                        reader.Read();
                    while (((reader.Name == "br") || (reader.Name == "span")) && ((reader.NodeType == XmlNodeType.Element) || (reader.NodeType == XmlNodeType.EndElement)))
                    {
                        reader.Read();
                        while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                            reader.Read();
                        if (reader.NodeType == XmlNodeType.Text)
                        {
                            Text += "\n" + reader.Value;
                        }
                        reader.Read();
                        while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                            reader.Read();
                    }
                }
            }
            else if (reader.NodeType == XmlNodeType.Text)
            {
                Text = reader.Value;
                reader.Read();
                while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                    reader.Read();
                while ((reader.Name == "br") && (reader.NodeType == XmlNodeType.Element))
                {
                    reader.Read();
                    while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                        reader.Read();
                    if (reader.NodeType == XmlNodeType.Text)
                    {
                        Text += "\n" + reader.Value;
                    }
                    reader.Read();
                    while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                        reader.Read();
                }
            }

            return Text;
        }
        /// <summary>
        /// ParseTTMLSubtitles
        /// Parse TTML Subtitles 
        /// </summary>
        /// <param name="manifestBuffer">The buffer of the manifest being parsed.</param>
        public bool ParseAndAddTTMLSubtitles(ulong timeOffset, string name, string lang, int bitrate, int period,  byte[] subtitleBuffer)
        {
            bool bResult = false;
            using (var subtitleStream = new MemoryStream(subtitleBuffer))
            {
                bResult = this.ParseTTMLSubtitles(timeOffset, name, lang, bitrate, period,  subtitleStream);
            }
            return bResult;
        }
        /// <summary>
        /// Parses the TTML stream.
        /// </summary>
        /// <param name="subtitleStream">The manifest stream being parsed.</param>
        public bool ParseTTMLSubtitles(ulong timeOffset, string name, string lang, int bitrate, int period, Stream subtitleStream)
        {
            bool bResult = false;
            try
            {
                using (XmlReader reader = XmlReader.Create(subtitleStream))
                {
                    if (reader.Read() && reader.IsStartElement(TTMLSubtitlesElement))
                    {
                        while (reader.Read())
                        {
                            if (reader.Name == TTMLSubtitlesBody && reader.NodeType == XmlNodeType.Element)
                            {
                                reader.Read();
                                while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                                    reader.Read();
                                if (reader.Name == TTMLSubtitlesDiv && reader.NodeType == XmlNodeType.Element)
                                {
                                    string BeginDiv = "0s";
                                    if (reader.HasAttributes)
                                    {
                                        for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                        {
                                            reader.MoveToAttribute(attInd);
                                            if (reader.NodeType == XmlNodeType.Attribute)
                                            {
                                                if (reader.Name == "begin")
                                                    BeginDiv = reader.Value;
                                            }
                                        }
                                    }
                                    ulong BeginDivTime = SubtitleItem.ParseTime(BeginDiv);

                                    while (((reader.Name == TTMLSubtitlesP) && (reader.NodeType == XmlNodeType.Element)) || reader.Read())
                                    {
                                        while ((reader.Name == "") && (reader.NodeType == XmlNodeType.Whitespace))
                                            reader.Read();
                                        if (reader.Name == TTMLSubtitlesP && reader.NodeType == XmlNodeType.Element)
                                        {
                                            string Text = string.Empty;
                                            string Id = string.Empty;
                                            string Begin = string.Empty;
                                            string End = string.Empty;
                                            string Dur = string.Empty;
                                            if (reader.HasAttributes)
                                            {
                                                for (int attInd = 0; attInd < reader.AttributeCount; attInd++)
                                                {
                                                    reader.MoveToAttribute(attInd);
                                                    if (reader.NodeType == XmlNodeType.Attribute)
                                                    {
                                                        if (reader.Name == "begin")
                                                            Begin = reader.Value;
                                                        else if (reader.Name == "dur")
                                                            Dur = reader.Value;
                                                        else if (reader.Name == "end")
                                                            End = reader.Value;
                                                        else if (reader.Name == "xml:id")
                                                            Id = reader.Value;
                                                    }
                                                }
                                            }
                                            reader.Read();

                                            Text = GetMultiLineText(reader);

                                            if (!string.IsNullOrEmpty(Text) &&
                                                !string.IsNullOrEmpty(Begin) &&
                                                !string.IsNullOrEmpty(End)
                                                )
                                            {

                                                ulong BeginTime = SubtitleItem.ParseTime(Begin);
                                                ulong EndTime = SubtitleItem.ParseTime(End);


                                                if(SubtitleTrackList == null)
                                                {
                                                    SubtitleTrackList = new Dictionary<string, SubtitleTrackDescription>();
                                                }
                                                if (SubtitleTrackList != null)
                                                {
                                                    string key = name + lang;
                                                    if (!SubtitleTrackList.ContainsKey(key))
                                                        SubtitleTrackList.Add(key, new SubtitleTrackDescription(name, lang, bitrate, period));
                                                    if (SubtitleTrackList[key].SubtitleTrack == null)
                                                        SubtitleTrackList[key].SubtitleTrack = new Windows.Media.Core.TimedMetadataTrack(name,lang,Windows.Media.Core.TimedMetadataKind.Caption);
                                                    AddSubtitleItem(SubtitleTrackList[key].SubtitleTrack, BeginDivTime + BeginTime + timeOffset * 1000, BeginDivTime + EndTime + timeOffset * 1000, Text);
                                                    System.Diagnostics.Debug.WriteLine("Subtitle: " + (BeginDivTime + BeginTime + timeOffset * 1000).ToString() + " - " + (BeginDivTime + EndTime + timeOffset * 1000).ToString() + " Content: \r\n" + Text);

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                bResult = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while parsing Subtitles file: " + ex.Message);
                bResult = false;
            }
            return bResult;
        }
        public bool AddSubtitleItem(Windows.Media.Core.TimedMetadataTrack track, ulong start, ulong end, string Text)
        {
            bool result = false;
            if (track != null)
            {

                Windows.Media.Core.TimedTextCue t = new Windows.Media.Core.TimedTextCue();
                t.StartTime = TimeSpan.FromMilliseconds(start);
                t.Duration = TimeSpan.FromMilliseconds(end - start);
                Windows.Media.Core.TimedTextLine ttl = new Windows.Media.Core.TimedTextLine();
                ttl.Text = Text.Trim();
                t.Lines.Add(ttl);


                //char sep = '\n';
                //string[] lines = Text.Split(sep);
                //foreach(var line in lines)
                //{
                //    Windows.Media.Core.TimedTextLine ttl = new Windows.Media.Core.TimedTextLine();
                //    ttl.Text = line;
                //    t.Lines.Add(ttl);
                //}

                var ttd = new Windows.Media.Core.TimedTextDouble();
                ttd.Unit = Windows.Media.Core.TimedTextUnit.Percentage;
                ttd.Value = 100;

                Windows.Media.Core.TimedTextRegion region = new Windows.Media.Core.TimedTextRegion();
                region.ScrollMode = Windows.Media.Core.TimedTextScrollMode.Rollup;
                region.TextWrapping = Windows.Media.Core.TimedTextWrapping.Wrap;
                region.WritingMode = Windows.Media.Core.TimedTextWritingMode.LeftRightTopBottom;
                Windows.Media.Core.TimedTextPoint pos = new Windows.Media.Core.TimedTextPoint();
                pos.Unit = Windows.Media.Core.TimedTextUnit.Percentage;
                pos.X = 5;
                pos.Y = 70;
                region.Position = pos;
                Windows.Media.Core.TimedTextSize sz = new Windows.Media.Core.TimedTextSize();
                sz.Unit = Windows.Media.Core.TimedTextUnit.Percentage;
                sz.Height = 30;
                sz.Width = 90;
                region.Extent = sz;
                region.DisplayAlignment = Windows.Media.Core.TimedTextDisplayAlignment.Center;

                t.CueRegion = region;

                Windows.Media.Core.TimedTextStyle sty = new Windows.Media.Core.TimedTextStyle();

                Windows.UI.Color backColor = new Windows.UI.Color();
                backColor.A = 0;
                backColor.B = 255;
                backColor.G = 255;
                backColor.R = 255;
                Windows.UI.Color foreColor = new Windows.UI.Color();
                foreColor.A = 255;
                foreColor.B = 255;
                foreColor.G = 255;
                foreColor.R = 255;
                sty.Background = backColor;

                sty.FlowDirection = Windows.Media.Core.TimedTextFlowDirection.LeftToRight;
                sty.FontFamily = "default";
                sty.FontSize = ttd;
                sty.FontStyle = Windows.Media.Core.TimedTextFontStyle.Normal;
                sty.FontWeight = Windows.Media.Core.TimedTextWeight.Normal;
                sty.Foreground = foreColor;
                sty.IsBackgroundAlwaysShown = false;
                sty.IsLineThroughEnabled = false;
                sty.IsOverlineEnabled = false;
                sty.LineAlignment = Windows.Media.Core.TimedTextLineAlignment.Center;
                t.CueStyle = sty;
                t.Id = "test";


                try
                {
                    track.AddCue(t);
                    System.Diagnostics.Debug.Write("New caption: " + Text);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write("Exception will inserting caption: " + ex.Message);
                }
            }
            return result;
        }
        public string GetTTMLTextFromMP4Boxes(byte[] data)
        {
            string result = string.Empty;
            if ((data != null) && (data.Length > 0))
            {
                int index = 0;
                while (index < data.Length)
                {
                    Mp4Box box = Mp4Box.CreateMp4Box(data, index);
                    if (box.GetBoxType() == "moof")
                    {
                        index += box.GetBoxLength();
                    }
                    else if (box.GetBoxType() == "mdat")
                    {
                        result = System.Text.Encoding.UTF8.GetString(box.GetBoxData(), 0, box.GetBoxData().Length);
                        index += box.GetBoxLength();
                        break;
                    }
                    else
                    {
                        index += box.GetBoxLength();
                    }
                }
            }
            return result;
        }
        public byte[] GetTTMLBytesFromMP4Boxes(byte[] data)
        {
            byte[] result = null;
            if ((data != null) && (data.Length > 0))
            {
                int index = 0;
                while (index < data.Length)
                {
                    Mp4Box box = Mp4Box.CreateMp4Box(data, index);
                    if (box.GetBoxType() == "moof")
                    {
                        index += box.GetBoxLength();
                    }
                    else if (box.GetBoxType() == "mdat")
                    {
                        result = box.GetBoxData();
                        index += box.GetBoxLength();
                        break;
                    }
                    else
                    {
                        index += box.GetBoxLength();
                    }
                }
            }
            return result;
        }
        public bool StopLoadingSubtitles()
        {
            bool result = false;
            downloadManifestTaskRunning = false;
            System.Threading.Tasks.Task.Delay(1000).Wait();
            if (SubtitleTask!=null)
            {
                int Index = 0;
                while ((!SubtitleTask.IsCompleted)&&(Index++<5))
                {
                    System.Threading.Tasks.Task.Delay(1000).Wait();
                }
                SubtitleTask = null;
            }
            return result;
        }
        public bool StartLoadingSubtitles(ulong offset = 0, int period = 0)
        {
            bool result = false;
            SubtitleLiveOffset = offset;
            ManifestUpdatePeriod = period;
            SubtitleTask = System.Threading.Tasks.Task.Factory.StartNew(async ()
                =>
            {
                try
                {
                    System.Diagnostics.Debug.Write("Starting to capture and convert TTML subtitles from " + RootUri);

                    downloadManifestTaskRunning = true;
                    SmoothManifestManager = ManifestManager.CreateManifestManager(new Uri(RootUri), false, 5000000, 20);
                    if (SmoothManifestManager != null)
                    {

                        bool res = await SmoothManifestManager.LoadAndParseSmoothManifest();
                        if (res == true)
                        {
                            DateTime LatestManifestDownloadTime = DateTime.Now;
                            Status = SmoothAssetStatus.ManifestLoaded;
                            System.Diagnostics.Debug.Write("SmoothStreaming Manifest: " + RootUri + " correctly downloaded and parsed ");

                            // If Live update the list of chunks to downlad
                            while (this.downloadManifestTaskRunning)
                            {

                                // Download Text Chunks
                                if ((SmoothManifestManager.TextChunkListList != null) && (SmoothManifestManager.TextChunkListList.Count > 0))
                                {
                                    if(Status < SmoothAssetStatus.SubtitlesAvailable)
                                        Status = SmoothAssetStatus.SubtitlesAvailable;
                                    // Something to download
                                    if (!SmoothManifestManager.IsDownloadCompleted(SmoothManifestManager.TextChunkListList))
                                    {

                                        System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks for Manifest Uri: " + SmoothManifestManager.ManifestUri.ToString());
                                        int Index = 0;
                                        foreach (var cl in SmoothManifestManager.TextChunkListList)
                                        {
                                            ulong CaptureSubtitleStartTime = 0;
                                            if ((SubtitleLiveOffset > 0) && (SmoothManifestManager.IsLive))
                                            {
                                                CaptureSubtitleStartTime = cl.LastTimeChunksToRead - (SubtitleLiveOffset * SmoothManifestManager.TimeScale) > 0 ?
                                                                            cl.LastTimeChunksToRead - (SubtitleLiveOffset * SmoothManifestManager.TimeScale) : 0;
                                            }
                                            Index++;
                                            ChunkBuffer cb;
                                            while ((cl.ChunksToReadQueue.TryDequeue(out cb)) && (downloadManifestTaskRunning == true))
                                            {


                                                if (cb.Time >= CaptureSubtitleStartTime)
                                                {
                                                    string baseUri = ManifestManager.GetBaseUri(RootUri);
                                                    string url = baseUri + "/" + cl.TemplateUrl.Replace("{start_time}", cb.Time.ToString());
                                                    System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " downloadThread downloading text chunks : " + url.ToString());
                                                    cb.chunkBuffer = await SmoothManifestManager.DownloadChunkAsync(new Uri(url));

                                                    if (cb.IsChunkDownloaded())
                                                    {
                                                        ulong l = cb.GetLength();
                                                        if (l > 0)
                                                        {
                                                            double time = SmoothManifestManager.TimescaleToHNS(cb.Time) / (ManifestManager.TimeUnit);
                                                            string text = GetTTMLTextFromMP4Boxes(cb.chunkBuffer);
                                                            if (!string.IsNullOrEmpty(text))
                                                            {
                                                                System.Diagnostics.Debug.Write("TTML chunk at : " + time.ToString() + " seconds \r\n" + text);
                                                            }
                                                            byte[] subtitleBuffer = GetTTMLBytesFromMP4Boxes(cb.chunkBuffer);
                                                            if (subtitleBuffer != null)
                                                            {
                                                                string subtitleTrackName = (!string.IsNullOrEmpty(cl.Configuration.TrackName) ? cl.Configuration.TrackName : "sub" + Index.ToString());
                                                                string subtitleTrackLang = (!string.IsNullOrEmpty(cl.Configuration.Language) ? cl.Configuration.Language : "unk");
                                                                int HLSPeriod = 6000;
                                                                ParseAndAddTTMLSubtitles((ulong)time, subtitleTrackName, subtitleTrackLang, cl.Configuration.Bitrate, HLSPeriod, subtitleBuffer);
                                                            }

                                                        }

                                                        cl.InputBytes += l;
                                                        cl.InputChunks++;

                                                        cl.ChunksQueue.Enqueue(cb);
                                                    }
                                                    else
                                                        cl.InputChunks++;

                                                }
                                            }
                                            Status = SmoothAssetStatus.SubtitlesLoaded;

                                        }

                                    }
                                    else
                                    {
                                        if (SmoothManifestManager.IsLive)
                                            result = true;
                                    }
                                }
                                else
                                    Status = SmoothAssetStatus.SubtitlesNotAvailable;

                                if ((SmoothManifestManager.IsLive))
                                {
                                    System.Diagnostics.Debug.Write("Updating the live TTML subtitles for " + RootUri);
                                    double delta = (DateTime.Now - LatestManifestDownloadTime).TotalMilliseconds;
                                    if (delta< ManifestUpdatePeriod)
                                        System.Threading.Tasks.Task.Delay(ManifestUpdatePeriod - (int)delta).Wait();
                                    System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " Update Manifest for Uri: " + RootUri.ToString());
                                    await SmoothManifestManager.ParseAndUpdateSmoothManifest();
                                    System.Diagnostics.Debug.WriteLine(string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " Update Manifest done for Uri: " + RootUri.ToString());
                                    LatestManifestDownloadTime = DateTime.Now;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.Write("Capture and conversion of TTML subtitles done for " + RootUri);
                                    this.downloadManifestTaskRunning = false;
                                }

                            }
                        }
                    }
                }
                catch(Exception )
                {
                    result = false;
                }

            }
                );

            return result;
        }
        public async System.Threading.Tasks.Task<bool> LoadSmoothManifest(string root, ulong offset = 0, int period = 0)
        {
            bool result = false;
            RootUri = root;
            SubtitleLiveOffset = offset;
            ManifestUpdatePeriod = period;
            try
            {
                System.Diagnostics.Debug.Write("Starting to capture and convert TTML subtitles from " + RootUri);

                downloadManifestTaskRunning = true;
                SmoothManifestManager = ManifestManager.CreateManifestManager(new Uri(RootUri), false, 5000000, 20);
                if (SmoothManifestManager != null)
                {

                    bool res = await SmoothManifestManager.LoadAndParseSmoothManifest();
                    if (res == true)
                    {
                        DateTime LatestManifestDownloadTime = DateTime.Now;
                        Status = SmoothAssetStatus.ManifestLoaded;
                        System.Diagnostics.Debug.Write("SmoothStreaming Manifest: " + RootUri + " correctly downloaded and parsed ");

                        foreach (var t in SmoothManifestManager.TextChunkListList)
                        {
                            if (SubtitleTrackList == null)
                                SubtitleTrackList = new Dictionary<string, SubtitleTrackDescription>();
                            if (SubtitleTrackList != null)
                            {
                                SubtitleTrackDescription s = new SubtitleTrackDescription(t.Configuration.TrackName, t.Configuration.Language, 0, 0);
                                string key = t.Configuration.TrackName + t.Configuration.Language;
                                SubtitleTrackList.Add(key,s);
                            }
                        }
                        
                        result = true;
                    }
                }
            }
            catch (Exception)
            {
                result = false;
            }
            return result;
        }



    }

}
