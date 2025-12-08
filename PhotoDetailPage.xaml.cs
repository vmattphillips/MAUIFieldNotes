using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;
using FieldNotesApp.Services;
using FieldNotesApp.Models;

namespace FieldNotesApp
{
    public partial class PhotoDetailPage : ContentPage
    {
        private readonly DatabaseService _database;
        private byte[] _mediaBytes;
        private double? _latitude;
        private double? _longitude;
        private byte[] _voiceRecordingBytes;
        private bool _isVideo;
        private string _tempVideoPath;
        private int _entryId;
        private int _mediaId; // Store existing media ID
        private int? _voiceRecordingId; // Store existing voice recording ID
        private bool _isEditMode; // Track if we're editing an existing entry
        private bool _mediaChanged; // Track if media was replaced

        public PhotoDetailPage(DatabaseService database, byte[] mediaBytes, bool isVideo = false, int entryId = 0)
        {
            InitializeComponent();
            _database = database;
            _mediaBytes = mediaBytes;
            _isVideo = isVideo;
            _entryId = entryId;
            _isEditMode = entryId > 0;
            _mediaChanged = false;

            if (isVideo)
            {
                VideoPlayer.IsVisible = true;
                _tempVideoPath = Path.Combine(FileSystem.CacheDirectory, $"temp_video_{Guid.NewGuid()}.mp4");
                File.WriteAllBytes(_tempVideoPath, mediaBytes);
                VideoPlayer.Source = MediaSource.FromFile(_tempVideoPath);
                VideoPlayer.ShouldAutoPlay = false;
            }
            else
            {
                PhotoImage.IsVisible = true;
                PhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(mediaBytes));
            }

            // If editing existing entry, load its data
            if (_isEditMode)
            {
                LoadExistingEntry(_entryId);
            }
            else
            {
                EntryNameField.Text = DateTime.Now.Date.ToString() + "_" + (isVideo ? "video" : "image");
            }
        }

        private async void LoadExistingEntry(int entryId)
        {
            try
            {
                var entry = await _database.GetEntryAsync(entryId);
                if (entry != null)
                {
                    // Store existing IDs to avoid re-saving
                    _mediaId = entry.MediaId;
                    _voiceRecordingId = entry.VoiceRecordingId;

                    // Load entry data
                    EntryNameField.Text = entry.EntryName;
                    _latitude = entry.Latitude;
                    _longitude = entry.Longitude;
                    NotesEditor.Text = entry.Notes;

                    if (entry.Latitude.HasValue && entry.Longitude.HasValue)
                    {
                        LocationLabel.Text = $"Lat: {entry.Latitude:F6}, Lon: {entry.Longitude:F6}";
                    }

                    // Load voice recording if exists
                    if (entry.VoiceRecordingId.HasValue)
                    {
                        var voiceRecording = await _database.GetVoiceRecordingAsync(entry.VoiceRecordingId.Value);
                        if (voiceRecording != null)
                        {
                            _voiceRecordingBytes = voiceRecording.Data;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load entry: {ex.Message}", "OK");
            }
        }

        private async void OnGeotagClicked(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status == PermissionStatus.Granted)
                {
                    var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(10)
                    });

                    if (location != null)
                    {
                        _latitude = location.Latitude;
                        _longitude = location.Longitude;
                        LocationLabel.Text = $"Lat: {_latitude:F6}, Lon: {_longitude:F6}";
                    }
                }
                else
                {
                    await DisplayAlert("Permission Denied", "Location permission is required", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to get location: {ex.Message}", "OK");
            }
        }

        private async void OnRecordClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Coming Soon", "Voice recording will be implemented next", "OK");
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                int finalMediaId;
                int? finalVoiceRecordingId = _voiceRecordingId;

                // Only save media if it's new or changed
                if (_isEditMode && !_mediaChanged)
                {
                    // Editing existing entry and media hasn't changed - reuse existing media ID
                    finalMediaId = _mediaId;
                }
                else
                {
                    // New entry or media was replaced - save new media
                    finalMediaId = await _database.SaveMediaAsync(_mediaBytes, _isVideo);

                    // If we're editing and media changed, optionally delete old media
                    // (Uncomment if you want to clean up old media files)
                    // if (_isEditMode && _mediaChanged && _mediaId > 0)
                    // {
                    //     await _database.DeleteMediaAsync(_mediaId);
                    // }
                }

                // Save voice recording only if it's new or changed
                if (_voiceRecordingBytes != null)
                {
                    // Check if voice recording is new (not loaded from existing entry)
                    if (!_isEditMode || _voiceRecordingId == null)
                    {
                        // New voice recording - save it
                        finalVoiceRecordingId = await _database.SaveVoiceRecordingAsync(_voiceRecordingBytes, 0);
                    }
                    // If editing and voice recording exists, keep the existing ID
                    // Voice recording data is already in _voiceRecordingBytes from LoadExistingEntry
                }

                // Create/Update the entry
                var entry = new PhotoEntry
                {
                    Id = _entryId, // Will be 0 for new entries, existing ID for updates
                    IsVideo = _isVideo,
                    EntryName = EntryNameField.Text,
                    MediaId = finalMediaId,
                    VoiceRecordingId = finalVoiceRecordingId,
                    Latitude = _latitude,
                    Longitude = _longitude,
                    Notes = NotesEditor.Text,
                    CreatedAt = _isEditMode ? (await _database.GetEntryAsync(_entryId)).CreatedAt : DateTime.Now
                };

                await _database.SavePhotoEntryAsync(entry);

                string message = _isEditMode ? "Entry updated successfully!" : "Entry saved to database!";
                await DisplayAlert("Success", message, "OK");

                // Clean up temp video file
                if (!string.IsNullOrEmpty(_tempVideoPath) && File.Exists(_tempVideoPath))
                {
                    File.Delete(_tempVideoPath);
                }

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save: {ex.Message}", "OK");
            }
        }

        // Optional: Add method to replace media (for future feature)
        private void OnReplaceMediaClicked(object sender, EventArgs e)
        {
            _mediaChanged = true;
            // Implement media replacement logic here if needed
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Clean up temp video file
            if (!string.IsNullOrEmpty(_tempVideoPath) && File.Exists(_tempVideoPath))
            {
                try
                {
                    File.Delete(_tempVideoPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}