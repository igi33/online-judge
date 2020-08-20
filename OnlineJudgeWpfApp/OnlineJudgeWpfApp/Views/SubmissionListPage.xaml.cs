using OnlineJudgeWpfApp.Models;
using OnlineJudgeWpfApp.Operations;
using OnlineJudgeWpfApp.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace OnlineJudgeWpfApp.Views
{
    /// <summary>
    /// Interaction logic for SubmissionListPage.xaml
    /// </summary>
    public partial class SubmissionListPage : Page
    {
        public MainWindowViewModel MainWindowVm { get; set; }

        public int SelectedId { get; set; } // Row with SelectedId will be highlighted

        public SubmissionListPage(MainWindowViewModel mainWindowVm, int selectedId = 0)
        {
            InitializeComponent();

            MainWindowVm = mainWindowVm;
            SelectedId = selectedId;
        }

        private void submissionListPage_Loaded(object sender, RoutedEventArgs e)
        {
            ShowSubmissions();
        }

        private void ShowSubmissions()
        {
            SubmissionOperations ops = new SubmissionOperations();
            List<Submission> submissions = ops.GetSubmissions(0, 0, 50);
            if (submissions == null)
            {
                MessageBox.Show("Submissions request failed");
            }
            else
            {
                int index = submissions.FindIndex(s => s.Id == SelectedId);
                if (index >= 0)
                {
                    submissions[index].Selected = true;
                }
                dgSubmissions.ItemsSource = submissions;
            }
        }

        public void goToTaskPage(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int id = (int)button.Tag;
            NavigationService.Navigate(new TaskDetailsPage(MainWindowVm, id));
        }

        public void goToProfilePage(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int id = (int)button.Tag;
            NavigationService.Navigate(new ProfilePage(MainWindowVm, id));
        }

        private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }
    }
}
