using SQLite;

namespace FieldNotesApp.Models
{
    [Table("photo_entries")]
    public class PhotoEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string EntryName { get; set; }

        public bool IsVideo { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int MediaId { get; set; }  // Foreign key to media table

        public int? VoiceRecordingId { get; set; } // Optional Foreign key to voice recording
    }

    [Table("media")]
    public class Media
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public bool IsVideo { get; set; }

        [MaxLength(50)]
        public string MimeType { get; set; } // "image/jpeg" or "video/mp4"

        public byte[] Data { get; set; } // Store the actual bytes

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("voice_recordings")]
    public class VoiceRecording
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public byte[] Data { get; set; }

        public int DurationSeconds { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}