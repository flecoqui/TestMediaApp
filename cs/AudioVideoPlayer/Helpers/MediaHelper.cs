using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioVideoPlayer.Helpers
{
    public static class MediaHelper
    {
        const string header = "{ \"Groups\": [{\"UniqueId\": \"audio_video_picture\",\"Title\": \"Windows 10 Audio Video Tests\",\"Category\": \"Windows 10 Audio Video Tests\",\"ImagePath\": \"ms-appx:///Assets/AudioVideo.png\",\"Description\": \"Windows 10 Audio Video Tests\",\"Items\": [";
        const string songItem = " \"UniqueId\": \"{0}\", \"Comment\": \"\", \"Title\": \"{1}\", \"ImagePath\": \"ms-appx:///Assets/MP4.png\",\"Description\": \"\", \"Content\": \"{2}\", \"PosterContent\": \"{3}\",\"Start\": \"0\",\"Duration\": \"0\",\"PlayReadyUrl\": \"null\",\"PlayReadyCustomData\": \"null\",\"BackgroundAudio\": true";
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
                System.Diagnostics.Debug.WriteLine("Exception while reading file " + filePath +" : " + Ex.Message);
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
                    if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(fileName))
                    {
                        Windows.Storage.StorageFolder folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(folderPath);
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
        public static async System.Threading.Tasks.Task<ulong> ProcessDirectory(string extensions, Windows.Storage.StorageFile writer, string targetDirectory)
        {
            ulong counter = 0; 
            // Process the list of files found in the directory.
            string[] fileEntries = System.IO.Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                counter += await ProcessFile(extensions, writer, fileName);

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = System.IO.Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                counter += await ProcessDirectory( extensions, writer, subdirectory);
            return counter;
        }
        static bool IsMusicFile(string ext)
        {
            if (audioExts.Contains(ext))
                return true;
            return false;
        }
        static bool IsPictureFile(string ext)
        {
            if (pictureExts.Contains(ext))
                return true;
            return false;
        }
        static bool IsVideoFile(string ext)
        {
            if (videoExts.Contains(ext))
                return true;
            return false;
        }
        public static async System.Threading.Tasks.Task<ulong> ProcessFile( string extensions, Windows.Storage.StorageFile writer, string path)
        {
            ulong counter = 0;
            string ext = System.IO.Path.GetExtension(path);
            if ((!string.IsNullOrEmpty(ext)) && (extensions.Contains(ext)))
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
                            using (var thumbnail = await File.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.MusicView, 300))
                            {
                                if (thumbnail != null && thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Image)
                                {
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

                    }
                    else if (IsVideoFile(ext))
                    {
                        Windows.Storage.FileProperties.VideoProperties v = await File.Properties.GetVideoPropertiesAsync();
                        posteruri = path;
                        title = v.Title;
                        using (var thumbnail = await File.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.VideosView, 300))
                        {
                            if (thumbnail != null && thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Image)
                            {
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
                                await Windows.Storage.FileIO.AppendTextAsync(writer, header);
                                await Windows.Storage.FileIO.AppendTextAsync(writer, "{");
                                uri = "file://" + uri.Replace("\\", "\\\\");
                                posteruri = "file://" + posteruri.Replace("\\", "\\\\");
                                await Windows.Storage.FileIO.AppendTextAsync(writer, string.Format(songItem, counter.ToString(), title, uri, posteruri));
                                await Windows.Storage.FileIO.AppendTextAsync(writer, "}");
                            }
                        }
                        else
                        {
                            if (writer != null)
                            {
                                await Windows.Storage.FileIO.AppendTextAsync(writer, ",\r\n");
                                await Windows.Storage.FileIO.AppendTextAsync(writer, "{");
                                uri = "file://" + uri.Replace("\\", "\\\\");
                                posteruri = "file://" + posteruri.Replace("\\", "\\\\");
                                await Windows.Storage.FileIO.AppendTextAsync(writer, string.Format(songItem, counter.ToString(), title, uri, posteruri));
                                await Windows.Storage.FileIO.AppendTextAsync(writer, "}");
                            }
                        }
                        counter++;
                    }
                }
            }
            return counter;
        }

        public static async System.Threading.Tasks.Task<bool> CreateLocalPlaylist(string folderName, string extensions, string outputFile )
        {
            try
            {
                ulong counter = 0;
                Windows.Storage.StorageFile File = await CreateFile(outputFile);
                if (File != null)
                {
                        if (await IsFile(folderName) == true)
                        {
                            counter += await ProcessFile( extensions, File, folderName);
                        }
                        else if (await IsFolder(folderName))
                        {
                            // This path is a directory
                            counter += await ProcessDirectory( extensions, File, folderName);
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
