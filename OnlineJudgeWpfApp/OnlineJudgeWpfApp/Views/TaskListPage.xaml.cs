using OnlineJudgeWpfApp.Operations;
using OnlineJudgeWpfApp.ViewModels;
using OnlineJudgeWpfApp.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace OnlineJudgeWpfApp.Views
{
    /// <summary>
    /// Interaction logic for TaskListPage.xaml
    /// </summary>
    public partial class TaskListPage : Page
    {
        public MainWindowViewModel MainWindowVm { get; set; }

        public int TagId { get; set; }

        public TaskListPage(MainWindowViewModel mainWindowVm, int tagId = 0)
        {
            InitializeComponent();

            MainWindowVm = mainWindowVm;
            TagId = tagId;
        }

        private void taskListPage_Loaded(object sender, RoutedEventArgs e)
        {
            ShowTasks();
        }

        private void ShowTasks()
        {
            TaskOperations ops = new TaskOperations();
            List<Models.Task> tasks = ops.GetTasks(TagId);
            if (tasks == null)
            {
                MessageBox.Show("Tasks request failed");
            }
            else
            {
                if (TagId != 0)
                {
                    TagOperations tagOps = new TagOperations();
                    Tag tag = tagOps.GetTag(TagId);
                    if (tag != null)
                    {
                        // Add Tag name to heading text
                        tbkHeading.Text += string.Format(" - Tagged {0}", tag.Name);
                    }
                }
                dgTasks.ItemsSource = tasks;
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

        private void Create_Task_Nav_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TaskFormPage(MainWindowVm));
        }
    }
}
