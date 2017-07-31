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
        /// Save Text into File
        /// </summary>
        public static bool SaveStringIntoFile(string fileName, string content)
        {
            bool result = false;
            try
            {
                // Create sample file; replace if exists.
                Windows.Storage.StorageFolder storageFolder =
                    Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile SaveFile = Task.Run(async () => {
                    return await storageFolder.CreateFileAsync(fileName + ".xml",
                        Windows.Storage.CreationCollisionOption.ReplaceExisting);
                }).Result;

                Task.Run(async () => {
                    await Windows.Storage.FileIO.WriteTextAsync(SaveFile, content); }).Wait();
                result = true;
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while saving into file: " + ex.Message);
            }
            return result;
        }
        /// <summary>
        /// Restore Text from File
        /// </summary>
        public static string RestoreStringFromFile(string fileName)
        {
            string result = string.Empty;
            try
            {
                // Create sample file; replace if exists.
                Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                Windows.Storage.StorageFile RestoreFile = Task.Run( async () => {
                    return await storageFolder.CreateFileAsync(fileName + ".xml",
                        Windows.Storage.CreationCollisionOption.OpenIfExists);
                }).Result;
                if (RestoreFile != null)
                {
                    result = Task.Run(async () => {
                        return await Windows.Storage.FileIO.ReadTextAsync(RestoreFile);
                        }).Result;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while restoring from file: " + ex.Message);
            }
            return result;
        }

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
