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

            if (_database == null)
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "fieldnotes.db3");
                _database = new SQLiteAsyncConnection(dbPath);
            }
            await _database.CreateTableAsync<NoteEntry>();
            await _database.CreateTableAsync<VoiceRecording>();
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

        // NoteEntry operations
        public async Task<int> SaveNoteEntryAsync(NoteEntry entry)
        {
            await InitAsync();

            entry.SerializeFilePaths();

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

        public async Task<List<NoteEntry>> GetAllEntriesAsync()
        {
            await InitAsync();
            var entries = await _database.Table<NoteEntry>()
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            // Deserialize FilePaths for each entry
            foreach (var entry in entries)
            {
                entry.DeserializeFilePaths();
            }

            return entries;
        }

        public async Task<NoteEntry?> GetEntryAsync(int id)
        {
            await InitAsync();
            var entry = await _database.Table<NoteEntry>()
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync();

            // Deserialize FilePaths if entry exists
            entry?.DeserializeFilePaths();

            return entry;
        }

        public async Task<int> DeleteEntryAsync(NoteEntry entry)
        {
            await InitAsync();

            // Delete associated voice recording
            if (entry.VoiceRecordingId.HasValue)
            {
                await _database.DeleteAsync<VoiceRecording>(entry.VoiceRecordingId.Value);
            }

            // Delete the entry itself
            return await _database.DeleteAsync(entry);
        }

        // Helper method to get entry with all related data
        public async Task<(NoteEntry? entry, VoiceRecording? voiceRecording)> GetFullEntryAsync(int entryId)
        {
            await InitAsync();
            var entry = await GetEntryAsync(entryId);

            if (entry == null)
                return (null, null);

            VoiceRecording? voiceRecording = null;
            if (entry.VoiceRecordingId.HasValue)
            {
                voiceRecording = await GetVoiceRecordingAsync(entry.VoiceRecordingId.Value);
            }

            return (entry, voiceRecording);
        }
    }
}