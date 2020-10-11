using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Cognotes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CogNotesMainWindow : Window
    {
        NoteRepository db;
        CogNotesViewModel vm;
        static string homeFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        Cmd addNoteCol;
        DispatcherTimer timerSaveDrafts;

        public CogNotesMainWindow()
        {
            timerSaveDrafts = new DispatcherTimer();
            timerSaveDrafts.Interval = TimeSpan.FromSeconds(30);

            db = new NoteRepository($@"Data Source={System.IO.Path.Combine(homeFolder, "cognotes.db")}");
            db.CreateTables();
            InitializeComponent();
            this.DataContext = vm = new CogNotesViewModel(db);
            addNoteCol = new Cmd(AddNoteCol, () => vm.NoteStreams.Count < 5);
            if (File.Exists(topicsPath()))
            {
                foreach (string topic in File.ReadAllLines(topicsPath()).Take(4))
                {
                    string[] elems = topic.Split('\x1f');
                    if (elems.Length != 2) { 
                        // Read the new version
                        vm.AddNoteStream(new NoteStreamEditorViewModel(db, topic, addNoteCol, getRemoveCommand) {
                            SearchTerms = topic
                        });
                    } else {
                        // Migrate up from old version.
                        vm.AddNoteStream(new NoteStreamEditorViewModel(db, elems[0], addNoteCol, getRemoveCommand) {
                            SearchTerms = elems[1]
                        });
                    }
                }
            }
            if (vm.NoteStreams.Count() == 0) {
                vm.AddNoteStream(new NoteStreamEditorViewModel(db, "Notes", addNoteCol, getRemoveCommand) {
                    SearchTerms = "",
                });
            }
            this.Closing += CogNotesMainWindow_Closing;
        }
        private string topicsPath() => System.IO.Path.Combine(homeFolder, ".cognos_topics.txt");

        private void CogNotesMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            File.WriteAllLines(
                topicsPath(),
                vm.NoteStreams.Select(x => x.Title + "\x1f" + x.SearchTerms));
            db.Dispose();
        }

        private Cmd getRemoveCommand(NoteStreamEditorViewModel toRemove)
        {
            return new Cmd(() => RemoveNoteCol(toRemove), () => vm.NoteStreams.Count > 1);
        }

        private void RemoveNoteCol(NoteStreamEditorViewModel colToRemove)
        {
            vm.RemoveNoteStream(colToRemove);
        }

        private void AddNoteCol()
        {
            vm.AddNoteStream(new NoteStreamEditorViewModel(db, $"Notes {vm.NoteStreams.Count}", addNoteCol, getRemoveCommand) {
                SearchTerms = ""
            });
        }
    }
}
