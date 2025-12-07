using SQLite;
using FieldNotesApp.Models;

namespace FieldNotesApp.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        public DatabaseService()
        {
        }

        private async Task InitAsync()
        {
            if (_database != null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "fieldnotes.db3");
            _database = new SQLiteAsyncConnection(dbPath);

            await _database.CreateTableAsync<PhotoEntry>();
            await _database.CreateTableAsync<Media>();
            await _database.CreateTableAsync<VoiceRecording>();
        }

        // Media operations
        public async Task<int> SaveMediaAsync(byte[] mediaBytes, bool isVideo)
        {
            await InitAsync();

            var media = new Media
            {
                Data = mediaBytes,
                IsVideo = isVideo,
                MimeType = isVideo ? "video/mp4" : "image/jpeg"
            };

            await _database.InsertAsync(media);
            return media.Id;
        }

        public async Task<Media> GetMediaAsync(int id)
        {
            await InitAsync();
            return await _database.Table<Media>().Where(m => m.Id == id).FirstOrDefaultAsync();
        }

        // Voice recording operations
        public async Task<int> SaveVoiceRecordingAsync(byte[] audioBytes, int durationSeconds)
        {
            await InitAsync();

            var recording = new VoiceRecording
            {
                Data = audioBytes,
                DurationSeconds = durationSeconds
            };

            await _database.InsertAsync(recording);
            return recording.Id;
        }

        public async Task<VoiceRecording> GetVoiceRecordingAsync(int id)
        {
            await InitAsync();
            return await _database.Table<VoiceRecording>().Where(v => v.Id == id).FirstOrDefaultAsync();
        }

        // PhotoEntry operations
        public async Task<int> SavePhotoEntryAsync(PhotoEntry entry)
        {
            await InitAsync();

            if (entry.Id == 0)
            {
                await _database.InsertAsync(entry);
            }
            else
            {
                await _database.UpdateAsync(entry);
            }

            return entry.Id;
        }

        public async Task<List<PhotoEntry>> GetAllEntriesAsync()
        {
            await InitAsync();
            return await _database.Table<PhotoEntry>()
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<PhotoEntry> GetEntryAsync(int id)
        {
            await InitAsync();
            return await _database.Table<PhotoEntry>().Where(e => e.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> DeleteEntryAsync(PhotoEntry entry)
        {
            await InitAsync();

            // Delete associated media
            if (entry.MediaId > 0)
            {
                await _database.DeleteAsync<Media>(entry.MediaId);
            }

            // Delete associated voice recording
            if (entry.VoiceRecordingId.HasValue)
            {
                await _database.DeleteAsync<VoiceRecording>(entry.VoiceRecordingId.Value);
            }

            // Delete the entry itself
            return await _database.DeleteAsync(entry);
        }

        // Helper method to get entry with all related data
        public async Task<(PhotoEntry entry, Media media, VoiceRecording voiceRecording)> GetFullEntryAsync(int entryId)
        {
            await InitAsync();

            var entry = await GetEntryAsync(entryId);
            if (entry == null)
                return (null, null, null);

            var media = await GetMediaAsync(entry.MediaId);
            VoiceRecording voiceRecording = null;

            if (entry.VoiceRecordingId.HasValue)
            {
                voiceRecording = await GetVoiceRecordingAsync(entry.VoiceRecordingId.Value);
            }

            return (entry, media, voiceRecording);
        }
    }
}