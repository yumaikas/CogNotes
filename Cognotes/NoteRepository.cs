using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace Cognotes
{
    public class NoteRepository
    {
        string dbpath;
        public NoteRepository(string dbpath)
        {
            this.dbpath = dbpath;
        }

        public IEnumerable<Note> SearchNotes(string searchTerms)
        {
            using(SQLiteConnection conn = new SQLiteConnection(dbpath))
            using (SQLiteCommand cmd = conn.CreateCommand()) {
                cmd.CommandText = @"
                    Select NoteId as Id, Content, Tagline, Created, Updated
                    from Notes
                    where
                        Content like '%' || @searchTerms || '%'
                        AND Content not like '%@archive%'
                    order by Created DESC
                ";
                cmd.Parameters.AddWithValue("@searchTerms", searchTerms);

                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        yield return new Note() { 
                            Id = (int)reader["Id"],
                            Content = (string)reader["Content"],
                            Tagline = (string)reader["Tagline"],
                            Created = DateTimeOffset.FromUnixTimeSeconds((int?)reader["Created"] ?? 0).LocalDateTime,
                            Updated = DateTimeOffset.FromUnixTimeSeconds((int?)reader["Updated"]?? 0).LocalDateTime
                        };
                    }
                }
            }
        }

        public async Task<int> SaveNote(Note n)
        {
            using (SQLiteConnection conn = new SQLiteConnection(dbpath))
            using (SQLiteCommand cmd = conn.CreateCommand())
            {
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
                        where NoteID = ?
                    ";

                    cmd.Parameters.AddWithValue("@content", n.Content);
                    cmd.Parameters.AddWithValue("@tagline", n.Tagline);
                    cmd.Parameters.AddWithValue("@note_id", n.Id.Value);
                    return (int)await cmd.ExecuteScalarAsync();
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
                    return (int)await cmd.ExecuteScalarAsync();
                }
            }
        }

        public void CreateTables() { 
            using(SQLiteConnection conn = new SQLiteConnection(dbpath))
            using(SQLiteCommand cmd = conn.CreateCommand())
            {
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
