using OnlineJudgeWpfApp.Models;
using OnlineJudgeWpfApp.Operations;
using OnlineJudgeWpfApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace OnlineJudgeWpfApp.Views
{
    /// <summary>
    /// Interaction logic for SubmissionListPage.xaml
    /// </summary>
    public partial class SubmissionListPage : Page
    {
        public MainWindowViewModel MainWindowVm { get; set; }

        public int SelectedId { get; set; }

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
            List<Submission> submissions = ops.GetSubmissions();
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
            int id = int.Parse(button.Tag.ToString());
            NavigationService.Navigate(new TaskDetailsPage(MainWindowVm, id));
        }

        public void goToProfilePage(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int id = int.Parse(button.Tag.ToString());
            NavigationService.Navigate(new ProfilePage(MainWindowVm, id));
        }
    }
}
