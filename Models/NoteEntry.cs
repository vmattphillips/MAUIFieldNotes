using SQLite;

namespace FieldNotesApp.Models
{
    public class NoteEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public List<string> FilePaths { get; set; }
        public string EntryName { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? VoiceRecordingId { get; set; } // Optional Foreign key to voice recording
    }

    public class VoiceRecording
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public byte[] Data { get; set; }

        public int DurationSeconds { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}