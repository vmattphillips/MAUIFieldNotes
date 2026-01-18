using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Views;
using FieldNotesApp.Models;
using FieldNotesApp.Services;
using Microsoft.Maui.Media;

namespace FieldNotesApp
{
    public partial class MainPage : ContentPage
    {
        private readonly DatabaseService _database;
        private bool _isMenuOpen = false;
        private bool _menuInitialized = false;

        public MainPage(DatabaseService database)
        {
            InitializeComponent();
            _database = database;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadEntries();  // Refresh list when returning to page
            
            // Initialize menu animation on first load
            if (!_menuInitialized)
            {
                _ = InitializeMenu();
            }
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
                    EntryName = e.EntryName,
                    CreatedAt = e.CreatedAt,
                    NumOfMedia = e.FilePaths.Count
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
                    await Navigation.PushAsync(new PhotoDetailPage(_database, photoEntry.FilePaths, photoEntry.Id));
                }

                // Deselect the item
                EntriesCollectionView.SelectedItem = null;
            }
        }

        private async void OnAddExistingClicked(object sender, EventArgs e)
        {
            await CloseMenu();
            try
            {
                var mediaFiles = await MediaPicker.PickPhotosAsync(new MediaPickerOptions
                {
                    SelectionLimit = 50,
                    CompressionQuality = 100,
                    RotateImage = true,
                    PreserveMetaData = true,
                });

                if (mediaFiles != null && mediaFiles.Any())
                {
                    var filePaths = new List<string>();

                    foreach (var media in mediaFiles)
                    {
                        filePaths.Add(media.FullPath);
                    }

                    // Navigate once with all file paths
                    await Navigation.PushAsync(new PhotoDetailPage(_database, filePaths));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed to select media: {ex.Message}", "OK");
            }
        }

        private async void OnTakePhotoClicked(object sender, EventArgs e)
        {
            await CloseMenu();
            try
            {
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
#if ANDROID
                    await AndroidMediaSaver.SavePhotoToDcimAsync(photo);
                    Toast.Make("A copy has been saved to your camera folder").Show();
#endif

                    var filePaths = new List<string> { photo.FullPath };
                    await Navigation.PushAsync(new PhotoDetailPage(_database, filePaths));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed: {ex.Message}", "OK");
            }
        }

        private async void OnRecordVideoClicked(object sender, EventArgs e)
        {
            await CloseMenu();
            try
            {
                var video = await MediaPicker.CaptureVideoAsync();

                if (video != null)
                {
#if ANDROID
                    Toast.Make("A copy has been saved to your camera folder").Show();
#endif

                    var filePaths = new List<string> { video.FullPath };
                    await Navigation.PushAsync(new PhotoDetailPage(_database, filePaths));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"Failed: {ex.Message}", "OK");
            }
        }

        private async void OnFabClicked(object sender, EventArgs e)
        {
            if (_isMenuOpen)
            {
                await CloseMenu();
            }
            else
            {
                await OpenMenu();
            }
        }

        private async Task OpenMenu()
        {
            _isMenuOpen = true;

            // Now show with animation
            MenuItems.IsVisible = true;

            // Fade overlay smoothly
            MenuOverlay.IsVisible = true;
            await MenuOverlay.FadeToAsync(0.5, 200);

            // Rotate FAB icon
            FabIcon.RotateToAsync(45, 250, Easing.SpringOut);

            // Animate menu items
            await Task.WhenAll(
                MenuItems.TranslateToAsync(0, 0, 300, Easing.CubicOut),
                MenuItems.FadeToAsync(1, 250),
                MenuItems.ScaleToAsync(1, 300, Easing.SpringOut)
            );
        }

        private async Task CloseMenu()
        {
            _isMenuOpen = false;

            // Rotate FAB back
            FabIcon.RotateToAsync(0, 200, Easing.CubicIn);

            // Animate everything away
            await Task.WhenAll(
                MenuItems.TranslateToAsync(0, 50, 200, Easing.CubicIn),
                MenuItems.FadeToAsync(0, 200),
                MenuItems.ScaleToAsync(0.8, 200, Easing.CubicIn)
            );

            MenuOverlay.IsVisible = false;
            MenuItems.IsVisible = false;
            MenuOverlay.Opacity = 0.5; // Reset for next time
        }

        private async Task InitializeMenu()
        {
            // Pre-render menu completely off-screen
            MenuOverlay.Opacity = 0;
            MenuOverlay.IsVisible = true;
            MenuItems.IsVisible = true;
            MenuItems.Opacity = 0;

            // Let it render
            await Task.Delay(50);

            // Now animate it in and immediately back out (user won't see this)
            MenuOverlay.Opacity = 0.5;
            await Task.WhenAll(
                MenuItems.TranslateToAsync(0, 0, 1, Easing.Linear),
                MenuItems.FadeToAsync(1, 1),
                MenuItems.ScaleToAsync(1, 1)
            );

            // Instantly hide again
            MenuOverlay.IsVisible = false;
            MenuOverlay.Opacity = 0;
            MenuItems.IsVisible = false;
            MenuItems.TranslationY = 100;
            MenuItems.Opacity = 0;
            MenuItems.Scale = 0.8;

            _menuInitialized = true;
        }

        private async void OnOverlayTapped(object sender, EventArgs e)
        {
            await CloseMenu();
        }

        private async void OnDeleteTapped(object sender, EventArgs e)
        {
            var border = (Border)sender;
            var displayEntry = (EntryDisplayModel)border.BindingContext;

            bool confirm = await DisplayAlertAsync(
                "Delete Entry",
                "Are you sure you want to delete this entry? The Photo will remain in your gallery.",
                "Yes",
                "No");

            if (confirm)
            {
                var fullEntry = await this._database.GetEntryAsync(displayEntry.Id);

                if (fullEntry != null)
                {
                    await this._database.DeleteEntryAsync(fullEntry);
                    LoadEntries();
                }
                else
                {
                    await DisplayAlertAsync("Error", "Entry not found", "OK");
                }
            }
        }

        // Helper class for display
        public class EntryDisplayModel
        {
            public int Id { get; set; }
            public string EntryName { get; set; }
            public DateTime CreatedAt { get; set; }
            public int NumOfMedia {get; set; }
        }
    }
}