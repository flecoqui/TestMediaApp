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

namespace AudioVideoPlayer.Helpers
{
    public static class MediaHelper
    {
        const string headerStart = "{ \"Groups\": [{\"UniqueId\": \"audio_video_picture\",\"Title\": \"";
        const string headerEnd = "\",\"Category\": \"Windows 10 Audio Video Tests\",\"ImagePath\": \"ms-appx:///Assets/AudioVideo.png\",\"Description\": \"Windows 10 Audio Video Tests\",\"Items\": [\r\n";
        const string audioItem = " \"UniqueId\": \"{0}\", \"Comment\": \"\", \"Title\": \"{1}\", \"ImagePath\": \"ms-appx:///Assets/MP4.png\",\"Description\": \"\", \"Content\": \"{2}\", \"PosterContent\": \"{3}\",\"Start\": \"0\",\"Duration\": \"0\",\"PlayReadyUrl\": \"null\",\"PlayReadyCustomData\": \"null\",\"BackgroundAudio\": true";
        const string pictureItem = " \"UniqueId\": \"{0}\", \"Comment\": \"\", \"Title\": \"{1}\", \"ImagePath\": \"ms-appx:///Assets/PHOTO.png\",\"Description\": \"\", \"Content\": \"{2}\", \"PosterContent\": \"{3}\",\"Start\": \"0\",\"Duration\": \"10000\",\"PlayReadyUrl\": \"null\",\"PlayReadyCustomData\": \"null\",\"BackgroundAudio\": true";
        const string footer = "\r\n]}]}";
        public const string videoExts = ".asf;.avi;.ismv;.ts;.m4a;.mkv;.mov;.mp4;";
        public const string audioExts = ".mp3;.aac;.wma;.wmv;wav;.flac;";
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
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while reading file " + filePath + " : " + Ex.Message);
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
                        Windows.Storage.StorageFolder folder = Windows.Storage.KnownFolders.DocumentsLibrary;
                        if (!string.IsNullOrEmpty(folderPath))
                            folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(folderPath);
                        if (folder != null)
                        {
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
                        while ((f = await StorageHelper.GetFile(Windows.Storage.KnownFolders.DocumentsLibrary, name)) != null)
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
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while reading folder " + folderPath + " : " + Ex.Message);
            }
            return false;

        }
        // Process all files in the directory passed in, recurse on any directories 
        // that are found, and process the files they contain.
        public static async System.Threading.Tasks.Task<ulong> ProcessDirectory(ulong counter, string PlaylistName, string extensions, Windows.Storage.StorageFile writer, string targetDirectory)
        {

            // Process the list of files found in the directory.
            string[] fileEntries = System.IO.Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                counter = await ProcessFile(counter, PlaylistName, extensions, writer, fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                counter = await ProcessDirectory(counter, PlaylistName, extensions, writer, subdirectory);
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
        public static async System.Threading.Tasks.Task<string> SaveThumbnailToFileAsync(StorageFolder pictureFolder, string imageName, StorageItemThumbnail image)
        {
            try
            {
                if (pictureFolder != null)
                {
                    var file = await pictureFolder.CreateFileAsync(imageName + ".jpg", CreationCollisionOption.ReplaceExisting);
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
                    Windows.Storage.StorageFolder dfolder = Windows.Storage.KnownFolders.DocumentsLibrary;
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
                Windows.Storage.StorageFolder dfolder = Windows.Storage.KnownFolders.DocumentsLibrary;
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
        public static async System.Threading.Tasks.Task<ulong> ProcessFile(ulong counter, string PlaylistName, string extensions, Windows.Storage.StorageFile writer, string path)
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
                            Windows.Storage.FileProperties.MusicProperties m = await File.Properties.GetMusicPropertiesAsync();
                            if (m != null)
                            {
                                artist = m.Artist;
                                album = m.Album;
                                posteruri = path;
                                title = m.Title;
                                if (string.IsNullOrEmpty(title))
                                    title = System.IO.Path.GetFileNameWithoutExtension(path);
                                using (var thumbnail = await File.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.MusicView, 300))
                                {
                                    if (thumbnail != null && thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Image)
                                    {
                                        StorageFolder f = await GetThumbnailFolder(PlaylistName);
                                        string name = System.IO.Path.GetFileNameWithoutExtension(path);
                                        if ((f != null) && (!string.IsNullOrEmpty(name)))
                                        {
                                            string s = await SaveThumbnailToFileAsync(f, "tb_" + name, thumbnail);
                                            if (string.IsNullOrEmpty(s))
                                            {
                                                posteruri = s;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //Error Message here
                                    }
                                }
                            }

                        }
                        else if (IsPictureFile(ext))
                        {
                            Windows.Storage.FileProperties.ImageProperties p = await File.Properties.GetImagePropertiesAsync();
                            posteruri = path;
                            title = p.Title;
                            if (string.IsNullOrEmpty(title))
                                title = System.IO.Path.GetFileNameWithoutExtension(path);

                        }
                        else if (IsVideoFile(ext))
                        {
                            Windows.Storage.FileProperties.VideoProperties v = await File.Properties.GetVideoPropertiesAsync();
                            posteruri = path;
                            title = v.Title;
                            if (string.IsNullOrEmpty(title))
                                title = System.IO.Path.GetFileNameWithoutExtension(path);
                            using (var thumbnail = await File.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.VideosView, 300))
                            {
                                if (thumbnail != null && thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Image)
                                {
                                    StorageFolder f = await GetThumbnailFolder(PlaylistName);
                                    string name = System.IO.Path.GetFileNameWithoutExtension(path);
                                    if ((f != null) && (!string.IsNullOrEmpty(name)))
                                    {
                                        string s = await SaveThumbnailToFileAsync(f, "tb_" + name, thumbnail);
                                        if (string.IsNullOrEmpty(s))
                                        {
                                            posteruri = s;
                                        }
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
                                        s += string.Format(pictureItem, counter.ToString(), title, uri, posteruri);
                                    // await Windows.Storage.FileIO.AppendTextAsync(writer, string.Format(pictureItem, counter.ToString(), title, uri, posteruri));
                                    else
                                        s += string.Format(audioItem, counter.ToString(), title, uri, posteruri);
                                    //  await Windows.Storage.FileIO.AppendTextAsync(writer, string.Format(audioItem, counter.ToString(), title, uri, posteruri));
                                    // await Windows.Storage.FileIO.AppendTextAsync(writer, "}");
                                    s += "}";
                                    await Windows.Storage.FileIO.AppendTextAsync(writer, s);
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
                                        s += string.Format(pictureItem, counter.ToString(), title, uri, posteruri);
                                    //  await Windows.Storage.FileIO.AppendTextAsync(writer, string.Format(pictureItem, counter.ToString(), title, uri, posteruri));
                                    else
                                        s += string.Format(audioItem, counter.ToString(), title, uri, posteruri);
                                    //  await Windows.Storage.FileIO.AppendTextAsync(writer, string.Format(audioItem, counter.ToString(), title, uri, posteruri));
                                    //await Windows.Storage.FileIO.AppendTextAsync(writer, "}");
                                    s += "}";
                                    await Windows.Storage.FileIO.AppendTextAsync(writer, s);
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

        public static async System.Threading.Tasks.Task<bool> CreateLocalPlaylist(string PlaylistName, string folderName, string extensions, string outputFile )
        {
            try
            {
                ulong counter = 0;
                Windows.Storage.StorageFile File = await CreateFile(outputFile);
                if (File != null)
                {
                        if (await IsFile(folderName) == true)
                        {
                        counter = await ProcessFile(counter, PlaylistName, extensions, File, folderName);
                        }
                        else if (await IsFolder(folderName))
                        {
                            // This path is a directory
                            counter = await ProcessDirectory( counter, PlaylistName, extensions, File, folderName);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("{0} is not a valid file or directory.", folderName);
                        return false;
                        }
                        await Windows.Storage.FileIO.AppendTextAsync(File, footer);
                        System.Diagnostics.Debug.WriteLine(counter.ToString() + " files discovered on local storage\n");
                        return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while discovering media file on local hard drive:" + ex.Message);
            }
            return false;
        }
    }
}
