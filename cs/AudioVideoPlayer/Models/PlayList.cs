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
using Windows.UI.Xaml.Controls;

namespace AudioVideoPlayer.Models
{
    public class PlayList
    {
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
