namespace FieldNotesApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // Register routes for navigation
            Routing.RegisterRoute(nameof(PhotoDetailPage), typeof(PhotoDetailPage));
        }
    }
}
