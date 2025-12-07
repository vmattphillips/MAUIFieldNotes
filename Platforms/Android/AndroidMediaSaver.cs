#if ANDROID
using Android.Content;
using Android.Provider;
using Java.IO;
using Microsoft.Maui.ApplicationModel;
using Environment = Android.OS.Environment;

namespace FieldNotesApp.Services
{
    public static class AndroidMediaSaver
    {
        public static async Task SavePhotoToDcimAsync(FileResult photo)
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;

            var values = new ContentValues();
            values.Put(MediaStore.Images.Media.InterfaceConsts.DisplayName,
                $"IMG_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
            values.Put(MediaStore.Images.Media.InterfaceConsts.MimeType, "image/jpeg");
            values.Put(MediaStore.Images.Media.InterfaceConsts.RelativePath,
                $"{Environment.DirectoryDcim}/Camera");

            var uri = context.ContentResolver.Insert(
                MediaStore.Images.Media.ExternalContentUri,
                values
            );

            if (uri == null)
                throw new Exception("Failed to create MediaStore entry");

            using var input = await photo.OpenReadAsync();
            using var output = context.ContentResolver.OpenOutputStream(uri);

            if (output == null)
                throw new Exception("Failed to open output stream");

            await input.CopyToAsync(output);
        }
        public static async Task SavePhotoToDcimAsync(Stream photoStream)
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;

            var values = new ContentValues();
            values.Put(MediaStore.Images.Media.InterfaceConsts.DisplayName,
                $"IMG_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
            values.Put(MediaStore.Images.Media.InterfaceConsts.MimeType, "image/jpeg");
            values.Put(MediaStore.Images.Media.InterfaceConsts.RelativePath,
                $"{Environment.DirectoryDcim}/Camera");

            var uri = context.ContentResolver.Insert(
                MediaStore.Images.Media.ExternalContentUri,
                values
            );

            if (uri == null)
                throw new Exception("Failed to create MediaStore entry");

            using var output = context.ContentResolver.OpenOutputStream(uri);

            if (output == null)
                throw new Exception("Failed to open output stream");

            await photoStream.CopyToAsync(output);
        }
    }
}
#endif
