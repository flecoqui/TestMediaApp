using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioVideoPlayer.Helpers
{
    public static  class StorageHelper
    {
        /// <summary>
        /// Create Folder
        /// </summary>
        public static async System.Threading.Tasks.Task<Windows.Storage.StorageFolder> CreateLocalFolder(string folderName)
        {
            try
            { 
            var f = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(folderName);
            return f;
            }
            catch(Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while creating folder: " + Ex.Message);
            }
            return null;
        }
        /// <summary>
        /// Get Folder
        /// </summary>
        public static async System.Threading.Tasks.Task<Windows.Storage.StorageFolder> GetFolder(string folderName)
        {
            try
            {
                var f = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(folderName);
                return f;
            }
            catch(Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while getting folder: " + Ex.Message);
            }
            return null;
        }
        /// <summary>
        /// Get File
        /// </summary>
        public static async System.Threading.Tasks.Task<Windows.Storage.StorageFile> GetFile(string folderName,string fileName)
        {
            try
            {
                var f = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(folderName);
                if (f != null)
                {
                    var file = await f.GetFileAsync(fileName);
                    return file;
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while getting folder: " + Ex.Message);
            }
            return null;
        }
        /// <summary>
        /// Remove File
        /// </summary>
        public static async System.Threading.Tasks.Task<bool> RemoveFile(string folderName, string fileName)
        {
            try
            {
                var f = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(folderName);
                if (f != null)
                {
                    var file = await f.GetFileAsync(fileName);
                    if(file!=null)
                        await file.DeleteAsync();
                    return true;
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while getting folder: " + Ex.Message);
            }
            return false;
        }
        /// <summary>
        /// Get Unique FileName
        /// </summary>
        public static async System.Threading.Tasks.Task<string> GetUniqueFileName(string folderName, string fileName)
        {
            try
            {
                string name = fileName;
                string ext = System.IO.Path.GetExtension(fileName);
                string root = System.IO.Path.GetFileNameWithoutExtension(fileName);
                int index = 0;
                Windows.Storage.StorageFile f = null;
                while((f = await StorageHelper.GetFile(folderName, name))!=null)
                {
                    name = root + "_" + index.ToString() + (string.IsNullOrEmpty(ext) ? "" :  ext);
                    index++;
                }
                return name;
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while getting folder: " + Ex.Message);
            }
            return null;
        }
        /// <summary>
        /// Copy a file in a folder
        /// </summary>
        public static async System.Threading.Tasks.Task<Windows.Storage.StorageFile> CopyFileToFolder(string path, string folderName)
        {
            try
            {
                Windows.Storage.StorageFolder folder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(folderName);
                if (folder != null)
                {

                    Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                    if (file != null)
                    {
                        return await file.CopyAsync(folder);
                    }
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while copying file: " + Ex.Message);
            }
            return null;
        }
        /// <summary>
        /// Copy a file in a folder with a new name
        /// </summary>
        public static async System.Threading.Tasks.Task<Windows.Storage.StorageFile> CopyFileToFolder(string path, string folderName, string desiredNewName)
        {
            try
            {
                Windows.Storage.StorageFolder folder = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFolderAsync(folderName);
                if (folder != null)
                {

                    Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                    if (file != null)
                    {
                        return await file.CopyAsync(folder, desiredNewName);
                    }
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while copying file: " + Ex.Message);
            }
            return null;
        }
        /// <summary>
        /// Remove  a file 
        /// </summary>
        public static async System.Threading.Tasks.Task<bool> RemoveFile(string path)
        {
            try
            {
                Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                if (file != null)
                {
                    await file.DeleteAsync();
                }
            }
            catch (Exception Ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while removing file: " + Ex.Message);
            }
            return false;
        }

    }
}
