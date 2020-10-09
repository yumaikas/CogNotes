using System.Collections.ObjectModel;

namespace Cognotes
{
    public class CogNotesViewModel : ChangeNotifier
    {
        private ObservableCollection<NoteStreamEditorViewModel> noteStreams;
        public ObservableCollection<NoteStreamEditorViewModel> NoteStreams { 
            get => noteStreams; 
        }

        public void AddNoteStream(NoteStreamEditorViewModel stream)
        {
            noteStreams.Add(stream);
            OnPropertyChanged("NoteStreams");
        }

        public void RemoveNoteStream(NoteStreamEditorViewModel stream)
        {
            noteStreams.Remove(stream);
            OnPropertyChanged("NoteStreams");
        }

        private NoteRepository db;
        public CogNotesViewModel(NoteRepository db)
        {
            this.db = db;
            this.noteStreams = new ObservableCollection<NoteStreamEditorViewModel>();
        }
    }
}
