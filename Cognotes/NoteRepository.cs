using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;

namespace Cognotes
{
    public class NoteRepository
    {
        public Action OnNoteSaved;
        string dbpath;
        public NoteRepository(string dbpath)
        {
            this.dbpath = dbpath;
        }

        public IEnumerable<Note> SearchNotes(string searchTerms)
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
                    OnNoteSaved?.Invoke();
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
            OnNoteSaved?.Invoke();
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
