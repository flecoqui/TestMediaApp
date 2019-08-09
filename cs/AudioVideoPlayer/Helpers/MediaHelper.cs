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
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.UserDataTasks;
using Windows.Foundation;
using Windows.UI.Composition.Interactions;

namespace AudioVideoPlayer.Helpers
{
    public static class MediaHelper
    {
        const string headerStart = "{ \"Groups\": [{\"UniqueId\": \"audio_video_picture\",\"Title\": \"";
        const string headerEnd = "\",\"Category\": \"Windows 10 Audio Video Tests\",\"ImagePath\": \"ms-appx:///Assets/AudioVideo.png\",\"Description\": \"Windows 10 Audio Video Tests\",\"Items\": [\r\n";
        const string videoItem = " \"UniqueId\": \"{0}\", \"Comment\": \"\", \"Title\": \"{1}\", \"ImagePath\": \"ms-appx:///Assets/VIDEO.png\",\"Description\": \"\", \"Content\": \"{2}\", \"PosterContent\": \"{3}\",\"Start\": \"0\",\"Duration\": \"0\",\"PlayReadyUrl\": \"null\",\"PlayReadyCustomData\": \"null\",\"BackgroundAudio\": true, \"Category\": \"{5}\"";
        const string musicItem = " \"UniqueId\": \"{0}\", \"Comment\": \"\", \"Title\": \"{1}\", \"ImagePath\": \"ms-appx:///Assets/MUSIC.png\",\"Description\": \"\", \"Content\": \"{2}\", \"PosterContent\": \"{3}\",\"Start\": \"0\",\"Duration\": \"0\",\"PlayReadyUrl\": \"null\",\"PlayReadyCustomData\": \"null\",\"BackgroundAudio\": true, \"Artist\": \"{4}\", \"Album\": \"{5}\", \"Category\": \"{6}\"";
        const string pictureItem = " \"UniqueId\": \"{0}\", \"Comment\": \"\", \"Title\": \"{1}\", \"ImagePath\": \"ms-appx:///Assets/PHOTO.png\",\"Description\": \"\", \"Content\": \"{2}\", \"PosterContent\": \"{3}\",\"Start\": \"0\",\"Duration\": \"{4}\",\"PlayReadyUrl\": \"null\",\"PlayReadyCustomData\": \"null\",\"BackgroundAudio\": truem, \"Category\": \"{5}\"";
        const string footer = "\r\n]}]}";
        public const string videoExts = ".asf;.avi;.ismv;.ts;.mkv;.mov;.mp4;.wmv;";
        public const string audioExts = ".mp3;.aac;.wma;.wav;.flac;.m4a;";
        public const string pictureExts = ".png;.jpg";
        public enum MediaType
        {
            Music = 0,
            Video,
            Picture
        }
        public static async System.Threading.Tasks.Task<bool> IsFile(string filePath)
        {
            try
            {
                Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(filePath);
                if (file != null)
                    return true;
            }
            catch (Exception)
            {
                //System.Diagnostics.Debug.WriteLine("Exception while reading file " + filePath + " : " + Ex.Message);
            }
            return false;

        }
        public static async System.Threading.Tasks.Task<Windows.Storage.StorageFile> CreateFile(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    string folderPath = System.IO.Path.GetDirectoryName(filePath);
                    string fileName = System.IO.Path.GetFileName(filePath);
                    if (!string.IsNullOrEmpty(fileName))
                    {
//                        Windows.Storage.StorageFolder folder = Windows.Storage.KnownFolders.DocumentsLibrary;
                        Windows.Storage.StorageFolder folder = Windows.Storage.KnownFolders.Playlists;
                        if (!string.IsNullOrEmpty(folderPath))
                            folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(folderPath);
                        if (folder != null)
                        {
                            var f = await Helpers.StorageHelper.GetFile(folder, fileName);
                            if (f != null)
                                await f.DeleteAsync();
                            
                            return await folder.CreateFileAsync(fileName, Windows.Storage.CreationCollisionOption.ReplaceExisting);
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while creating file " + filePath + " : " + Ex.Message);
            }
            return null;

        }
        public static async System.Threading.Tasks.Task<string> GetUniquePlaylistName(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        int index = 0;
                        string ext = ".tma";
                        Windows.Storage.StorageFile f = null;
                        string name = fileName + ext;
//                        while ((f = await StorageHelper.GetFile(Windows.Storage.KnownFolders.DocumentsLibrary, name)) != null)
                        while ((f = await StorageHelper.GetFile(Windows.Storage.KnownFolders.Playlists, name)) != null)
                        {
                                name = fileName + "_" + index.ToString() + (string.IsNullOrEmpty(ext) ? "" : ext);
                            index++;
                        }
                        return System.IO.Path.GetFileNameWithoutExtension(name);
                    }
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while creating file " + filePath + " : " + Ex.Message);
            }
            return null;

        }
        public static async System.Threading.Tasks.Task<string> GetUniquePlaylistPath(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        int index = 0;
                        string ext = ".tma";
                        Windows.Storage.StorageFile f = null;
                        string name = fileName + ext;

                        StorageFolder folder = await GetPlaylistsFolder();
                        if (folder != null)
                        {
                            while ((f = await StorageHelper.GetFile(folder, name)) != null)
                            {
                                name = fileName + "_" + index.ToString() + (string.IsNullOrEmpty(ext) ? "" : ext);
                                index++;
                            }
                            return System.IO.Path.Combine(folder.Path, name);
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while creating file " + filePath + " : " + Ex.Message);
            }
            return null;

        }
        public static async System.Threading.Tasks.Task<string> GetPlaylistPath(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath))
                {
//                    string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    string fileName = filePath;
                    if (!string.IsNullOrEmpty(fileName))
                    {

                        string ext = ".tma";                        
                        string name = fileName + ext;

                        StorageFolder folder = await GetPlaylistsFolder();
                        if (folder != null)
                        {
                            return System.IO.Path.Combine(folder.Path, name);
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while creating file " + filePath + " : " + Ex.Message);
            }
            return null;

        }
        public static async System.Threading.Tasks.Task<bool> IsFolder(string folderPath)
        {
            try
            {
                Windows.Storage.StorageFolder folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(folderPath);
                if (folder != null)
                    return true;
            }
            catch (Exception)
            {
                //System.Diagnostics.Debug.WriteLine("Exception while reading folder " + folderPath + " : " + Ex.Message);
            }
            return false;

        }
        public static async System.Threading.Tasks.Task<int> RenameDirectory(int counter, string PlaylistName,  Stream writer, string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = System.IO.Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                counter = await RenameFile(counter, PlaylistName, writer, fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                counter = await RenameDirectory(counter, PlaylistName, writer, subdirectory);
            Windows.Storage.StorageFolder Folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(targetDirectory);
            if (Folder != null)
            {
                string orgname = Folder.Name;
                if (IsNameChangeRequired(orgname))
                {
                    string newname = GetNewName(orgname);
                    if (!string.IsNullOrEmpty(newname))
                    {
                        AppendText(writer, orgname + " -> " + newname + "\r\n");
                        //await Folder.RenameAsync(newname);
                        counter++;
                    }
                }
            }

            return counter;
        }
        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        public static async System.Threading.Tasks.Task<int> ProcessDirectory(string InitialFolder, int counter, string PlaylistName, string extensions, bool bCreateThumbnails, int SlideShowPeriod, Stream writer, string targetDirectory)
        {

            // Process the list of files found in the directory.
            string[] fileEntries = System.IO.Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                if (LocalPlaylistStopRequested == true)
                    break;
                counter = await ProcessFile(InitialFolder, counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, writer, fileName);
            }
            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                if (LocalPlaylistStopRequested == true)
                    break;
                counter = await ProcessDirectory(InitialFolder, counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, writer, subdirectory);
            }
            return counter;
        }
        static bool IsMusicFile(string ext)
        {
            if (audioExts.IndexOf(ext, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }
        static bool IsPictureFile(string ext)
        {
            if (pictureExts.IndexOf(ext, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }
        static bool IsVideoFile(string ext)
        {
            if (videoExts.IndexOf(ext, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }
        public static async System.Threading.Tasks.Task<string> SaveThumbnailToFileAsync(StorageFolder pictureFolder, string imageName, StorageItemThumbnail image, bool bOverWrite = false)
        {
            try
            {
                if (pictureFolder != null)
                {
                    // if file already exists return 
                    var file = await Helpers.StorageHelper.GetFile(pictureFolder, imageName + ".jpg");
                    if ((file != null)&&(bOverWrite == false))
                        return file.Path;

                    file = await pictureFolder.CreateFileAsync(imageName + ".jpg", CreationCollisionOption.ReplaceExisting);
                    if (file != null)
                        using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            Windows.Storage.Streams.Buffer MyBuffer = new Windows.Storage.Streams.Buffer(Convert.ToUInt32(image.Size));
                            if (MyBuffer != null)
                            {
                                IBuffer iBuf = await image.ReadAsync(MyBuffer, MyBuffer.Capacity, InputStreamOptions.None);
                                if(iBuf!=null)
                                {
                                    await stream.WriteAsync(iBuf);
                                }
                            }

                            return file.Path;
                        }
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while creating thumbnail file: " + ex.Message);
            }
            return null;
        }
        public static async System.Threading.Tasks.Task<StorageFolder> GetThumbnailFolder(string PlaylistName)
        {
            try
            {
                if (!string.IsNullOrEmpty(PlaylistName))
                {
                    //Windows.Storage.StorageFolder dfolder = Windows.Storage.KnownFolders.DocumentsLibrary;
                    Windows.Storage.StorageFolder dfolder = Windows.Storage.KnownFolders.Playlists;
                    if (dfolder != null)
                    {
                        Windows.Storage.StorageFolder afolder = await StorageHelper.GetFolder(dfolder, Information.SystemInformation.ApplicationName);
                        if (afolder == null)
                        {
                            afolder = await dfolder.CreateFolderAsync(Information.SystemInformation.ApplicationName);
                        }
                        if (afolder != null)
                        {
                            Windows.Storage.StorageFolder pfolder = await StorageHelper.GetFolder(afolder, PlaylistName);
                            if (pfolder == null)
                            {
                                pfolder = await afolder.CreateFolderAsync(PlaylistName);
                            }
                            return pfolder;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while getting playlist thumbnail folder: " + ex.Message );
            }
            return null;
        }
        public static async System.Threading.Tasks.Task<StorageFolder> GetPlaylistsFolder()
        {
            try
            {
//                Windows.Storage.StorageFolder dfolder = Windows.Storage.KnownFolders.DocumentsLibrary;
                Windows.Storage.StorageFolder dfolder = Windows.Storage.KnownFolders.Playlists;
                if (dfolder != null)
                {
                    Windows.Storage.StorageFolder afolder = await StorageHelper.GetFolder(dfolder, Information.SystemInformation.ApplicationName);
                    if (afolder == null)
                    {
                        afolder = await dfolder.CreateFolderAsync(Information.SystemInformation.ApplicationName);
                    }
                    return afolder;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while getting playlist folder: " + ex.Message);
            }
            return null;
        }
        static string GetTextOld(string Text)
        {
            return  Text.Replace('"', ' ').Replace('\\', ' ').Replace('/', ' ').Replace('*', ' ').Replace("'", " ");

        }
        static string GetText(string Text)
        {
            return Text.Replace('"', ' ').Replace('\\', ' ').Replace('/', ' ').Replace('*', ' ');

        }
        static string GetTextForFileName(string Text)
        {
            return Text.Replace('"', '_').Replace("'", "_").Replace(',', '_').Replace('\\', '_').Replace('/', '_').Replace(' ', '_').Replace(':', '_').Replace('.', '_').Replace(';', '_').Replace('*', '_').Replace('<', '_').Replace('>', '_').Replace('|', '_').Replace('?', '_');
        }
        static async System.Threading.Tasks.Task<string> GetMusicPosterFile(string path)
        {
            string result = "ms-appx:///Assets/MUSIC.png";
            if (!string.IsNullOrEmpty(path)) 
            {
                int pos = path.LastIndexOf('\\');
                if (pos > 0)
                {
                    string s = path.Substring(0, pos + 1) + "artwork.jpg";
                    var file = await StorageFile.GetFileFromPathAsync(path);
                    if (file!=null)
                        result = s;
                }
            }            
            return result;
        }
        public static async System.Threading.Tasks.Task<int> ProcessFile(string InitialFolder, int counter, string PlaylistName, string extensions, bool bCreateThumbnails, int SlideShowPeriod, Stream writer, string path)
        {
            try
            {
                string ext = System.IO.Path.GetExtension(path);
                if ((!string.IsNullOrEmpty(ext)) && (extensions.IndexOf(ext, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Windows.Storage.StorageFile File = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                    if (File != null)
                    {
                        string artist = string.Empty;
                        string album = string.Empty;
                        string category = string.Empty;
                        int track = -1;
                        string posteruri = "";// GetPosterFile(path, extensions);
                        string title = "";//GetFileName(path);
                        string fullPath = System.IO.Path.GetFullPath(path);
                        category = GetCategoryFromPath(InitialFolder, fullPath);

                        if (IsMusicFile(ext))
                        {
                            posteruri = "ms-appx:///Assets/MUSIC.png";
                            Windows.Storage.FileProperties.MusicProperties m = await File.Properties.GetMusicPropertiesAsync();
                            if (m != null)
                            {
                                artist = GetText(m.Artist);
                                album = GetText(m.Album);
                                title = GetText(m.Title);
                                track = (int)m.TrackNumber;
                            }

                            if (string.IsNullOrEmpty(title))
                                title = System.IO.Path.GetFileNameWithoutExtension(path);
                            else
                            {
                                if (track > 0)
                                    title = track.ToString("00") + "-" + artist + "-" + album + "-" + title;
                                else
                                    title = artist + "-" + album + "-" + title;
                            }
                            using (var thumbnail = await File.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.MusicView, 300))
                            {
                                if (thumbnail != null && thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Image)
                                {

                                    if (bCreateThumbnails)
                                    {
                                        StorageFolder f = await GetThumbnailFolder(PlaylistName);
                                        string name = string.Empty;
                                        if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(album))
                                            name = GetTextForFileName(artist + "_" + album);
                                        if (string.IsNullOrEmpty(name))
                                            name = System.IO.Path.GetFileNameWithoutExtension(path);
                                        if ((f != null) && (!string.IsNullOrEmpty(name)))
                                        {
                                            string s = await SaveThumbnailToFileAsync(f, "tb_" + name, thumbnail);
                                            if (!string.IsNullOrEmpty(s))
                                            {
                                                posteruri = s;
                                            }
                                        }
                                    }
                                    else
                                        posteruri = path;
                                }
                                else
                                {
                                    posteruri = await GetMusicPosterFile(path);
                                }
                            }
                        }
                        else if (IsPictureFile(ext))
                        {
                            Windows.Storage.FileProperties.ImageProperties p = await File.Properties.GetImagePropertiesAsync();
                            if(p!=null)
                                title = GetText(p.Title);
                            posteruri = path;
                            if (string.IsNullOrEmpty(title))
                                title = System.IO.Path.GetFileNameWithoutExtension(path);

                        }
                        else if (IsVideoFile(ext))
                        {
                            Windows.Storage.FileProperties.VideoProperties v = await File.Properties.GetVideoPropertiesAsync();
                            if(v!=null)
                                title = GetText(v.Title);
                            posteruri = "ms-appx:///Assets/VIDEO.png";
                            if (string.IsNullOrEmpty(title))
                                title = System.IO.Path.GetFileNameWithoutExtension(path);
                            using (var thumbnail = await File.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.VideosView, 300))
                            {
                                if (thumbnail != null && thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Image)
                                {
                                    if (bCreateThumbnails)
                                    {
                                        StorageFolder f = await GetThumbnailFolder(PlaylistName);
                                        string name = System.IO.Path.GetFileNameWithoutExtension(path);
                                        if ((f != null) && (!string.IsNullOrEmpty(name)))
                                        {
                                            string s = await SaveThumbnailToFileAsync(f, "tb_" + name, thumbnail);
                                            if (!string.IsNullOrEmpty(s))
                                            {
                                                posteruri = s;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        posteruri = path;
                                    }
                                }
                                else
                                {
                                    //Error Message here
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(title))
                        {
                            string uri = path;
                            if (counter == 0)
                            {
                                if (writer != null)
                                {
                                    string header = headerStart + PlaylistName + headerEnd;
                                    string s = header;
                                    //await Windows.Storage.FileIO.AppendTextAsync(writer, header);
                                    //await Windows.Storage.FileIO.AppendTextAsync(writer, "{");
                                    s += "{";
                                    uri = "file://" + uri.Replace("\\", "\\\\");
                                    posteruri = "file://" + posteruri.Replace("\\", "\\\\");
                                    if (IsPictureFile(ext))
                                        s += string.Format(pictureItem, counter.ToString(), title, uri, posteruri, SlideShowPeriod.ToString(), category);
                                    else if (IsMusicFile(ext))
                                        s += string.Format(musicItem, counter.ToString(), title, uri, posteruri,artist,album, category);
                                    else 
                                        s += string.Format(videoItem, counter.ToString(), title, uri, posteruri,category);
                                    // await Windows.Storage.FileIO.AppendTextAsync(writer, string.Format(pictureItem, counter.ToString(), title, uri, posteruri));
                                    //  await Windows.Storage.FileIO.AppendTextAsync(writer, string.Format(audioItem, counter.ToString(), title, uri, posteruri));
                                    // await Windows.Storage.FileIO.AppendTextAsync(writer, "}");
                                    s += "}";
                                    AppendText(writer, s);
                                    //await Windows.Storage.FileIO.AppendTextAsync(writer, s);
                                }
                            }
                            else
                            {
                                if (writer != null)
                                {

                                    //await Windows.Storage.FileIO.AppendTextAsync(writer, ",\r\n");
                                    string s = ",\r\n";
                                    // await Windows.Storage.FileIO.AppendTextAsync(writer, "{");
                                    s += "{";
                                    uri = "file://" + uri.Replace("\\", "\\\\");
                                    posteruri = "file://" + posteruri.Replace("\\", "\\\\");
                                    if (IsPictureFile(ext))
                                        s += string.Format(pictureItem, counter.ToString(), title, uri, posteruri, SlideShowPeriod.ToString(), category);
                                    else if (IsMusicFile(ext))
                                        s += string.Format(musicItem, counter.ToString(), title, uri, posteruri, artist, album, category);
                                    else 
                                        s += string.Format(videoItem, counter.ToString(), title, uri, posteruri, category);
                                    //await Windows.Storage.FileIO.AppendTextAsync(writer, "}");
                                    s += "}";
                                    AppendText(writer, s);
//                                    await Windows.Storage.FileIO.AppendTextAsync(writer, s);
                                }
                            }
                            counter++;
                            LocalPlaylistCounter++;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while processing file: " + ex.Message);
            }
            return counter;
        }
        public static async System.Threading.Tasks.Task<int> RenameFile(int counter, string PlaylistName, Stream writer, string path)
        {
            try
            {
                Windows.Storage.StorageFile File = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                if (File != null)
                {
                    string orgname = File.Name;
                    if (IsNameChangeRequired(orgname))
                    {
                        string newname = GetNewName(orgname);
                        if (!string.IsNullOrEmpty(newname))
                        {
                            AppendText(writer, orgname + " -> " + newname + "\r\n");

                            //await File.RenameAsync(newname);
                            counter++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while processing file: " + ex.Message);
            }
            return counter;
        }
        public static async System.Threading.Tasks.Task<int> CreateEmptyPlaylist(string PlaylistName, string outputFile)
        {
            try
            {
                int counter = 0;
                Windows.Storage.StorageFile File = await CreateFile(outputFile);
                if (File != null)
                {
                    Stream stream = await File.OpenStreamForWriteAsync();
                    if (stream != null)
                    {
                        string header = headerStart + PlaylistName + headerEnd;

                        AppendText(stream, header);
                        AppendText(stream, footer);
                        stream.Flush();
                        System.Diagnostics.Debug.WriteLine(counter.ToString() + " files discovered on local storage\n");
                        return counter;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while discovering media file on local hard drive:" + ex.Message);
            }
            return -1;
        }
        public static async System.Threading.Tasks.Task<int> SavePlaylist(string PlaylistName, string outputFile, ObservableCollection<DataModel.MediaItem> list)
        {
            try
            {
                int counter = 0;
                Windows.Storage.StorageFile File = await CreateFile(outputFile);
                if (File != null)
                {
                    Stream stream = await File.OpenStreamForWriteAsync();
                    if (stream != null)
                    {
                        int SlideShowPeriod = 10;
                        string header = headerStart + PlaylistName + headerEnd;
                        string ext = string.Empty;
                        AppendText(stream, header);
                        foreach(var item in list)
                        {
                            if (counter == 0)
                            {
                                string s = "{";
                                ext = GetExtension(item.Content);
                                if (IsPictureFile(ext))
                                    s += string.Format(pictureItem, counter.ToString(), item.Title, item.Content, item.PosterContent, SlideShowPeriod.ToString(),item.Category);
                                else if (IsMusicFile(ext))
                                    s += string.Format(musicItem, counter.ToString(), item.Title, item.Content, item.PosterContent,item.Artist,item.Album, item.Category);
                                else
                                    s += string.Format(videoItem, counter.ToString(), item.Title, item.Content, item.PosterContent, item.Category);
                                s += "}";

                                AppendText(stream, s);
                            }
                            else
                            {
                                string s = ",\r\n";
                                s += "{";
                                ext = GetExtension(item.Content);
                                if (IsPictureFile(ext))
                                    s += string.Format(pictureItem, counter.ToString(), item.Title, item.Content, item.PosterContent, SlideShowPeriod.ToString(), item.Category);
                                else if (IsMusicFile(ext))
                                    s += string.Format(musicItem, counter.ToString(), item.Title, item.Content, item.PosterContent, item.Artist, item.Album, item.Category);
                                else
                                    s += string.Format(videoItem, counter.ToString(), item.Title, item.Content, item.PosterContent, item.Category);
                                s += "}";
                                AppendText(stream, s);
                            }
                            counter++;
                        }
                        AppendText(stream, footer);
                        stream.Flush();
                        System.Diagnostics.Debug.WriteLine(counter.ToString() + " files discovered on local storage\n");
                        return counter;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while discovering media file on local hard drive:" + ex.Message);
            }
            return -1;
        }
        private static int localPlaylistCounter;
        public static int LocalPlaylistCounter
        {
            set
            {
                if (localPlaylistCounter != value)
                {
                    localPlaylistCounter = value;
                    if (LocalItemDiscovered != null)
                        LocalItemDiscovered(LocalPlayListTask, value);
                }
            }
            get
            {
                return localPlaylistCounter;
            }
        }
        static System.Threading.Tasks.Task LocalPlayListTask = null;
        private static bool localPlaylistRunning = false;
        public static bool LocalPlaylistRunning
        {
            set
            {
                if (localPlaylistRunning != value)
                {
                    localPlaylistRunning = value;
                    if (LocalTaskStatus != null)
                        LocalTaskStatus(LocalPlayListTask, value);
                }
            }
            get
            {
                return localPlaylistRunning;
            }
        }

        static bool LocalPlaylistStopRequested = false;
        //
        // Summary:
        //     Raised when a local item is discovered
        public  static event TypedEventHandler<System.Threading.Tasks.Task, int> LocalItemDiscovered;
        //
        // Summary:
        //     Raised when the local Playlist Creation Task status change 
        public static event TypedEventHandler<System.Threading.Tasks.Task, bool> LocalTaskStatus;
        public static bool IsLocalPlaylistTaskRunning()
        {
            if(LocalPlayListTask !=null)
            {
                //if ((LocalPlayListTask.Status == TaskStatus.Running)
                    if(LocalPlaylistRunning == true)
                        return true;
            }
            return false;
        }
        public static async System.Threading.Tasks.Task<bool> StopLocalPlaylistTask()
        {

            if (IsLocalPlaylistTaskRunning())
            {
                LocalPlaylistStopRequested = true;
                while (IsLocalPlaylistTaskRunning())
                {
                    await System.Threading.Tasks.Task.Delay(1000);
                }
                LocalPlayListTask = null;
                return true;
            }
            LocalPlayListTask = null;
            return false;

        }
        public static async System.Threading.Tasks.Task<bool> StartLocalPlaylistTask(string PlaylistName, string folderName, string extensions, bool bCreateThumbnails, int SlideShowPeriod, string outputFile)
        {
            if(LocalPlayListTask != null)
            {
                await StopLocalPlaylistTask();
            }
            if(LocalPlayListTask == null)
            {
               LocalPlayListTask = new Task(async () =>
               {
                   try
                   {
                       LocalPlaylistRunning = true;
                       LocalPlaylistStopRequested = false;
                       LocalPlaylistCounter = 0;
                       int counter = 0;
                       Windows.Storage.StorageFile File = await CreateFile(outputFile);
                       if (File != null)
                       {
                           Stream stream = await File.OpenStreamForWriteAsync();
                           if (stream != null)
                           {
                               if (await IsFile(folderName) == true)
                               {
                                   counter = await ProcessFile(folderName, counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, stream, folderName);
                               }
                               else if (await IsFolder(folderName))
                               {
                                   // This path is a directory
                                   counter = await ProcessDirectory(folderName, counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, stream, folderName);
                               }
                               else
                               {
                                   System.Diagnostics.Debug.WriteLine("{0} is not a valid file or directory.", folderName);
                               }
                               //await Windows.Storage.FileIO.AppendTextAsync(File, footer);
                               AppendText(stream, footer);
                               stream.Flush();
                               System.Diagnostics.Debug.WriteLine(counter.ToString() + " files discovered on local storage\n");
                           }
                       }
                   }
                   catch (Exception ex)
                   {
                       System.Diagnostics.Debug.WriteLine("Exception while discovering media file on local hard drive:" + ex.Message);
                   }
                   finally
                   {
                       LocalPlaylistRunning = false;
                       LocalPlaylistStopRequested = false;
                   }

               });
               LocalPlayListTask.Start();
               return true;
            }
            return false;
        }
        public static async System.Threading.Tasks.Task<int> CreateLocalPlaylist(string PlaylistName, string folderName, string extensions, bool bCreateThumbnails , int SlideShowPeriod , string outputFile )
        {
            try
            {
                int counter = 0;
                Windows.Storage.StorageFile File = await CreateFile(outputFile);
                if (File != null)
                {
                    Stream stream = await File.OpenStreamForWriteAsync();
                    if (stream != null)
                    {
                        if (await IsFile(folderName) == true)
                        {
                            counter = await ProcessFile(folderName, counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, stream, folderName);
                        }
                        else if (await IsFolder(folderName))
                        {
                            // This path is a directory
                            counter = await ProcessDirectory(folderName, counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, stream, folderName);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("{0} is not a valid file or directory.", folderName);
                            return -1;
                        }
                        //await Windows.Storage.FileIO.AppendTextAsync(File, footer);
                        AppendText(stream, footer);
                        stream.Flush();
                        System.Diagnostics.Debug.WriteLine(counter.ToString() + " files discovered on local storage\n");
                        return counter;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while discovering media file on local hard drive:" + ex.Message);
            }
            return -1;
        }
        public static async System.Threading.Tasks.Task<int> RenameLocalPlaylist(string PlaylistName, string folderName, string outputFile)
        {
            try
            {
                int counter = 0;
                Windows.Storage.StorageFile File = await CreateFile(outputFile);
                if (File != null)
                {
                    Stream stream = await File.OpenStreamForWriteAsync();
                    if (stream != null)
                    {
                        if (await IsFile(folderName) == true)
                        {
                            counter = await RenameFile(counter, PlaylistName, stream, folderName);
                        }
                        else if (await IsFolder(folderName))
                        {
                            // This path is a directory
                            counter = await RenameDirectory(counter, PlaylistName, stream, folderName);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("{0} is not a valid file or directory.", folderName);
                            return -1;
                        }
                        stream.Flush();
                        System.Diagnostics.Debug.WriteLine(counter.ToString() + " files discovered on local storage\n");
                        return counter;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while discovering media file on local hard drive:" + ex.Message);
            }
            return -1;
        }
        static string GetExtension(string uri)
        {
            string result = string.Empty;
            int pos = 0;
            if ((pos = uri.LastIndexOf(".")) > 0)
            {
                if (pos + 1 < uri.Length)
                    result = uri.Substring(pos + 1);
            }
            return result;
        }
        static string GetUriFileName(string uri)
        {
            string result = string.Empty;
            int pos = 0;
            if ((pos = uri.LastIndexOf("/")) > 0)
            {
                if (pos + 1 < uri.Length)
                    result = uri.Substring(pos + 1);
            }
            return result;
        }
        static string GetFileName(string uri)
        {
            string result = string.Empty;
            int pos = 0;
            if ((pos = uri.LastIndexOf("\\")) > 0)
            {
                if (pos + 1 < uri.Length)
                    result = uri.Substring(pos + 1);
            }
            return result;
        }
        static string GetCategoryFromPath(string initialFolder, string path)
        {
            string result = string.Empty;
            if(!string.IsNullOrEmpty(initialFolder) &&
                !string.IsNullOrEmpty(path))
            {
                int pos = path.IndexOf(initialFolder);
                if(pos >= 0)
                {
                    int lastPos = path.LastIndexOf("\\");
                    result = path.Substring(pos + initialFolder.Length, path.Length - (pos + initialFolder.Length + (path.Length - lastPos)));
                    result = result.Replace('\\', '/');
                }
            }
            return result;
        }
        static string GetCategoryFromUri(string initialFolder, string uri)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(initialFolder) &&
                !string.IsNullOrEmpty(uri))
            {
                int pos = uri.IndexOf(initialFolder);
                if (pos >= 0)
                {
                    int lastPos = uri.LastIndexOf("/");
                    result = uri.Substring(pos + initialFolder.Length, uri.Length - (pos + initialFolder.Length + (uri.Length - lastPos)));
                }
            }
            return result;
        }
        static string GetArtistFromUri(string initialFolder, string uri)
        {
            string result = GetCategoryFromUri(initialFolder,uri);
            if (!string.IsNullOrEmpty(result))
            {
                int lastPos = result.LastIndexOf("/");
                if((lastPos == (result.Length-1)) && (lastPos > 0))
                    lastPos = result.LastIndexOf("/",lastPos-1);
                if (lastPos > 0)
                {
                    int pos = result.LastIndexOf("/", lastPos - 1);
                    if (pos >= 0)
                    {
                        result = result.Substring(pos + 1, lastPos - pos -1);
                    }
                }
            }
            return result;
        }
        static string GetAlbumFromUri(string initialFolder, string uri)
        {
            string result = GetCategoryFromUri(initialFolder, uri);
            if (!string.IsNullOrEmpty(result))
            {
                int lastPos = result.LastIndexOf("/");
                if ((lastPos == (result.Length - 1)) && (lastPos > 0))
                    lastPos = result.LastIndexOf("/", lastPos - 1);
                if (lastPos > 0)
                {
                    result = result.Substring(lastPos + 1, result.Length - lastPos - 1 );
                }
            }
            return result;
        }
        static string GetPosterUri(string unescapeuri, string extensions, IEnumerable<IListBlobItem> list)
        {
            string result = "ms-appx:///Assets/MUSIC.png";
            string ext = GetExtension(unescapeuri);
            if (string.IsNullOrEmpty(ext))
            {
                if (IsMusicFile(ext))
                {
                    result = "ms-appx:///Assets/MUSIC.png";
                }
                else if (IsPictureFile(ext))
                {
                    result = "ms-appx:///Assets/PHOTO.png";
                }
                else if (IsVideoFile(ext))
                {
                    result = "ms-appx:///Assets/VIDEO.png";
                }
            }
            if ((!string.IsNullOrEmpty(ext)) && (extensions.IndexOf(ext, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                string uri = unescapeuri;                        
                int pos = uri.LastIndexOf('/');
                if (pos > 0)
                {
                    string prefix = uri.Substring(0, pos + 1);
                    foreach (var i in list)
                    {
                        if (i.GetType() == typeof(CloudBlockBlob))
                        {
                            CloudBlockBlob blob = (CloudBlockBlob)i;

                            string url = blob.Uri.ToString();
                            if(url.EndsWith(".jpg",StringComparison.OrdinalIgnoreCase) ||
                                url.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                            {
                                if(url.StartsWith(prefix))
                                {
                                    string s = url.Substring(pos + 1);
                                    if (s.IndexOf('/') < 0)
                                    {
                                        string excapeString = EncodeUri(url);
                                        if (!string.IsNullOrEmpty(excapeString))
                                        {
                                            excapeString = excapeString.Replace("\'", "%27");

                                            return excapeString;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
        static int AppendText(Stream stream, string Text)
        {
            try
            {
                if ((stream != null) && (!string.IsNullOrEmpty(Text)))
                {
                    stream.Seek(0, SeekOrigin.End);
                    byte[] b = System.Text.UTF8Encoding.UTF8.GetBytes(Text);
                    stream.Write(b, 0, b.Length);
                    return b.Length;
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while writing in stream:  " + ex.Message);
            }
            return 0;
        }
        public static async System.Threading.Tasks.Task<int> CreateCloudPlaylistOld(string PlaylistName, string AccountName, string AccountKey, string Container, string extensions, bool bCreateThumbnails, int SlideShowPeriod, string outputFile)
        {
            List<string> blobs = new List<string>();
            int counter = 0;
            try
            {
                Windows.Storage.StorageFile writer = await CreateFile(outputFile);
                if (writer != null)
                {
                    Stream stream = await writer.OpenStreamForWriteAsync();
                    if (stream != null)
                    {
                        // Retrieve storage account from connection string.
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                        "DefaultEndpointsProtocol=https;AccountName=" + AccountName + ";AccountKey=" + AccountKey);

                        // Create the blob client.
                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                        // Retrieve reference to a previously created container.
                        CloudBlobContainer container = blobClient.GetContainerReference(Container);


                        // Loop over items within the container and output the length and URI.
                        //var result = await container.ListBlobsSegmentedAsync(null);
                        BlobResultSegment result;
                        BlobContinuationToken token = null;
                        do
                        {
                            
                            result = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.None, 500, token, null, null);
                            if (result != null)
                            {
                                token = result.ContinuationToken;
                                foreach (IListBlobItem item in result.Results)
                                {
                                    if (item.GetType() == typeof(CloudBlockBlob))
                                    {
                                        CloudBlockBlob blob = (CloudBlockBlob)item;

                                        string unescapeuri = blob.Uri.ToString();
                                        string ext = GetExtension(unescapeuri);
                                        if ((!string.IsNullOrEmpty(ext)) && (extensions.IndexOf(ext, StringComparison.OrdinalIgnoreCase) >= 0))
                                        {
                                            string artist = string.Empty;
                                            string album = string.Empty;
                                            string posteruri = string.Empty;
                                            if (bCreateThumbnails)
                                                posteruri = GetPosterUri(unescapeuri, extensions, result.Results);
                                            //string title = GetUriFileName(unescapeuri);
                                            string title = GetUriFileName(blob.Name);
                                            if (!string.IsNullOrEmpty(title))
                                            {
                                                string uri = EncodeUri(unescapeuri);
                                                uri = uri.Replace("\'", "%27");

                                                if (counter == 0)
                                                {
                                                    if (writer != null)
                                                    {
                                                        string header = headerStart + PlaylistName + headerEnd;
                                                        string s = header;
                                                        s += "{";
                                                        if (IsPictureFile(ext))
                                                            s += string.Format(pictureItem, counter.ToString(), title, uri, posteruri, SlideShowPeriod.ToString());
                                                        else if (IsMusicFile(ext))
                                                            s += string.Format(musicItem, counter.ToString(), title, uri, posteruri);
                                                        else
                                                            s += string.Format(videoItem, counter.ToString(), title, uri, posteruri);
                                                        s += "}";

//                                                        await Windows.Storage.FileIO.AppendTextAsync(writer, s);
                                                        AppendText(stream, s);
                                                    }
                                                }
                                                else
                                                {
                                                    if (writer != null)
                                                    {

                                                        string s = ",\r\n";
                                                        s += "{";
                                                        if (IsPictureFile(ext))
                                                            s += string.Format(pictureItem, counter.ToString(), title, uri, posteruri, SlideShowPeriod.ToString());
                                                        else if (IsMusicFile(ext))
                                                            s += string.Format(musicItem, counter.ToString(), title, uri, posteruri);
                                                        else
                                                            s += string.Format(videoItem, counter.ToString(), title, uri, posteruri);
                                                        s += "}";
//                                                        await Windows.Storage.FileIO.AppendTextAsync(writer, s);
                                                        AppendText(stream, s);

                                                    }
                                                }
                                                counter++;
                                            }
                                        }
                                    }
                                    else if (item.GetType() == typeof(CloudPageBlob))
                                    {
                                        CloudPageBlob pageBlob = (CloudPageBlob)item;
                                    }
                                    else if (item.GetType() == typeof(CloudBlobDirectory))
                                    {
                                        CloudBlobDirectory directory = (CloudBlobDirectory)item;

                                    }
                                }
                            }
                        }
                        while (result.ContinuationToken != null);

                        //await Windows.Storage.FileIO.AppendTextAsync(writer, footer);
                        AppendText(stream, footer);
                        stream.Flush();

                        System.Diagnostics.Debug.WriteLine(counter.ToString() + " files discovered on cloud storage\n");
                        return counter;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while discovering media file on cloud storage:" + ex.Message);
            }
            return -1;
        }

        private static string EncodeUri(string input)
        {
            //string result0 = System.Net.WebUtility.HtmlEncode(input);

            input = input.Replace("%3F", "?");

            //string result1 = Uri.EscapeUriString(input);
            string result1 = Uri.EscapeDataString(input);
            result1 = result1.Replace("%3A", ":");
            result1 = result1.Replace("%2F", "/");
            //result2 = result2.Replace("%3F", "?");
//            excapeString = excapeString.Replace("\'", "%27");
            //     if (result1 != result2)
            //         return result1;
            return result1;
        }
        public static async System.Threading.Tasks.Task<int> ProcessCloudFolder(CloudBlobDirectory initialDirectory, bool bFirst, string PlaylistName,CloudBlobContainer container, CloudBlobDirectory directory, Stream stream, string extensions, bool bCreateThumbnails, int SlideShowPeriod)
        {
            int counter = 0;
            BlobResultSegment result = null;
            BlobContinuationToken token = null;
            do
            {
                if (CloudPlaylistStopRequested == true)
                    break;
                if (directory != null)
                    result = await directory.ListBlobsSegmentedAsync(token);
                else
                    result = await container.ListBlobsSegmentedAsync(null, false, BlobListingDetails.None, 500, token, null, null);
                if (result != null)
                {
                    token = result.ContinuationToken;
                    foreach (IListBlobItem item in result.Results)
                    {
                        if (CloudPlaylistStopRequested == true)
                            break;

                        if (item.GetType() == typeof(CloudBlockBlob))
                        {
                            CloudBlockBlob blob = (CloudBlockBlob)item;

                            string unescapeuri = blob.Uri.ToString();
                            string ext = GetExtension(unescapeuri);
                            if ((!string.IsNullOrEmpty(ext)) && (extensions.IndexOf(ext, StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                string posteruri = string.Empty;
                                string category = GetCategoryFromUri(initialDirectory != null? initialDirectory.Uri.ToString(): container.Uri.ToString(), unescapeuri);
                                if (bCreateThumbnails)
                                    posteruri = GetPosterUri(unescapeuri, extensions, result.Results);
                                //string title = GetUriFileName(unescapeuri);
                                string title = GetUriFileName(blob.Name);
                                if (!string.IsNullOrEmpty(title))
                                {
                                    string uri = EncodeUri(unescapeuri);
                                    uri = uri.Replace("\'", "%27");

                                    if (bFirst == true)
                                    {
                                        bFirst = false;
                                        string s = "";
                                        s += "{";
                                        if (IsPictureFile(ext))
                                            s += string.Format(pictureItem, counter.ToString(), title, uri, posteruri, SlideShowPeriod.ToString(), category);
                                        else if (IsMusicFile(ext))
                                        {
                                            string artist = GetArtistFromUri(initialDirectory != null ? initialDirectory.Uri.ToString() : container.Uri.ToString(), unescapeuri);
                                            string album = GetAlbumFromUri(initialDirectory != null ? initialDirectory.Uri.ToString() : container.Uri.ToString(), unescapeuri);
                                            s += string.Format(musicItem, counter.ToString(), title, uri, posteruri, artist, album, category);
                                        }
                                        else
                                            s += string.Format(videoItem, counter.ToString(), title, uri, posteruri, category);
                                        s += "}";
                                        AppendText(stream, s);
                                    }
                                    else
                                    {
                                        string s = ",\r\n";
                                        s += "{";
                                        if (IsPictureFile(ext))
                                            s += string.Format(pictureItem, counter.ToString(), title, uri, posteruri, SlideShowPeriod.ToString(), category);
                                        else if (IsMusicFile(ext))
                                        {
                                            string artist = GetArtistFromUri(initialDirectory != null ? initialDirectory.Uri.ToString() : container.Uri.ToString(), unescapeuri);
                                            string album = GetAlbumFromUri(initialDirectory != null ? initialDirectory.Uri.ToString() : container.Uri.ToString(), unescapeuri);
                                            s += string.Format(musicItem, counter.ToString(), title, uri, posteruri, artist, album, category);
                                        }
                                        else
                                            s += string.Format(videoItem, counter.ToString(), title, uri, posteruri, category);
                                        s += "}";
                                        AppendText(stream, s);
                                    }
                                    counter++;
                                    CloudPlaylistCounter++;
                                }
                            }
                        }
                        else if (item.GetType() == typeof(CloudPageBlob))
                        {
                            CloudPageBlob pageBlob = (CloudPageBlob)item;
                        }
                        else if (item.GetType() == typeof(CloudBlobDirectory))
                        {
                            CloudBlobDirectory subdirectory = (CloudBlobDirectory)item;
                            bFirst = ((counter == 0) && (bFirst == true))? true : false;
                            int c = await ProcessCloudFolder(initialDirectory, bFirst, PlaylistName, container, subdirectory, stream, extensions, bCreateThumbnails, SlideShowPeriod);
                            counter += c;
                        }
                    }
                }
            }
            while (result.ContinuationToken != null);
            return counter;
        }
        private static int cloudPlaylistCounter;
        public static int CloudPlaylistCounter
        {
            set
            {
                if (cloudPlaylistCounter != value)
                {
                    cloudPlaylistCounter = value;
                    if (CloudItemDiscovered != null)
                        CloudItemDiscovered(CloudPlayListTask, value);
                }
            }
            get
            {
                return cloudPlaylistCounter;
            }
        }
        static System.Threading.Tasks.Task CloudPlayListTask = null;
        private static bool cloudPlaylistRunning = false;

        public static bool CloudPlaylistRunning
        {
            set
            {
                if (cloudPlaylistRunning != value)
                {
                    cloudPlaylistRunning = value;
                    if (CloudItemDiscovered != null)
                        CloudTaskStatus(CloudPlayListTask, value);
                }
            }
            get
            {
                return cloudPlaylistRunning;
            }
        }

        static bool CloudPlaylistStopRequested = false;
        //
        // Summary:
        //     Raised when a local item is discovered
        public static event TypedEventHandler<System.Threading.Tasks.Task, int> CloudItemDiscovered;
        //
        // Summary:
        //     Raised when the creation task status change 
        public static event TypedEventHandler<System.Threading.Tasks.Task, bool> CloudTaskStatus;

        public static bool IsCloudPlaylistTaskRunning()
        {
            if (CloudPlayListTask != null)
            {
                if (CloudPlaylistRunning == true)
                    return true;
            }
            return false;
        }
        public static async System.Threading.Tasks.Task<bool> StopCloudPlaylistTask()
        {
            if (IsCloudPlaylistTaskRunning())
            {
                CloudPlaylistStopRequested = true;
                while (IsCloudPlaylistTaskRunning())
                {
                    await System.Threading.Tasks.Task.Delay(1000);
                }
                CloudPlayListTask = null;
                return true;
            }
            CloudPlayListTask = null;
            return false;

        }
        public static async System.Threading.Tasks.Task<bool> StartCloudPlaylistTask(string PlaylistName, string AccountName, string AccountKey, string Container, string folder, string extensions, bool bCreateThumbnails, int SlideShowPeriod, string outputFile)
        {
            if (CloudPlayListTask != null)
            {
                await StopCloudPlaylistTask();
            }
            if (CloudPlayListTask == null)
            {
                CloudPlayListTask = new Task(async () =>
                {
                    try
                    {
                        List<string> blobs = new List<string>();
                        CloudPlaylistRunning = true;
                        CloudPlaylistStopRequested = false;
                        CloudPlaylistCounter = 0;
                        int counter = 0;
                        Windows.Storage.StorageFile writer = await CreateFile(outputFile);
                        if (writer != null)
                        {
                            Stream stream = await writer.OpenStreamForWriteAsync();
                            if (stream != null)
                            {
                                // Retrieve storage account from connection string.
                                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                                "DefaultEndpointsProtocol=https;AccountName=" + AccountName + ";AccountKey=" + AccountKey);

                                // Create the blob client.
                                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                                // Retrieve reference to a previously created container.
                                CloudBlobContainer container = blobClient.GetContainerReference(Container);

                                string header = headerStart + PlaylistName + headerEnd;
                                string s = header;
                                AppendText(stream, s);
                                CloudBlobDirectory directory = (!string.IsNullOrEmpty(folder) ? container.GetDirectoryReference(folder) : null);
                                bool bFirst = true;
                                int c = await ProcessCloudFolder(directory, bFirst, PlaylistName, container, directory, stream, extensions, bCreateThumbnails, SlideShowPeriod);
                                counter += c;
                                AppendText(stream, footer);
                                stream.Flush();

                                System.Diagnostics.Debug.WriteLine(counter.ToString() + " files discovered on cloud storage\n");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception while discovering media file on local hard drive:" + ex.Message);
                    }
                    finally
                    {
                        CloudPlaylistRunning = false;
                        CloudPlaylistStopRequested = false;
                    }

                });
                CloudPlayListTask.Start();
                return true;
            }
            return false;
        }
        public static async System.Threading.Tasks.Task<int> CreateCloudPlaylist(string PlaylistName, string folderName, string extensions, bool bCreateThumbnails, int SlideShowPeriod, string outputFile)
        {
            try
            {
                int counter = 0;
                Windows.Storage.StorageFile File = await CreateFile(outputFile);
                if (File != null)
                {
                    Stream stream = await File.OpenStreamForWriteAsync();
                    if (stream != null)
                    {
                        if (await IsFile(folderName) == true)
                        {
                            counter = await ProcessFile(folderName, counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, stream, folderName);
                        }
                        else if (await IsFolder(folderName))
                        {
                            // This path is a directory
                            counter = await ProcessDirectory(folderName, counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, stream, folderName);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("{0} is not a valid file or directory.", folderName);
                            return -1;
                        }
                        //await Windows.Storage.FileIO.AppendTextAsync(File, footer);
                        AppendText(stream, footer);
                        stream.Flush();
                        System.Diagnostics.Debug.WriteLine(counter.ToString() + " files discovered on local storage\n");
                        return counter;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while discovering media file on local hard drive:" + ex.Message);
            }
            return -1;
        }

        public static async System.Threading.Tasks.Task<int> CreateCloudPlaylist(string PlaylistName, string AccountName, string AccountKey, string Container, string folder, string extensions, bool bCreateThumbnails, int SlideShowPeriod, string outputFile)
        {
            List<string> blobs = new List<string>();
            int counter = 0;
            try
            {
                Windows.Storage.StorageFile writer = await CreateFile(outputFile);
                if (writer != null)
                {
                    Stream stream = await writer.OpenStreamForWriteAsync();
                    if (stream != null)
                    {
                        // Retrieve storage account from connection string.
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                        "DefaultEndpointsProtocol=https;AccountName=" + AccountName + ";AccountKey=" + AccountKey);

                        // Create the blob client.
                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                        // Retrieve reference to a previously created container.
                        CloudBlobContainer container = blobClient.GetContainerReference(Container);

                        string header = headerStart + PlaylistName + headerEnd;
                        string s = header;
                        AppendText(stream, s);
                        CloudBlobDirectory directory = (!string.IsNullOrEmpty(folder) ? container.GetDirectoryReference(folder):null);
                        bool bFirst = true;
                        int c = await ProcessCloudFolder(directory,bFirst, PlaylistName, container, directory, stream, extensions, bCreateThumbnails, SlideShowPeriod);
                        counter += c;
                        AppendText(stream, footer);
                        stream.Flush();

                        System.Diagnostics.Debug.WriteLine(counter.ToString() + " files discovered on cloud storage\n");
                        return counter;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while discovering media file on cloud storage:" + ex.Message);
            }
            return -1;
        }
        static string[] conversionTable = {
            "&", "and",
            "é", "e",
            "è", "e",
            "ê", "e",
            "ë","e",
            "à", "a",
            "ç", "c",
            "Á", "A",
            "É", "E",
            "Í", "I",
            "Ó", "O",
            "Ú", "U",
            "Ü", "U",
            "á", "a",
            "í", "i",
            "î", "i",
            "ï", "i",
            "ó", "o",

            "ö", "o",
            "ø", "o",
            "õ", "o",

            "ú", "u",
            "û", "u",
            "ù", "u",
            "ü", "u",
            "Ñ", "N",
            "ñ", "n",
            "ß", "ss",
            "¿","?",
            "À","A",
            "â","a",
            "Â","A",
            "Ç","C",
            "ò","o",
            "œ","oe",
            "ä","a",
            "ž","z",
            "ô","o"
        };
        static string GetNewChar(char c)
        {
            string result = new string(c,1);
            int i = 0;
            while (i < conversionTable.Count()/2)
            {
                if(conversionTable[i*2].IndexOf(c)>=0)
                {
                    return conversionTable[i * 2 + 1];
                }
                i++;
            }
            return result;
        }
        static bool IsNameChangeRequired(string name)
        {
            string newName = string.Empty;
            foreach (char c in name)
            {
                string s = new string(c, 1);
                byte[] tab = Encoding.UTF8.GetBytes(s);
                if ((tab != null) && (tab.Count() > 0))
                {
                    if ((tab[0] != 0x20) &&
                        (tab[0] != 0x28) &&
                        (tab[0] != 0x29) &&
                        (tab[0] != 0x2D) && (tab[0] != 0x2C) &&
                        (tab[0] != 0x5B) && (tab[0] != 0x5D) &&
                        (tab[0] != 0x27) && (tab[0] != 0x21) &&
                        (tab[0] != 0x2E) &&
                        (tab[0] != 0x3A) &&
                        (tab[0] != 0x3B) &&
                        (tab[0] != 0x2F) &&
                        ((tab[0] < 0x30) ||
                        ((tab[0] > 0x39) && (tab[0] < 0x41)) ||
                        (tab[0] > 0x7A)))
                    {
                        return true;
                    }

                }
            }
            return false;
        }
        static string  GetNewName(string name)
        {
            string newName = string.Empty;
            if (IsNameChangeRequired(name))
            {
                foreach (char c in name)
                {
                    newName += GetNewChar(c);
                }
            }
            return newName;
        }
        public static async System.Threading.Tasks.Task<int> RenameCloudFolder(bool bFirst, string PlaylistName, CloudBlobContainer container, CloudBlobDirectory directory, Stream stream)
        {
            int counter = 0;
            BlobResultSegment result = null;
            BlobContinuationToken token = null;
            do
            {
                if (directory != null)
                    result = await directory.ListBlobsSegmentedAsync(token);
                else
                    result = await container.ListBlobsSegmentedAsync(null, false, BlobListingDetails.None, 500, token, null, null);
                if (result != null)
                {
                    token = result.ContinuationToken;
                    foreach (IListBlobItem item in result.Results)
                    {
                        if (item.GetType() == typeof(CloudBlockBlob))
                        {
                            CloudBlockBlob blob = (CloudBlockBlob)item;
                            if (blob != null)
                            {
                                string newName = GetNewName(blob.Name);
                                if (!string.IsNullOrEmpty(newName))
                                {
                                    AppendText(stream, blob.Name + " -> " + newName + "\r\n");
                                    
                                    CloudBlockBlob newblob = container.GetBlockBlobReference(newName);

                                    if (!await newblob.ExistsAsync())
                                    {
                                        await newblob.StartCopyAsync(blob);
                                        await blob.DeleteIfExistsAsync();
                                    }
                                    
                                }
                            }
                        }
                        else if (item.GetType() == typeof(CloudBlobDirectory))
                        {
                            CloudBlobDirectory subdirectory = (CloudBlobDirectory)item;
                            bFirst = ((counter == 0) && (bFirst == true)) ? true : false;
                            int c = await RenameCloudFolder(bFirst, PlaylistName, container, subdirectory, stream);
                            counter += c;
                        }
                    }
                }
            }
            while (result.ContinuationToken != null);
            return counter;
        }

        public static async System.Threading.Tasks.Task<int> RenameCloudPlaylist(string PlaylistName, string AccountName, string AccountKey, string Container, string folder, string outputFile)
        {
            List<string> blobs = new List<string>();
            try
            {
                Windows.Storage.StorageFile writer = await CreateFile(outputFile);
                if (writer != null)
                {
                    Stream stream = await writer.OpenStreamForWriteAsync();
                    if (stream != null)
                    {
                        // Retrieve storage account from connection string.
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                        "DefaultEndpointsProtocol=https;AccountName=" + AccountName + ";AccountKey=" + AccountKey);

                        // Create the blob client.
                        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

                        // Retrieve reference to a previously created container.
                        CloudBlobContainer container = blobClient.GetContainerReference(Container);

                        CloudBlobDirectory directory = (!string.IsNullOrEmpty(folder) ? container.GetDirectoryReference(folder) : null);
                        bool bFirst = true;
                        int c = await RenameCloudFolder(bFirst, PlaylistName, container, directory, stream);
                        stream.Flush();
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while discovering media file on cloud storage:" + ex.Message);
            }
            return -1;
        }

    }
}
