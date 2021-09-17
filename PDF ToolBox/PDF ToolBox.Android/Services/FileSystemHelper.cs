using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;

using PDF_ToolBox.Droid;
using PDF_ToolBox.Droid.Services;
using PDF_ToolBox.Services;

[assembly: Xamarin.Forms.Dependency(typeof(FileSystemHelper))]
namespace PDF_ToolBox.Droid.Services
{
    public class FileSystemHelper : IFileSystemHelper
    {
        public string GetAppExternalStorage()
        {
            var context = Android.App.Application.Context; 
            var filePath = context.GetExternalFilesDir("");
            return filePath.Path;
        }
        public string GetDeviceId()
        {
            return Android.Provider.Settings.Secure.GetString(Android.App.Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
        }
    }
}