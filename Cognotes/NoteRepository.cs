using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;

namespace Cognotes
{
    public class NoteRepository: IDisposable
    {
        public Action OnNoteSaved;
        string dbpath;
        // HAX: We're going to assume that we'll never have enough
        // cached note view models here that we'd need to evict
        // them. 
        Dictionary<long, NoteViewModel> viewCache;
        public NoteRepository(string dbpath)
        {
            this.dbpath = dbpath;
            viewCache = new Dictionary<long, NoteViewModel>();
        }
        public void SaveOpenNotes() {
            // Save any notes that have been left open by accident.
            foreach (var nvm in viewCache.Values) {
                if (nvm.IsEditing) {
                    nvm.SaveNote();
                }
            }
        }
        public void Dispose()
        {
            SaveOpenNotes();
        }
        public IEnumerable<NoteViewModel> ViewNotesForSearch(string searchTerms)
        {
            foreach (Note n in SearchNotes(searchTerms))
            {
                NoteViewModel view;
                if (viewCache.TryGetValue(n.Id.Value, out view)) {
                    if (!view.IsEditing) {
                        view.AssignNote(n);
                    }
                    // Don't refresh notes that haven't been saved yet.
                } else {
                    viewCache[n.Id.Value] = new NoteViewModel(n, this);
                }
                yield return viewCache[n.Id.Value];
            }
        }

        private IEnumerable<Note> SearchNotes(string searchTerms)
        {
            using(SQLiteConnection conn = new SQLiteConnection(dbpath))
            using (SQLiteCommand cmd = conn.CreateCommand()) {
                conn.Open();
                cmd.CommandText = @"
                    Select NoteId as Id, Content, Tagline, Created, Updated
                    from Notes
                    where
                        (Content like '%' || @searchTerms || '%'
                        OR Tagline like '%' || @searchTerms || '%')
                        and (
                            @searchTerms like '%archive%'
                            OR
                            (Content not like '%@archive%'
                            AND Tagline not like '%@archive%'))
                    order by Created DESC
                ";
                cmd.Parameters.AddWithValue("@searchTerms", searchTerms);

                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        var n = new Note();
                        n.Id = (long)reader["Id"];
                        n.Content = (string)reader["Content"];
                        n.Tagline = (string)reader["Tagline"];
                        n.Created = DateTimeOffset.FromUnixTimeSeconds(IfNull(reader, "Created", 0)).LocalDateTime;
                        n.Updated = DateTimeOffset.FromUnixTimeSeconds(IfNull(reader, "Updated", 0)).LocalDateTime;
                        yield return n;
                    }
                }
            }
        }
        public void NotifyChanges()
        {
            OnNoteSaved?.Invoke();
        }

        private T IfNull<T>(IDataReader dr, string Key, T fallback)
        {
            if(dr.IsDBNull(dr.GetOrdinal(Key))) {
                return fallback;
            }
            return (T)dr[Key];
        }

        public long SaveNote(Note n)
        {
            long retval;
            using (SQLiteConnection conn = new SQLiteConnection(dbpath))
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
                conn.Open();
                if (n.Id.HasValue) { 
                    // Save note
                    cmd.CommandText = @"
                    Insert into NoteHistory
                        (NoteId, Content, Tagline, Created, Updated)
                        Select @note_id, Content, Tagline, Created, strftime('%s', 'now')
                        from Notes where NoteID = @note_id;

                        Update Notes
                            Set
                                Content = @content,
                                Tagline = @tagline,
                                Updated = strftime('%s', 'now')
                        where NoteID = @note_id;
                        Select @note_id;
                    ";

                    cmd.Parameters.AddWithValue("@content", n.Content);
                    cmd.Parameters.AddWithValue("@tagline", n.Tagline);
                    cmd.Parameters.AddWithValue("@note_id", n.Id.Value);
                    retval = (long)cmd.ExecuteScalar();
                } else
                {
                    // Create note
                    cmd.CommandText = @"
                    Insert into Notes (content, tagline, created) 
                        values (@content, @tagline, strftime('%s', 'now'));
                    Select last_insert_rowid();
                    ";
                    cmd.Parameters.AddWithValue("@content", n.Content);
                    cmd.Parameters.AddWithValue("@tagline", n.Tagline);
                    retval = (long)cmd.ExecuteScalar();
                }
            }
            return retval;
        }

        public void CreateTables() { 
            using(SQLiteConnection conn = new SQLiteConnection(dbpath))
            using(SQLiteCommand cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = @"
                    Create Table If Not Exists Notes (
                        NoteId INTEGER PRIMARY KEY,
                        Content text,
                        Tagline text,
                        Created int, -- unix timestamp
                        Updated int, -- unix timestamp
                        Deleted int -- unix timestamp
                    );
                    Create Table If Not Exists NoteHistory (
                        Id INTEGER PRIMARY KEY,
                        NoteId int,
                        Content text,
                        Tagline text,
                        Created int, -- unix timestamp
                        Updated int, -- unix timestamp
                        Deleted int -- unix timestamp
                    );
                    Create Table If Not Exists StateKV (
                        KvId INTEGER PRIMARY KEY,
                        Key text,
                        Content text,
                        Created int, -- unix timestamp
                        Updated int, -- unix timestamp
                        Deleted int -- unix timestamp
                    );
                    Create Unique Index If Not Exists UniqueKV ON StateKV(Key);
                    Create Index If Not Exists KVidx ON StateKV(Key, Content);
                ";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
