using Microsoft.Maui.Media;
using FieldNotesApp.Services;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace FieldNotesApp
{
    public partial class MainPage : ContentPage
    {
        private readonly DatabaseService _database;
        private bool _isRecording = false;
        private bool _isCameraMode = false;
        private CameraMode _currentCameraMode = CameraMode.Photo;

        public MainPage(DatabaseService database)
        {
            InitializeComponent();
            _database = database;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadEntries();
        }

        private async void LoadEntries()
        {
            try
            {
                var entries = await _database.GetAllEntriesAsync();

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
                var photoEntry = await _database.GetEntryAsync(selectedEntry.Id);

                if (photoEntry != null)
                {
                    var mediaEntry = await _database.GetMediaAsync(photoEntry.MediaId);
                    await Navigation.PushAsync(new PhotoDetailPage(_database, mediaEntry.Data, photoEntry.IsVideo, photoEntry.Id));
                }

                EntriesCollectionView.SelectedItem = null;
            }
        }

        private async void OnOpenCameraClicked(object sender, EventArgs e)
        {
            try
            {
                // Request camera permission
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

                // Switch to camera mode
                _isCameraMode = true;
                _currentCameraMode = CameraMode.Photo;
                UpdateUIForCameraMode();

                // Start camera preview
                await CameraWindow.StartCameraPreview(CancellationToken.None);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to open camera: {ex.Message}", "OK");
            }
        }

        private async void OnTakePhotoClicked(object sender, EventArgs e)
        {
            try
            {
                TakePhotoButton.IsEnabled = false;

                // Capture image
                var stream = await CameraWindow.CaptureImage(CancellationToken.None);

                if (stream != null)
                {
                    byte[] photoBytes;
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        photoBytes = memoryStream.ToArray();
                    }

                    // Close camera and return to list view
                    CloseCameraView();

                    // Navigate to detail page
                    await Navigation.PushAsync(new PhotoDetailPage(_database, photoBytes, isVideo: false));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to capture photo: {ex.Message}", "OK");
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
                if (!_isRecording)
                {
                    // Check Permissions
                    var micStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                    if (micStatus != PermissionStatus.Granted)
                    {
                        micStatus = await Permissions.RequestAsync<Permissions.Microphone>();
                    }

                    if (micStatus != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Permission Denied", "Microphone permission is required for video recording", "OK");
                        return;
                    }

                    // Start recording
                    Toast.Make("Recording Started.").Show();
                    RecordVideoButton.IsEnabled = false;
                    await CameraWindow.StartVideoRecording(CancellationToken.None);
                    _isRecording = true;
                    RecordVideoButton.Text = FieldNotesApp.Icons.FontAwesomeIcons.Stop; //"Stop Recording";
                    RecordVideoButton.IsEnabled = true;
                }
                else
                {
                    // Stop recording
                    RecordVideoButton.IsEnabled = false;
                    var videoStream = await CameraWindow.StopVideoRecording(CancellationToken.None);
                    _isRecording = false;

                    if (videoStream != null)
                    {
                        byte[] videoBytes;

                        // Reset stream position to the beginning
                        if (videoStream.CanSeek)
                        {
                            videoStream.Position = 0;
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            await videoStream.CopyToAsync(memoryStream);
                            videoBytes = memoryStream.ToArray();
                        }

                        // Dispose the video stream
                        videoStream.Dispose();

                        // Check if we actually got data
                        if (videoBytes == null || videoBytes.Length == 0)
                        {
                            await DisplayAlert("Error", "No video data captured", "OK");
                            RecordVideoButton.Text = FieldNotesApp.Icons.FontAwesomeIcons.Video; //"Record Video";
                            RecordVideoButton.IsEnabled = true;
                            return;
                        }

                        // Close camera and return to list view
                        CloseCameraView();

                        Toast.Make("Video recorded successfully").Show();

                        // Navigate to detail page
                        await Navigation.PushAsync(new PhotoDetailPage(_database, videoBytes, isVideo: true));
                    }

                    RecordVideoButton.Text = "Record Video";
                    RecordVideoButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to record video: {ex.Message}", "OK");
                _isRecording = false;
                RecordVideoButton.Text = "Record Video";
                RecordVideoButton.IsEnabled = true;
            }
        }

        private void OnCloseCameraClicked(object sender, EventArgs e)
        {
            CloseCameraView();
        }

        private void UpdateUIForCameraMode()
        {
            // Hide list view elements
            EntryScrollView.IsVisible = false;
            TopMenu.IsVisible = false;
            BottomMenu.IsVisible = false; // Hide the default bottom menu

            // Show camera elements
            CameraContainer.IsVisible = true;
            BottomCameraMenu.IsVisible = true;

            // Hide the open camera button, show capture/record buttons
            OpenCameraButton.IsVisible = false;
            TakePhotoButton.IsVisible = true;
            RecordVideoButton.IsVisible = true;
            CloseCameraButton.IsVisible = true;
        }

        private void CloseCameraView()
        {
            try
            {
                // Stop camera preview
                CameraWindow.StopCameraPreview();

                // Reset recording state
                if (_isRecording)
                {
                    _isRecording = false;
                    RecordVideoButton.Text = "Record Video";
                }

                _isCameraMode = false;

                // Show list view elements
                EntryScrollView.IsVisible = true;
                TopMenu.IsVisible = true;
                BottomMenu.IsVisible = true; // Show the default bottom menu

                // Hide camera elements
                CameraContainer.IsVisible = false;
                BottomCameraMenu.IsVisible = false;

                // Show the open camera button, hide capture/record buttons
                OpenCameraButton.IsVisible = true;
                TakePhotoButton.IsVisible = false;
                RecordVideoButton.IsVisible = false;
                CloseCameraButton.IsVisible = false;

                // Refresh entries list
                LoadEntries();
            }
            catch (Exception ex)
            {
                DisplayAlertAsync("Error", $"Failed to close camera: {ex.Message}", "OK");
            }
        }

        private void CameraCaptured(object sender, MediaCapturedEventArgs e)
        {
            // This event fires when media is captured
            // We're handling capture in the button click handlers instead
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

        private enum CameraMode
        {
            Photo,
            Video
        }
    }
}