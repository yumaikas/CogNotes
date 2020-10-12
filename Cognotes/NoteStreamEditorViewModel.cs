using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Input;

namespace Cognotes
{
    public class NoteStreamEditorViewModel: ChangeNotifier, IDisposable
    {
        NoteRepository db;
        public NoteStreamEditorViewModel(NoteRepository db, string title, Cmd newNoteCol, Func<NoteStreamEditorViewModel, Cmd> getRemoveCommand)
        {
            this.db = db;
            db.OnNoteSaved += refreshNotes;
            this.title = title;
            SaveNote = new Cmd(saveNote, canSaveNote);
            RemoveNoteCol = getRemoveCommand(this);

            NewNoteCol = newNoteCol;
        }

        private void refreshNotes()
        {
             SearchResults = searchNotes(db, searchTerms);
        }


        private bool canSaveNote() {
            return (DraftNotes?.Length ?? 0) > 0 && (DraftTagline?.Length ?? 0) > 0;
        }
        private void saveNote() {
            db.SaveNote(new Note()
            {
                Content = DraftNotes,
                Tagline = DraftTagline,
            });
            DraftNotes = "";
            refreshNotes();
            db.OnNoteSaved -= refreshNotes;
            db.NotifyChanges();
            db.OnNoteSaved += refreshNotes;
        }

        private static ObservableCollection<NoteViewModel> searchNotes(NoteRepository db, string searchTerms)
        {
            return new ObservableCollection<NoteViewModel>(
                db.ViewNotesForSearch(searchTerms).Take(5)
            );
        }

        public void Dispose()
        {
            db.OnNoteSaved -= refreshNotes;
        }

        public ICommand NewNoteCol { get; private set; }
        public ICommand RemoveNoteCol { get; private set; }
        public ICommand SaveNote { get; private set; }
        private string title;
        public string Title { get => title; set => SetField(ref title, value); }
        private string draftNotes;
        public string DraftNotes { get => draftNotes; set => SetField(ref draftNotes, value); }
        private string draftTagline;
        public string DraftTagline { get => draftTagline; set => SetField(ref draftTagline, value); }
        private string searchTerms;
        public string SearchTerms { get => searchTerms;
            set  { 
                if (SetField(ref searchTerms, value)) {
                    SearchResults = searchNotes(db, value);
                }
            } 
        }

        private ObservableCollection<NoteViewModel> searchResults;
        public ObservableCollection<NoteViewModel> SearchResults { get => searchResults; set => SetField(ref searchResults, value); }
    }
}
