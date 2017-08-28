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
        bool IsMusic(string filters)
        {
            if (!string.IsNullOrEmpty(filters))
            {

                string[] array = filters.Split();
                foreach (var v in array)
                {
                    if (Helpers.MediaHelper.audioExts.IndexOf(v) >= 0)
                        return true;
                }

            }
            return false;

        }
        bool IsPicture(string filters)
        {
            if (!string.IsNullOrEmpty(filters))
            {

                string[] array = filters.Split();
                foreach (var v in array)
                {
                    if (Helpers.MediaHelper.pictureExts.IndexOf(v) >= 0)
                        return true;
                }

            }
            return false;

        }
        bool IsVideo(string filters)
        {
            if (!string.IsNullOrEmpty(filters))
            {

                string[] array = filters.Split();
                foreach (var v in array)
                {
                    if (Helpers.MediaHelper.videoExts.IndexOf(v) >= 0)
                        return true;
                }

            }
            return false;

        }

        /// <summary>
        /// UpdateControls Method which update the controls on the page  
        /// </summary>
        async void UpdateControls(bool bDisable = false)
        {

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                 () =>
                 {
                     if (   (!string.IsNullOrEmpty(localPlaylistName.Text)) &&
                            (!string.IsNullOrEmpty(localPlaylistFilters.Text)) &&
                            (!string.IsNullOrEmpty(localPlaylistFolder.Text)) &&
                            (!string.IsNullOrEmpty(localPlaylistPath.Text)))

                     {
                         localPlaylistNameLabel.Visibility = Visibility.Visible;
                         localPlaylistName.Visibility = Visibility.Visible;
                         localPlaylistFiltersLabel.Visibility = Visibility.Visible;
                         localPlaylistFilters.Visibility = Visibility.Visible;
                         localPlaylistFolderLabel.Visibility = Visibility.Visible;
                         localPlaylistFolder.Visibility = Visibility.Visible;
                         localPlaylistPathLabel.Visibility = Visibility.Visible;
                         localPlaylistPath.Visibility = Visibility.Visible;

                         if (IsPicture(localPlaylistFilters.Text))
                         {
                             localPlaylistPeriodLabel.Visibility = Visibility.Visible;
                             localPlaylistPeriod.Visibility = Visibility.Visible;
                         }
                         else
                         {
                             localPlaylistPeriodLabel.Visibility = Visibility.Collapsed;
                             localPlaylistPeriod.Visibility = Visibility.Collapsed;
                         }


                         if ((IsMusic(localPlaylistFilters.Text))||
                             (IsVideo(localPlaylistFilters.Text)))
                         {
                             localPlaylistThumbnailLabel.Visibility = Visibility.Visible;
                             localPlaylistThumbnail.Visibility = Visibility.Visible;
                         }
                         else
                         {
                             localPlaylistThumbnailLabel.Visibility = Visibility.Collapsed;
                             localPlaylistThumbnail.Visibility = Visibility.Collapsed;

                         }

                         if (string.Equals(localPlaylistItemsCount.Text, "-1"))
                         {
                             localPlaylistItemsCountLabel.Visibility = Visibility.Collapsed;
                             localPlaylistItemsCount.Visibility = Visibility.Collapsed;
                         }
                         else
                         {
                             localPlaylistItemsCountLabel.Visibility = Visibility.Visible;
                             localPlaylistItemsCount.Visibility = Visibility.Visible;
                         }
                         localPlaylistCreation.IsEnabled = true;

                     }
                     else
                     {
                         localPlaylistNameLabel.Visibility = Visibility.Collapsed;
                         localPlaylistName.Visibility = Visibility.Collapsed;
                         localPlaylistFiltersLabel.Visibility = Visibility.Collapsed;
                         localPlaylistFilters.Visibility = Visibility.Collapsed;
                         localPlaylistFolderLabel.Visibility = Visibility.Collapsed;
                         localPlaylistFolder.Visibility = Visibility.Collapsed;
                         localPlaylistPathLabel.Visibility = Visibility.Collapsed;
                         localPlaylistPath.Visibility = Visibility.Collapsed;
                         localPlaylistPeriodLabel.Visibility = Visibility.Collapsed;
                         localPlaylistPeriod.Visibility = Visibility.Collapsed;
                         localPlaylistThumbnailLabel.Visibility = Visibility.Collapsed;
                         localPlaylistThumbnail.Visibility = Visibility.Collapsed;
                         localPlaylistItemsCountLabel.Visibility = Visibility.Collapsed;
                         localPlaylistItemsCount.Visibility = Visibility.Collapsed;

                         localPlaylistCreation.IsEnabled = false;
                     }


                     if ((!string.IsNullOrEmpty(cloudPlaylistName.Text)) &&
                            (!string.IsNullOrEmpty(cloudPlaylistFilters.Text)) &&
                            (!string.IsNullOrEmpty(cloudPlaylistPath.Text)))

                     {
                         cloudPlaylistNameLabel.Visibility = Visibility.Visible;
                         cloudPlaylistName.Visibility = Visibility.Visible;
                         cloudPlaylistFiltersLabel.Visibility = Visibility.Visible;
                         cloudPlaylistFilters.Visibility = Visibility.Visible;
                         cloudPlaylistPathLabel.Visibility = Visibility.Visible;
                         cloudPlaylistPath.Visibility = Visibility.Visible;

                         cloudPlaylistAccountNameLabel.Visibility = Visibility.Visible;
                         cloudPlaylistAccountName.Visibility = Visibility.Visible;
                         cloudPlaylistAccountKeyLabel.Visibility = Visibility.Visible;
                         cloudPlaylistAccountKey.Visibility = Visibility.Visible;
                         cloudPlaylistContainerLabel.Visibility = Visibility.Visible;
                         cloudPlaylistContainer.Visibility = Visibility.Visible;

                         if (IsPicture(cloudPlaylistFilters.Text))
                         {
                             cloudPlaylistPeriodLabel.Visibility = Visibility.Visible;
                             cloudPlaylistPeriod.Visibility = Visibility.Visible;
                         }
                         else
                         {
                             cloudPlaylistPeriodLabel.Visibility = Visibility.Collapsed;
                             cloudPlaylistPeriod.Visibility = Visibility.Collapsed;
                         }


                         if ((IsMusic(cloudPlaylistFilters.Text)) ||
                             (IsVideo(cloudPlaylistFilters.Text)))
                         {
                             cloudPlaylistThumbnailLabel.Visibility = Visibility.Visible;
                             cloudPlaylistThumbnail.Visibility = Visibility.Visible;
                         }
                         else
                         {
                             cloudPlaylistThumbnailLabel.Visibility = Visibility.Collapsed;
                             cloudPlaylistThumbnail.Visibility = Visibility.Collapsed;

                         }

                         if (string.Equals(cloudPlaylistItemsCount.Text, "-1"))
                         {
                             cloudPlaylistItemsCountLabel.Visibility = Visibility.Collapsed;
                             cloudPlaylistItemsCount.Visibility = Visibility.Collapsed;
                         }
                         else
                         {
                             cloudPlaylistItemsCountLabel.Visibility = Visibility.Visible;
                             cloudPlaylistItemsCount.Visibility = Visibility.Visible;
                         }
                         cloudPlaylistCreation.IsEnabled = true;

                     }
                     else
                     {
                         cloudPlaylistNameLabel.Visibility = Visibility.Collapsed;
                         cloudPlaylistName.Visibility = Visibility.Collapsed;
                         cloudPlaylistFiltersLabel.Visibility = Visibility.Collapsed;
                         cloudPlaylistFilters.Visibility = Visibility.Collapsed;
                         cloudPlaylistAccountNameLabel.Visibility = Visibility.Collapsed;
                         cloudPlaylistAccountName.Visibility = Visibility.Collapsed;
                         cloudPlaylistAccountKeyLabel.Visibility = Visibility.Collapsed;
                         cloudPlaylistAccountKey.Visibility = Visibility.Collapsed;
                         cloudPlaylistContainerLabel.Visibility = Visibility.Collapsed;
                         cloudPlaylistContainer.Visibility = Visibility.Collapsed;
                         cloudPlaylistPathLabel.Visibility = Visibility.Collapsed;
                         cloudPlaylistPath.Visibility = Visibility.Collapsed;
                         cloudPlaylistPeriodLabel.Visibility = Visibility.Collapsed;
                         cloudPlaylistPeriod.Visibility = Visibility.Collapsed;
                         cloudPlaylistThumbnailLabel.Visibility = Visibility.Collapsed;
                         cloudPlaylistThumbnail.Visibility = Visibility.Collapsed;
                         cloudPlaylistItemsCountLabel.Visibility = Visibility.Collapsed;
                         cloudPlaylistItemsCount.Visibility = Visibility.Collapsed;

                         cloudPlaylistCreation.IsEnabled = false;
                     }


                 });
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

            // Update playlist controls
            localPlaylistItemsCount.Text = "-1";
            UpdateControls();


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
                localPlaylistItemsCount.Text = "-1";
                if (!string.IsNullOrEmpty(ViewModelLocator.Settings.PlaylistName) &&
                    !string.IsNullOrEmpty(ViewModelLocator.Settings.PlaylistFolder) &&
                    !string.IsNullOrEmpty(ViewModelLocator.Settings.PlaylistFilters) &&
                    !string.IsNullOrEmpty(ViewModelLocator.Settings.PlaylistPath) 
                    )
                { 
                    int counter = await Helpers.MediaHelper.CreateLocalPlaylist(ViewModelLocator.Settings.PlaylistName, ViewModelLocator.Settings.PlaylistFolder, ViewModelLocator.Settings.PlaylistFilters, ViewModelLocator.Settings.CreateThumbnails, ViewModelLocator.Settings.SlideShowPeriod, ViewModelLocator.Settings.PlaylistPath);
                    localPlaylistItemsCount.Text = counter.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                Shell.Current.DisplayWaitRing = false;
                UpdateControls();
            }

        }
        private async void CreateCloudPlaylist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                Shell.Current.DisplayWaitRing = true;
                cloudPlaylistItemsCount.Text = "-1";
                if (!string.IsNullOrEmpty(ViewModelLocator.Settings.CloudPlaylistName) &&
                    !string.IsNullOrEmpty(ViewModelLocator.Settings.AzureAccountKey) &&
                    !string.IsNullOrEmpty(ViewModelLocator.Settings.AzureAccountName) &&
                    !string.IsNullOrEmpty(ViewModelLocator.Settings.AzureContainer) &&
                    !string.IsNullOrEmpty(ViewModelLocator.Settings.CloudPlaylistFilters) &&
                    !string.IsNullOrEmpty(ViewModelLocator.Settings.CloudPlaylistPath)
                    )
                {
                    int counter = await Helpers.MediaHelper.CreateCloudPlaylist(ViewModelLocator.Settings.CloudPlaylistName, ViewModelLocator.Settings.AzureAccountName, ViewModelLocator.Settings.AzureAccountKey, ViewModelLocator.Settings.AzureContainer, ViewModelLocator.Settings.CloudPlaylistFilters, ViewModelLocator.Settings.CloudCreateThumbnails, ViewModelLocator.Settings.CloudSlideShowPeriod, ViewModelLocator.Settings.CloudPlaylistPath);
                    cloudPlaylistItemsCount.Text = counter.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                Shell.Current.DisplayWaitRing = false;
                UpdateControls();
            }

        }
        private async void AddPlaylist_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            filePicker.FileTypeFilter.Add(".json");
            filePicker.FileTypeFilter.Add(".tma");
            filePicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            //filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
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
                            SetErrorMessage("Playlist name already used");
                    }
                    else
                        SetErrorMessage("Error while parsing the playlist file");
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
        private void AddMusicContainer_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            AddContainer(Helpers.MediaHelper.MediaType.Music, Helpers.MediaHelper.audioExts);

        }
        private void AddVideoContainer_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            AddContainer(Helpers.MediaHelper.MediaType.Video, Helpers.MediaHelper.videoExts);
        }
        private void AddPictureContainer_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            AddContainer(Helpers.MediaHelper.MediaType.Picture, Helpers.MediaHelper.pictureExts);
        }

        private async void AddContainer(Helpers.MediaHelper.MediaType mediaType, string Extensions)
        {
            string typeMedia = string.Empty;
            string filters = string.Empty;
            cloudPlaylistItemsCount.Text = "-1";

            if (mediaType == Helpers.MediaHelper.MediaType.Music)
            {
                typeMedia = "Music";
                filters = Helpers.MediaHelper.audioExts;
            }
            else if (mediaType == Helpers.MediaHelper.MediaType.Video)
            {
                typeMedia = "Video";
                filters = Helpers.MediaHelper.videoExts;
            }
            else if (mediaType == Helpers.MediaHelper.MediaType.Picture)
            {
                typeMedia = "Picture";
                filters = Helpers.MediaHelper.pictureExts;
            }

            ClearErrorMessage();

            try
            {
                Shell.Current.DisplayWaitRing = true;
                string pName = "Cloud" + typeMedia + "Playlist";
                ViewModelLocator.Settings.CloudPlaylistName = await Helpers.MediaHelper.GetUniquePlaylistName(pName);
                ViewModelLocator.Settings.CloudPlaylistFilters = filters;
                ViewModelLocator.Settings.CloudPlaylistPath = await Helpers.MediaHelper.GetUniquePlaylistPath(pName);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
            }
            finally
            {
                Shell.Current.DisplayWaitRing = false;
                UpdateControls();
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
            localPlaylistItemsCount.Text = "-1";

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
                    UpdateControls();
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

        private async void cloudPlaylistName_LostFocus(object sender, RoutedEventArgs e)
        {
                ViewModelLocator.Settings.CloudPlaylistPath = await Helpers.MediaHelper.GetUniquePlaylistPath(cloudPlaylistName.Text);
        }
        private async void localPlaylistName_LostFocus(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.Settings.PlaylistPath = await Helpers.MediaHelper.GetUniquePlaylistPath(localPlaylistName.Text);
        }
    }


}
