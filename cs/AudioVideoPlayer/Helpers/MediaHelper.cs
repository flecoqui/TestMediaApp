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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AudioVideoPlayer.Helpers
{
    public static class MediaHelper
    {
        const string headerStart = "{ \"Groups\": [{\"UniqueId\": \"audio_video_picture\",\"Title\": \"";
        const string headerEnd = "\",\"Category\": \"Windows 10 Audio Video Tests\",\"ImagePath\": \"ms-appx:///Assets/AudioVideo.png\",\"Description\": \"Windows 10 Audio Video Tests\",\"Items\": [\r\n";
        const string videoItem = " \"UniqueId\": \"{0}\", \"Comment\": \"\", \"Title\": \"{1}\", \"ImagePath\": \"ms-appx:///Assets/VIDEO.png\",\"Description\": \"\", \"Content\": \"{2}\", \"PosterContent\": \"{3}\",\"Start\": \"0\",\"Duration\": \"0\",\"PlayReadyUrl\": \"null\",\"PlayReadyCustomData\": \"null\",\"BackgroundAudio\": true";
        const string musicItem = " \"UniqueId\": \"{0}\", \"Comment\": \"\", \"Title\": \"{1}\", \"ImagePath\": \"ms-appx:///Assets/MUSIC.png\",\"Description\": \"\", \"Content\": \"{2}\", \"PosterContent\": \"{3}\",\"Start\": \"0\",\"Duration\": \"0\",\"PlayReadyUrl\": \"null\",\"PlayReadyCustomData\": \"null\",\"BackgroundAudio\": true";
        const string pictureItem = " \"UniqueId\": \"{0}\", \"Comment\": \"\", \"Title\": \"{1}\", \"ImagePath\": \"ms-appx:///Assets/PHOTO.png\",\"Description\": \"\", \"Content\": \"{2}\", \"PosterContent\": \"{3}\",\"Start\": \"0\",\"Duration\": \"{4}\",\"PlayReadyUrl\": \"null\",\"PlayReadyCustomData\": \"null\",\"BackgroundAudio\": true";
        const string footer = "\r\n]}]}";
        public const string videoExts = ".asf;.avi;.ismv;.ts;.m4a;.mkv;.mov;.mp4;.wmv;";
        public const string audioExts = ".mp3;.aac;.wma;wav;.flac;.m4a;";
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
        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        public static async System.Threading.Tasks.Task<int> ProcessDirectory(int counter, string PlaylistName, string extensions, bool bCreateThumbnails, int SlideShowPeriod, Stream writer, string targetDirectory)
        {

            // Process the list of files found in the directory.
            string[] fileEntries = System.IO.Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                counter = await ProcessFile(counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, writer, fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                counter = await ProcessDirectory(counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, writer, subdirectory);
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
        static string GetText(string Text)
        {
            return  Text.Replace('"', ' ').Replace('\\', ' ').Replace('/', ' ').Replace('*', ' ').Replace("'", " ");

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
        public static async System.Threading.Tasks.Task<int> ProcessFile(int counter, string PlaylistName, string extensions, bool bCreateThumbnails, int SlideShowPeriod, Stream writer, string path)
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
                        string posteruri = "";// GetPosterFile(path, extensions);
                        string title = "";//GetFileName(path);

                        if (IsMusicFile(ext))
                        {
                            posteruri = "ms-appx:///Assets/MUSIC.png";
                            Windows.Storage.FileProperties.MusicProperties m = await File.Properties.GetMusicPropertiesAsync();
                            if (m != null)
                            {
                                artist = GetText(m.Artist);
                                album = GetText(m.Album);
                                //title = GetText(m.Title);
                            }
                            if (string.IsNullOrEmpty(title))
                                title = System.IO.Path.GetFileNameWithoutExtension(path);
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
                                        s += string.Format(pictureItem, counter.ToString(), title, uri, posteruri, SlideShowPeriod.ToString());
                                    else if (IsMusicFile(ext))
                                        s += string.Format(musicItem, counter.ToString(), title, uri, posteruri);
                                    else 
                                        s += string.Format(videoItem, counter.ToString(), title, uri, posteruri);
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
                                        s += string.Format(pictureItem, counter.ToString(), title, uri, posteruri, SlideShowPeriod.ToString());
                                    else if (IsMusicFile(ext))
                                        s += string.Format(musicItem, counter.ToString(), title, uri, posteruri);
                                    else 
                                        s += string.Format(videoItem, counter.ToString(), title, uri, posteruri);
                                    //await Windows.Storage.FileIO.AppendTextAsync(writer, "}");
                                    s += "}";
                                    AppendText(writer, s);
//                                    await Windows.Storage.FileIO.AppendTextAsync(writer, s);
                                }
                            }
                            counter++;
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
                            counter = await ProcessFile(counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, stream, folderName);
                        }
                        else if (await IsFolder(folderName))
                        {
                            // This path is a directory
                            counter = await ProcessDirectory(counter, PlaylistName, extensions, bCreateThumbnails, SlideShowPeriod, stream, folderName);
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
                                        string excapeString = Uri.EscapeUriString(url);
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
        public static async System.Threading.Tasks.Task<int> CreateCloudPlaylist(string PlaylistName, string AccountName, string AccountKey, string Container, string extensions, bool bCreateThumbnails, int SlideShowPeriod, string outputFile)
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
                                            string title = GetUriFileName(unescapeuri);
                                            if (!string.IsNullOrEmpty(title))
                                            {
                                                string uri = Uri.EscapeUriString(unescapeuri);
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
    }
}
