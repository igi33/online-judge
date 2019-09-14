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
    /// Interaction logic for TaskDetailsPage.xaml
    /// </summary>
    public partial class TaskDetailsPage : Page
    {
        MainWindowViewModel MainWindowVm { get; set; }

        public int Id { get; set; }

        public TaskDetailsPage(MainWindowViewModel mainWindowVm, int id)
        {
            InitializeComponent();

            MainWindowVm = mainWindowVm;
            Id = id;
        }

        /**
         * Details Page Loaded
         * @param  object  sender
         * @param  RoutedEventArgs e
         */
        private void taskPage_Loaded(object sender, RoutedEventArgs e)
        {
            ShowTaskDetails();
        }

        /**
         * Fetch Task Details
         */
        private void ShowTaskDetails()
        {
            TaskOperations ops = new TaskOperations();
            Models.Task task = ops.GetTaskDetails(Id);
            if (task == null)
            {
                MessageBox.Show("Task details request failed");
                NavigationService.Navigate(new LoginPage(MainWindowVm));
            }
            else
            {
                tbkId.Text = task.Id.ToString();
                tbkName.Text = task.Name;
                tbkDesc.Text = task.Description;
                tbkTimelimit.Text = task.TimeLimit.ToString();
                tbkDatetime.Text = task.TimeSubmitted.ToString();
                tbkSubmitter.Text = task.User.Username;
                tbkOrigin.Text = task.Origin;

                // Add tag buttons
                foreach (Tag t in task.Tags)
                {
                    Button b = new Button
                    {
                        Content = t.Name,
                        Tag = t.Id,
                        Padding = new Thickness(5, 5, 5, 5),
                        Margin = new Thickness(0, 0, 5, 0)
                    };
                    b.Click += GoToTaggedTasks_tb_Click;
                    spTags.Children.Add(b);
                }
            }

            ComputerLanguageOperations langOps = new ComputerLanguageOperations();
            List<ComputerLanguage> langs = langOps.GetLangs();

            List<ComboBoxItem> langItems = new List<ComboBoxItem>();
            foreach (ComputerLanguage cl in langs)
            {
                langItems.Add(new ComboBoxItem
                {
                    Tag = cl.Id,
                    Content = cl.Name
                });
            }
            cbLang.ItemsSource = langItems;
            cbLang.SelectedIndex = 0;
        }

        private void GoToTaggedTasks_tb_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int id = int.Parse(button.Tag.ToString());
            NavigationService.Navigate(new TaskListPage(MainWindowVm, id));
        }

        private void Submit_Solution(object sender, RoutedEventArgs e)
        {

        }
    }
}
