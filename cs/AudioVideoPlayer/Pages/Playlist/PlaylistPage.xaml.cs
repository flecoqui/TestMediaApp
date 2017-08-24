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
using AudioVideoPlayer.Companion;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
            ClearErrorMessage();
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
                    if (comboPlayList.SelectedIndex < 0)
                        comboPlayList.SelectedIndex = 0;
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
            ClearErrorMessage();

            // Register Network
            RegisterNetworkHelper();

        }

        /// <summary>
        /// Method OnNavigatedFrom
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {

            // Unregister Network
            UnregisterNetworkHelper();


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
        private bool SelectPlaylistWithName(string Name)
        {
            if (comboPlayList.Items.Count > 0)
            {

                RemoveButton.IsEnabled = true;
                int index = 0;
                foreach (var item in comboPlayList.Items)
                {
                    if (item is Models.PlayList)
                    {
                        Models.PlayList p = item as Models.PlayList;
                        if (p != null)
                        {
                            if (string.Equals(p.Name, Name))
                            {
                                comboPlayList.SelectedIndex = index;
                                return true;
                            }
                        }
                    }
                    index++;
                }
            }
            return false;
        }
        void ClearErrorMessage()
        {
            ErrorMessage.Text = "";
        }
        void SetErrorMessage(string text)
        {
            if(!string.IsNullOrEmpty(text))
                ErrorMessage.Text = text;
        }
        private async void CreatePlaylist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                Shell.Current.DisplayWaitRing = true;
                await Helpers.MediaHelper.CreateLocalPlaylist(ViewModelLocator.Settings.PlaylistName, ViewModelLocator.Settings.PlaylistFolder, ViewModelLocator.Settings.PlaylistFilters, ViewModelLocator.Settings.PlaylistPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                Shell.Current.DisplayWaitRing = false;
            }

        }

        private async void AddPlaylist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.FileTypeFilter.Add(".json");
            filePicker.FileTypeFilter.Add(".tma");
            filePicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            //filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            filePicker.SettingsIdentifier = "PlaylistPicker";
            filePicker.CommitButtonText = "Add JSON or TMA (TestMEdiaApp)  Playlist File to your list";
            
            ClearErrorMessage();

            var file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);
                try
                {
                    Shell.Current.DisplayWaitRing = true;
                    //Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);
                    Models.PlayList playlist = await Models.PlayList.GetNewPlaylist(file.Path);
                    if (playlist != null)
                    {
                        if (!IsThePlaylistNameUsed(playlist.Name))
                        {
                            ViewModels.ViewModel vm = this.DataContext as ViewModels.ViewModel;
                            if (vm != null)
                            {
                                ObservableCollection<Models.PlayList> PlayListList = vm.Settings.PlayListList;
                                PlayListList.Add(playlist);
                                vm.Settings.PlayListList = PlayListList;
                                if(!SelectPlaylistWithName(playlist.Name))
                                {
                                    ImportButton.IsEnabled = false;
                                    RemoveButton.IsEnabled = false;
                                }
                            }
                        }
                        else
                        {
                            SetErrorMessage("Playlist name already used");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                }
                finally
                {
                    Shell.Current.DisplayWaitRing = false;
                    //Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
                }
            }
        }

        private void AddMusicFolder_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            AddFolder(Helpers.MediaHelper.MediaType.Music, Helpers.MediaHelper.audioExts);

        }
        private void AddVideoFolder_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            AddFolder(Helpers.MediaHelper.MediaType.Video, Helpers.MediaHelper.videoExts);
        }
        private void AddPictureFolder_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            AddFolder(Helpers.MediaHelper.MediaType.Picture, Helpers.MediaHelper.pictureExts);
        }

        private async void AddFolder(Helpers.MediaHelper.MediaType mediaType, string Extensions)
        {
            string typeMedia = string.Empty;
            string filters = string.Empty;
            Windows.Storage.Pickers.PickerLocationId id = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            if (mediaType == Helpers.MediaHelper.MediaType.Music)
            {
                typeMedia = "music";
                id = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
                filters = Helpers.MediaHelper.audioExts;
            }
            else if (mediaType == Helpers.MediaHelper.MediaType.Video)
            {
                typeMedia = "video";
                id = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                filters = Helpers.MediaHelper.videoExts;
            }
            else if (mediaType == Helpers.MediaHelper.MediaType.Picture)
            {
                typeMedia = "picture";
                id = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                filters = Helpers.MediaHelper.pictureExts;
            }

            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            folderPicker.SuggestedStartLocation = id;
            folderPicker.SettingsIdentifier = "PlaylistPicker";
            folderPicker.CommitButtonText = "Select the folder to create a " + typeMedia + " Playlist";
            ClearErrorMessage();

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);
                try
                {
                    Shell.Current.DisplayWaitRing = true;
                    ViewModelLocator.Settings.PlaylistFolder = folder.Path;
                    ViewModelLocator.Settings.PlaylistName = await Helpers.MediaHelper.GetUniquePlaylistName(folder.Path);
                    ViewModelLocator.Settings.PlaylistFilters = filters;
                    ViewModelLocator.Settings.PlaylistPath = await Helpers.MediaHelper.GetUniquePlaylistPath(folder.Path);

                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
                }
                finally
                {
                    Shell.Current.DisplayWaitRing = false;
                }
            }
        }
        private async  void ImportPlaylist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ClearErrorMessage();

            if (comboPlayList.SelectedItem is Models.PlayList)
            {
                Models.PlayList p = comboPlayList.SelectedItem as Models.PlayList;
                if (p != null)
                {
                   Windows.Storage.StorageFolder playlistFolder = await Helpers.StorageHelper.GetFolder("playlist");
                    if (playlistFolder == null)
                    {
                        playlistFolder = await Helpers.StorageHelper.CreateLocalFolder("playlist");
                    }
                    if (playlistFolder != null)
                    {
                        string folderName = System.IO.Path.GetFileName(playlistFolder.Path);
                        string fileName = System.IO.Path.GetFileName(p.Path);

                        // Remove existing imported playlist
                        if (!string.IsNullOrEmpty(p.ImportedPath))
                            await Helpers.StorageHelper.RemoveFile(p.ImportedPath);

                        // Get a unique filename
                        string destFileName = fileName;
                        destFileName = await Helpers.StorageHelper.GetUniqueFileName(folderName, fileName);

                        Windows.Storage.StorageFile importedPlaylistfile = await Helpers.StorageHelper.CopyFileToFolder(p.Path, folderName,destFileName);
                        if(importedPlaylistfile!=null)
                        {
                            p.ImportedPath = importedPlaylistfile.Path;
                            ObservableCollection<Models.PlayList> pll = ViewModelLocator.Settings.PlayListList;
                            if ((pll != null) && (comboPlayList.SelectedIndex < pll.Count))
                            {
                                pll[comboPlayList.SelectedIndex] = p;
                                ViewModelLocator.Settings.PlayListList = pll;
                                if (!SelectPlaylistWithName(p.Name))
                                {
                                    ImportButton.IsEnabled = false;
                                    RemoveButton.IsEnabled = false;
                                }

                            }
                        }
                    }
                }
            }

        }
        private async void RemovePlaylist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ClearErrorMessage();

            if (comboPlayList.SelectedItem is Models.PlayList)
            {
                ObservableCollection<Models.PlayList> pll =  ViewModelLocator.Settings.PlayListList;
                if ((pll != null) && (comboPlayList.SelectedIndex < pll.Count))
                {
                    
                    Models.PlayList p = comboPlayList.SelectedItem as Models.PlayList;
                    if (p != null)
                    {
                        if(!string.IsNullOrEmpty(p.ImportedPath))
                        {
                            bool b = await Helpers.StorageHelper.RemoveFile(p.ImportedPath);
                        }
                    }
                    pll.RemoveAt(comboPlayList.SelectedIndex);
                }
                ViewModelLocator.Settings.PlayListList = pll;
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
            ClearErrorMessage();

            if (comboPlayList.SelectedItem is Models.PlayList)
            {
                Models.PlayList p = (Models.PlayList)comboPlayList.SelectedItem;
                if(p!=null)
                {
                    if(!string.IsNullOrEmpty(p.ImportedPath))
                    {
                        ViewModelLocator.Settings.CurrentPlayListPath = p.ImportedPath;
                        ViewModelLocator.Settings.CurrentPlayListIndex = comboPlayList.SelectedIndex;
                        ViewModelLocator.Settings.CurrentMediaPath = string.Empty;
                        ViewModelLocator.Settings.CurrentMediaIndex = p.Index;
                    }
                    else if (!string.IsNullOrEmpty(p.Path))
                    {
                        ViewModelLocator.Settings.CurrentPlayListPath = p.Path;
                        ViewModelLocator.Settings.CurrentPlayListIndex = comboPlayList.SelectedIndex;
                        ViewModelLocator.Settings.CurrentMediaPath = string.Empty;
                        ViewModelLocator.Settings.CurrentMediaIndex = p.Index;
                    }
                    ImportButton.IsEnabled = (((p.bImported == false) && (!p.Path.StartsWith("ms-appx://"))) ? true : false);
                    RemoveButton.IsEnabled = true;
                }
            }
        }





        #region Network

        Helpers.NetworkHelper networkHelper;

        bool RegisterNetworkHelper()
        {
            UnregisterNetworkHelper();
            if (networkHelper == null)
            {
                networkHelper = new Helpers.NetworkHelper();
                networkHelper.InternetConnectionChanged += NetworkHelper_InternetConnectionChanged;
                NetworkHelper_InternetConnectionChanged(this, Helpers.NetworkHelper.IsInternetAvailable());
            }
            return true;
        }
        bool IsNetworkRequired()
        {
            bool bNetworkRequired = false;
            string s = ViewModels.StaticSettingsViewModel.CurrentPlayListPath;
            if (!string.IsNullOrEmpty(s))
            {
                if (s.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    bNetworkRequired = true;
                else if (s.StartsWith("redirect://", StringComparison.OrdinalIgnoreCase))
                    bNetworkRequired = true;
                else if (s.StartsWith("redirects://", StringComparison.OrdinalIgnoreCase))
                    bNetworkRequired = true;

                var p = ViewModelLocator.Settings.PlayListList.FirstOrDefault(x => string.Equals(x.Path, s, StringComparison.OrdinalIgnoreCase) || string.Equals(x.ImportedPath, s, StringComparison.OrdinalIgnoreCase));
                if (p != null)
                {
                    if (p.bRemoteItem == true)
                        bNetworkRequired = true;
                }
            }
            return bNetworkRequired;
        }
        private async void NetworkHelper_InternetConnectionChanged(object sender, bool e)
        {
            if (e == true)
            {
                await Shell.Current.DisplayNetworkWarning(false, "");
            }
            else
            {
                if (IsNetworkRequired())
                {
                    await Shell.Current.DisplayNetworkWarning(true, "The current playlist: " + ViewModels.StaticSettingsViewModel.CurrentPlayListPath + " requires an internet connection");
                }
            }

        }

        bool UnregisterNetworkHelper()
        {
            if (networkHelper != null)
            {
                networkHelper.InternetConnectionChanged -= NetworkHelper_InternetConnectionChanged;
                networkHelper = null;
            }
            return true;
        }
        #endregion Network




    }


}
