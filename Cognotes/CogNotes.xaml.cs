using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cognotes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CogNotesMainWindow : Window
    {
        NoteRepository db = new NoteRepository(@"Data Source=C:\temp\cognotes.db");
        public CogNotesMainWindow()
        {
            db.CreateTables();
            InitializeComponent();
        }
    }

    public class ChangeNotifier: INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
           => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
    public class NoteViewModel: ChangeNotifier
    {
        public NoteViewModel(Note n)
        {
            throw new NotImplementedException("TOOOOODOOOOO!");
        }
    }
    public class NoteStreamEditorViewModel: ChangeNotifier
    {
        NoteRepository db;
        public NoteStreamEditorViewModel(NoteRepository db)
        {
            this.db = db;
        }
        private string draftNotes;
        public string DraftNotes { get => draftNotes; set => SetField(ref draftNotes, value); }
        private string searchTerms;
        public string SearchTerms { get => searchTerms;
            set  { 
                if(SetField(ref searchTerms, value)) {
                    SetField(ref searchResults, db.SearchNotes(value).Select(n =>new NoteViewModel(n)).Take(5).ToList());
                }
            } 
        }

        private List<NoteViewModel> searchResults;
        public List<NoteViewModel> SearchResults { get => searchResults; set => SetField(ref searchResults, value); }
    }
}
