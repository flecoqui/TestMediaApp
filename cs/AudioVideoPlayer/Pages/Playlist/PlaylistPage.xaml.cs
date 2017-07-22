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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using AudioVideoPlayer.DataModel;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using System.Reflection;
using Companion;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Text.RegularExpressions;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AudioVideoPlayer.Pages.Playlist
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlaylistPage : Page
    {

        /// <summary>
        /// PlaylistPage constructor 
        /// </summary>
        public PlaylistPage()
        {
            this.InitializeComponent();
        }
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected  override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
        }
        /// <summary>
        /// Playlist method which loads another JSON playlist for the application 
        /// </summary>
        private void Playlist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.FileTypeFilter.Add(".json");
            filePicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            filePicker.SettingsIdentifier = "PlaylistPicker";
            filePicker.CommitButtonText = "Open JSON Playlist File to Process";
            /*
            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);
            }*/
        }

        private async void AddPlaylist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.FileTypeFilter.Add(".json");
            filePicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            filePicker.SettingsIdentifier = "PlaylistPicker";
            filePicker.CommitButtonText = "Open JSON Playlist File to Process";

            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);


                try
                {
                    Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);

                    MediaDataSource.Clear();
                    MediaDataGroup audio_video = await MediaDataSource.GetGroupAsync(file.Path, "audio_video_picture");
                    if ((audio_video != null) && (audio_video.Items.Count > 0))
                    {
                        Models.PlayList playlist = new Models.PlayList(file.Path, audio_video.Title);
                        if(playlist!=null)
                        {
                            playlist.Count = audio_video.Items.Count;
                            playlist.Index = 0;
                            playlist.bAnalyzed = false;
                            playlist.bImported = false;
                            playlist.bLocalItem = false;
                            playlist.bRemoteItem = false;
                            playlist.bRemovalDeviceItem = false;
                            playlist.ImportedPath = string.Empty;
                            ObservableCollection<Models.PlayList> PlayListList = ViewModels.StaticSettingsViewModel.PlayListList;
                            PlayListList.Add(playlist);
                            ViewModels.StaticSettingsViewModel.PlayListList = PlayListList;
                        }
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
                }
            }
        }
        private void ImportPlaylist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {

        }
    }


}
