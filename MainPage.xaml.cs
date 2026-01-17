using Microsoft.Maui.Media;
using FieldNotesApp.Services;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Alerts;

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
            await MenuOverlay.FadeTo(0.5, 200);

            // Rotate FAB icon
            FabIcon.RotateTo(45, 250, Easing.SpringOut);

            // Animate menu items
            await Task.WhenAll(
                MenuItems.TranslateTo(0, 0, 300, Easing.CubicOut),
                MenuItems.FadeTo(1, 250),
                MenuItems.ScaleTo(1, 300, Easing.SpringOut)
            );
        }

        private async Task CloseMenu()
        {
            _isMenuOpen = false;

            // Rotate FAB back
            FabIcon.RotateTo(0, 200, Easing.CubicIn);

            // Animate everything away
            await Task.WhenAll(
                MenuItems.TranslateTo(0, 50, 200, Easing.CubicIn),
                MenuItems.FadeTo(0, 200),
                MenuItems.ScaleTo(0.8, 200, Easing.CubicIn)
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
                MenuItems.TranslateTo(0, 0, 1, Easing.Linear),
                MenuItems.FadeTo(1, 1),
                MenuItems.ScaleTo(1, 1)
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

        // Helper class for display
        public class EntryDisplayModel
        {
            public int Id { get; set; }
            public string EntryName { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}