namespace FieldNotesApp.Models
{
    public class MediaItem
    {
        public string FilePath { get; set; }
        public bool IsImage { get; set; }
        public bool IsVideo { get; set; }

        public static MediaItem FromPath(string path)
        {
            var extension = Path.GetExtension(path).ToLower();
            var isVideo = extension == ".mp4" || extension == ".mov" ||
                         extension == ".avi" || extension == ".mkv";

            return new MediaItem
            {
                FilePath = path,
                IsImage = !isVideo,
                IsVideo = isVideo
            };
        }
    }
}