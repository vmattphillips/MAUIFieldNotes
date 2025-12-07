using Microsoft.Maui.Media;
using FieldNotesApp.Services;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Alerts;

namespace FieldNotesApp
{
    public partial class MainPage : ContentPage
    {
        private readonly DatabaseService _database;

        public MainPage(DatabaseService database)
        {
            InitializeComponent();
            _database = database;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadEntries();  // Refresh list when returning to page
        }

        private async void LoadEntries()
        {
            try
            {
                var entries = await _database.GetAllEntriesAsync();

                // Create display models with additional properties
                var displayEntries = entries.Select(e => new EntryDisplayModel
                {
                    Id = e.Id,
                    IsVideo = e.IsVideo,
                    CreatedAt = e.CreatedAt,
                    MediaTypeDisplay = $"{(e.IsVideo ? "Video" : "Photo")} - {e.EntryName}",
                }).OrderByDescending(e => e.CreatedAt).ToList();

                EntriesCollectionView.ItemsSource = displayEntries;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to load entries: {ex.Message}", "OK");
            }
        }

        private async void OnEntrySelected(object sender, SelectionChangedEventArgs e)
        {
             if (e.CurrentSelection.FirstOrDefault() is EntryDisplayModel selectedEntry)
            {
                // Load full entry from database and navigate to detail page
                var photoEntry = await _database.GetEntryAsync(selectedEntry.Id);

                if (photoEntry != null)
                {

                    var mediaEntry = await _database.GetMediaAsync(photoEntry.MediaId);
                    await Navigation.PushAsync(new PhotoDetailPage(_database, mediaEntry.Data, photoEntry.IsVideo, photoEntry.Id));
                }

                // Deselect the item
                EntriesCollectionView.SelectedItem = null;
            }
        }

        private async void OnTakePhotoClicked(object sender, EventArgs e)
        {
            try
            {
                TakePhotoButton.IsEnabled = false;

                var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                }

                if (cameraStatus != PermissionStatus.Granted)
                {
                    await DisplayAlertAsync("Permission Denied", "Camera permission is required", "OK");
                    return;
                }

                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await DisplayAlertAsync("Not Supported", "Camera is not available", "OK");
                    return;
                }

                var photo = await MediaPicker.Default.CapturePhotoAsync();

                if (photo != null)
                {
                    byte[] photoBytes;
                    using (var stream = await photo.OpenReadAsync())
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        photoBytes = memoryStream.ToArray();
                    }
#if ANDROID
                    AndroidMediaSaver.SavePhotoToDcimAsync(photo);
                    Toast.Make("A copy has been saved to your camera folder").Show();
#endif

                    await Navigation.PushAsync(new PhotoDetailPage(_database, photoBytes, isVideo: false));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed: {ex.Message}", "OK");
            }
            finally
            {
                TakePhotoButton.IsEnabled = true;
            }
        }

        private async void OnRecordVideoClicked(object sender, EventArgs e)
        {
            try
            {
                var video = await MediaPicker.CaptureVideoAsync();

                if (video != null)
                {
                    byte[] videoBytes;
                    using (var stream = await video.OpenReadAsync())
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        videoBytes = memoryStream.ToArray();
                    }
                    Toast.Make("A copy has been saved to your camera folder").Show();

                    await Navigation.PushAsync(new PhotoDetailPage(_database, videoBytes, isVideo: true));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed: {ex.Message}", "OK");
            }
        }
        // Helper class for display
        public class EntryDisplayModel
        {
            public int Id { get; set; }
            public string EntryName { get; set; }
            public bool IsVideo { get; set; }
            public DateTime CreatedAt { get; set; }
            public string MediaTypeDisplay { get; set; }
            public string MediaIcon { get; set; }
        }
    }
}