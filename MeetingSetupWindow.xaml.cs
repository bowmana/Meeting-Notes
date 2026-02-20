using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace MeetingNotesApp
{
    public partial class MeetingSetupWindow : Window, INotifyPropertyChanged
    {
        private DateTime _meetingDate = DateTime.Today;
        private string _meetingTitle = "";
        private string _meetingOrganizer = "";
        private string _meetingAttendees = "";
        private string _meetingComments = "";
        private NotionWorkspaceIntegration _selectedWorkspace;
        private ObservableCollection<NotionWorkspaceIntegration> _availableWorkspaces;

        public MeetingSetupWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Initialize with sample workspaces (in real app, load from settings)
            AvailableWorkspaces = new ObservableCollection<NotionWorkspaceIntegration>
            {
                new NotionWorkspaceIntegration 
                { 
                    WorkspaceName = "Personal Workspace", 
                    WorkspaceId = "personal-123",
                    StatusText = "Connected",
                    StatusColor = Brushes.Green
                },
                new NotionWorkspaceIntegration 
                { 
                    WorkspaceName = "Work Team", 
                    WorkspaceId = "work-456",
                    StatusText = "Connected",
                    StatusColor = Brushes.Green
                },
                new NotionWorkspaceIntegration 
                { 
                    WorkspaceName = "Client Projects", 
                    WorkspaceId = "client-789",
                    StatusText = "Connected",
                    StatusColor = Brushes.Green
                }
            };

            WorkspaceComboBox.ItemsSource = AvailableWorkspaces;
            WorkspaceComboBox.DisplayMemberPath = "WorkspaceName";

            // Set default workspace
            if (AvailableWorkspaces.Count > 0)
            {
                SelectedWorkspace = AvailableWorkspaces[0];
            }
        }

        public DateTime MeetingDate
        {
            get => _meetingDate;
            set
            {
                _meetingDate = value;
                OnPropertyChanged();
            }
        }

        public string MeetingTitle
        {
            get => _meetingTitle;
            set
            {
                _meetingTitle = value;
                OnPropertyChanged();
            }
        }

        public string MeetingOrganizer
        {
            get => _meetingOrganizer;
            set
            {
                _meetingOrganizer = value;
                OnPropertyChanged();
            }
        }

        public string MeetingAttendees
        {
            get => _meetingAttendees;
            set
            {
                _meetingAttendees = value;
                OnPropertyChanged();
            }
        }

        public string MeetingComments
        {
            get => _meetingComments;
            set
            {
                _meetingComments = value;
                OnPropertyChanged();
            }
        }

        public NotionWorkspaceIntegration SelectedWorkspace
        {
            get => _selectedWorkspace;
            set
            {
                _selectedWorkspace = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<NotionWorkspaceIntegration> AvailableWorkspaces
        {
            get => _availableWorkspaces;
            set
            {
                _availableWorkspaces = value;
                OnPropertyChanged();
            }
        }

        private void OnStartNotesClicked(object sender, RoutedEventArgs e)
        {
            if (SelectedWorkspace == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(MeetingTitle))
            {
                return;
            }

            // Create meeting info object
            var meetingInfo = new MeetingInfo
            {
                Date = MeetingDate,
                Title = MeetingTitle,
                Organizer = MeetingOrganizer,
                Attendees = MeetingAttendees,
                Comments = MeetingComments,
                Workspace = SelectedWorkspace,
                StartTime = DateTime.Now
            };

            // Open note-taking window with meeting info
            var noteWindow = new NoteTakingWindow(meetingInfo);
            noteWindow.Show();

            // Close this window
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MeetingInfo
    {
        public DateTime Date { get; set; }
        public string Title { get; set; }
        public string Organizer { get; set; }
        public string Attendees { get; set; }
        public string Comments { get; set; }
        public NotionWorkspaceIntegration Workspace { get; set; }
        public DateTime StartTime { get; set; }
    }
}
