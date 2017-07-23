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
        private void comboPlayList_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            RemoveButton.IsEnabled = false;
            ImportButton.IsEnabled = false;
            if (comboPlayList.Items.Count > 0)
            {
                ViewModels.ViewModel vm = this.DataContext as ViewModels.ViewModel;
                if (vm != null)
                {
                    string PlaylistPath = vm.Settings.CurrentPlayListPath;
                    int index = 0;
                    foreach (var item in comboPlayList.Items)
                    {
                        if (item is Models.PlayList)
                        {
                            Models.PlayList p = item as Models.PlayList;
                            if (p != null)
                            {
                                if (string.Equals(p.Path, PlaylistPath))
                                {
                                    comboPlayList.SelectedIndex = index;
                                    break;
                                }
                                if (string.Equals(p.ImportedPath, PlaylistPath))
                                {
                                    comboPlayList.SelectedIndex = index;
                                    break;
                                }
                            }
                        }
                        index++;
                    }
                }
            }
        }
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
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

        bool IsThePlaylistNameUsed(string name)
        {
            ViewModels.ViewModel vm = this.DataContext as ViewModels.ViewModel;
            if (vm != null)
            {
                foreach (var p in vm.Settings.PlayListList)
                {
                    if (string.Equals(p.Name, name))
                        return true;
                }
            }
            return false;
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
                    if(!IsThePlaylistNameUsed(file.Name))
                    {
                        Models.PlayList playlist = await Models.PlayList.GetNewPlaylist(file.Path);
                        if (playlist != null)
                        {
                            ViewModels.ViewModel vm = this.DataContext as ViewModels.ViewModel;
                            if (vm != null)
                            {
                                ObservableCollection<Models.PlayList> PlayListList = vm.Settings.PlayListList;
                                PlayListList.Add(playlist);
                                vm.Settings.PlayListList = PlayListList;
                                if (comboPlayList.Items.Count > 0)
                                {

                                    RemoveButton.IsEnabled = true;
                                    int index = 0;
                                    foreach (var item in comboPlayList.Items)
                                    {
                                        if(item is Models.PlayList)
                                        {
                                            Models.PlayList p = item as Models.PlayList;
                                            if(p!=null)
                                            {
                                                if(string.Equals(p.Name,playlist.Name))
                                                {
                                                    comboPlayList.SelectedIndex = index;
                                                    break;
                                                }
                                            }
                                        }
                                        index++;
                                    }
                                }
                                else
                                {
                                    ImportButton.IsEnabled = false;
                                    RemoveButton.IsEnabled = false;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
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
        private void RemovePlaylist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (comboPlayList.SelectedItem is Models.PlayList)
            {
                ObservableCollection<Models.PlayList> p =  ViewModelLocator.Settings.PlayListList;
                if ((p != null)&&(comboPlayList.SelectedIndex<p.Count))
                    p.RemoveAt(comboPlayList.SelectedIndex);
                ViewModelLocator.Settings.PlayListList = p;
                if (comboPlayList.Items.Count > 0)
                {

                    RemoveButton.IsEnabled = true;
                    comboPlayList.SelectedIndex = 0;
                }
                else
                {
                    ImportButton.IsEnabled = false;
                    RemoveButton.IsEnabled = false;
                }

            }            
        }

        private void comboPlayList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(comboPlayList.SelectedItem is Models.PlayList)
            {
                Models.PlayList p = (Models.PlayList)comboPlayList.SelectedItem;
                if(p!=null)
                {
                    if(!string.IsNullOrEmpty(p.ImportedPath))
                    {
                        ViewModelLocator.Settings.CurrentPlayListPath = p.ImportedPath;
                        ViewModelLocator.Settings.CurrentPlayListIndex = comboPlayList.SelectedIndex;
                        ViewModelLocator.Settings.CurrentMediaPath = string.Empty;
                        ViewModelLocator.Settings.CurrentMediaIndex = 0;
                    }
                    else if (!string.IsNullOrEmpty(p.Path))
                    {
                        ViewModelLocator.Settings.CurrentPlayListPath = p.Path;
                        ViewModelLocator.Settings.CurrentPlayListIndex = comboPlayList.SelectedIndex;
                        ViewModelLocator.Settings.CurrentMediaPath = string.Empty;
                        ViewModelLocator.Settings.CurrentMediaIndex = 0;
                    }
                    ImportButton.IsEnabled = (p.bImported == false ? true : false);
                    RemoveButton.IsEnabled = true;
                }
            }
        }
    }


}
