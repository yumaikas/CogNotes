using System;
using System.Windows.Input;

namespace Cognotes
{
    public class NoteViewModel: ChangeNotifier
    {
        private string content;
        private string tagline;
        private long? id;
        private bool is_editing;
        public string Content { get => content; set => SetField(ref content, value); }
        public string Tagline { get => tagline; set => SetField(ref tagline, value); }
        public long? Id { get => id; set => SetField(ref id, value); }
        public bool IsEditing { get => is_editing; 
            set {
                is_editing = value;
                notifyEditingChange();
            }
        }

        public string ButtonText {
            get {
                if (is_editing) { return "Save"; }
                else { return "Edit"; }
            }
        }
        public bool IsNotEditing { get => !is_editing; }

        public ICommand EditOrSaveNote { get; private set; }
        private NoteRepository db;

        private void notifyEditingChange()
        {
            OnPropertyChanged("IsEditing");
            OnPropertyChanged("IsNotEditing");
            OnPropertyChanged("ButtonText");
        }

        private Note lastSavedNote;
        public void SaveNote() {
            Note toSave = new Note()
            {
                Id = Id,
                Content = Content,
                Tagline = Tagline
            };

            if (lastSavedNote?.Id != null && lastSavedNote?.Id != toSave.Id)
            {
                var ex = new Exception("The id of this note, and the id of the last saved not do not match!");
                ex.Data["MyId"] = Id;
                ex.Data["LastSavedId"] = lastSavedNote?.Id;
                throw ex;
            }

            // Save the notes if they are not identical
            bool shouldSaveNote = 
                !(lastSavedNote?.Id == toSave.Id
                && lastSavedNote?.Content == toSave.Content
                && lastSavedNote?.Tagline == toSave.Tagline);

            if (shouldSaveNote) {
                db.SaveNote(toSave);
                lastSavedNote = toSave;
            }
        }
        private void editOrSaveNote()
        {
            if (is_editing)
            {
                // Todo: Save note
                db.SaveNote(new Note() {
                    Id = Id,
                    Content = content,
                    Tagline = Tagline
                });
                IsEditing = false;
            }
            else
            {
                IsEditing = true;
            }
        }

        public void AssignNote(Note n)
        {
            if (n.Id != Id)
            {
                var ex = new Exception("Note ID and NoteViewModel ID do not match!");
                ex.Data["NewNoteId"] = n.Id;
                ex.Data["MyId"] = Id;
                throw ex;
            }
            Content = n.Content;
            Tagline = n.Tagline;
        }
        public NoteViewModel(Note n, NoteRepository db)
        {
            EditOrSaveNote = new Cmd(
                () => editOrSaveNote(), 
                () => id.HasValue
            );
            this.db = db;
            content = n.Content;
            tagline = n.Tagline;
            id = n.Id;
            IsEditing = false;
        }
    }
}
