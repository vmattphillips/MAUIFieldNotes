using SQLite;
using System.Text.Json;

namespace FieldNotesApp.Models
{
    public class NoteEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Ignore]
        public List<string> FilePaths { get; set; }
        public string FilePathsJson { get; set; }
        public string EntryName { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? VoiceRecordingId { get; set; } // Optional Foreign key to voice recording

        public void SerializeFilePaths()
        {
            FilePathsJson = FilePaths != null
                ? JsonSerializer.Serialize(FilePaths)
                : "[]"; // Empty JSON array instead of null
        }

        public void DeserializeFilePaths()
        {
            if (string.IsNullOrEmpty(FilePathsJson))
            {
                FilePaths = new List<string>();
            }
            else
            {
                FilePaths = JsonSerializer.Deserialize<List<string>>(FilePathsJson) ?? new List<string>();
            }
        }
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