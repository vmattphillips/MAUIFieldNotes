using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;
using FieldNotesApp.Services;
using FieldNotesApp.Models;

namespace FieldNotesApp
{
    public partial class PhotoDetailPage : ContentPage
    {
        private readonly DatabaseService _database;
        private double? _latitude;
        private double? _longitude;
        private byte[] _voiceRecordingBytes;
        private int _entryId;
        List<string> _filePaths;

        public PhotoDetailPage(DatabaseService database, List<string> filepaths, int entryId = 0)
        {
            InitializeComponent();
            _database = database;
            _entryId = entryId;  // Store the entry ID if editing
            this._filePaths = filepaths;
            //if (isVideo)
            //{
            //    VideoPlayer.IsVisible = true;
            //    _tempVideoPath = Path.Combine(FileSystem.CacheDirectory, $"temp_video_{Guid.NewGuid()}.mp4");
            //    File.WriteAllBytes(_tempVideoPath, mediaBytes);
            //    VideoPlayer.Source = MediaSource.FromFile(_tempVideoPath);
            //    VideoPlayer.ShouldAutoPlay = false;
            //}
            //else
            //{
            //    PhotoImage.IsVisible = true;
            //    PhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(mediaBytes));
            //}

            // If editing existing entry, load its data
            if (_entryId > 0)
            {
                LoadExistingEntry(_entryId);
            }
            else
            {
                EntryNameField.Text = "Note " +  DateTime.Now.Date.ToString();
            }
        }

        private async void LoadExistingEntry(int entryId)
        {
            try
            {
                var entry = await _database.GetEntryAsync(entryId);
                if (entry != null)
                {
                    EntryNameField.Text = entry.EntryName;
                    _latitude = entry.Latitude;
                    _longitude = entry.Longitude;
                    NotesEditor.Text = entry.Notes;

                    if (entry.Latitude.HasValue && entry.Longitude.HasValue)
                    {
                        LocationLabel.Text = $"Lat: {entry.Latitude:F6}, Lon: {entry.Longitude:F6}";
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to load entry: {ex.Message}", "OK");
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
                    await DisplayAlertAsync("Permission Denied", "Location permission is required", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to get location: {ex.Message}", "OK");
            }
        }

        private async void OnRecordClicked(object sender, EventArgs e)
        {
            DisplayAlertAsync("Coming Soon", "Voice recording will be implemented next", "OK");
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {

                // Save voice recording if exists
                int? voiceRecordingId = null;
                if (_voiceRecordingBytes != null)
                {
                    voiceRecordingId = await _database.SaveVoiceRecordingAsync(_voiceRecordingBytes, 0);
                }

                // Create and save the entry
                var entry = new NoteEntry
                {
                    EntryName = EntryNameField.Text,
                    VoiceRecordingId = voiceRecordingId,
                    Latitude = _latitude,
                    Longitude = _longitude,
                    Notes = NotesEditor.Text,
                    FilePaths = this._filePaths
                };

                await _database.SaveNoteEntryAsync(entry);

                await DisplayAlertAsync("Success", "Entry saved to database!", "OK");

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to save: {ex.Message}", "OK");
            }
        }
    }
}