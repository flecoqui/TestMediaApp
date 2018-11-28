// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;
using AudioVideoPlayer.DataModel;

namespace AudioVideoPlayer.Models
{
    public class PlayList
    {
        public async static System.Threading.Tasks.Task<Models.PlayList> GetNewPlaylist(string path)
        {
            MediaDataSource.Clear();
            MediaDataGroup audio_video = await MediaDataSource.GetGroupAsync(path, "audio_video_picture");
            if (audio_video != null)
            {
                Models.PlayList playlist = new Models.PlayList(path, audio_video.Title);
                if (playlist != null)
                {
                    playlist.bImported = false;
                    playlist.bLocalItem = false;
                    playlist.bRemoteItem = false;

                    playlist.Count = audio_video.Items.Count;
                    foreach (var item in audio_video.Items)
                    {
                        if (!string.IsNullOrEmpty(item.PosterContent))
                        {
                            if (item.PosterContent.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                playlist.bRemoteItem = true;
                            if (item.PosterContent.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase))
                                playlist.bLocalItem = true;
                            if (item.PosterContent.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                                playlist.bLocalItem = true;
                        }
                        if (!string.IsNullOrEmpty(item.Content))
                        {

                            if (item.Content.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                                playlist.bRemoteItem = true;
                            else if (item.Content.StartsWith("redirect://", StringComparison.OrdinalIgnoreCase))
                                playlist.bRemoteItem = true;
                            else if (item.Content.StartsWith("redirects://", StringComparison.OrdinalIgnoreCase))
                                playlist.bRemoteItem = true;
                            else if (item.Content.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase))
                                playlist.bLocalItem = true;
                            else if (item.Content.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                                playlist.bLocalItem = true;
                        }
                    }
                    playlist.Index = 0;
                    playlist.bAnalyzed = true;
                    playlist.bImported = false;
                    playlist.ImportedPath = string.Empty;
                    MediaDataSource.Clear();
                    return playlist;
                }
            }
            MediaDataSource.Clear();
            return null;
        }

        public PlayList()
        {
        }
        public PlayList(string path, string name)
        {
            Path = path;
            Name = name;
        }
        public string Path { get; set; }
        public bool bAnalyzed { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public int Index { get; set; }
        public bool bImported { get; set; }
        public string ImportedPath { get; set; }
        public bool bRemoteItem { get; set; }
        public bool bLocalItem { get; set; }
        public bool bRemovalDeviceItem { get; set; }



    }

}
