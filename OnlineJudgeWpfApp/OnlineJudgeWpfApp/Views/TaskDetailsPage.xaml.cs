using OnlineJudgeWpfApp.Models;
using OnlineJudgeWpfApp.Operations;
using OnlineJudgeWpfApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;

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
            ShowFastestSolutions();
            ShowAvailableLangs();
        }

        private void ShowFastestSolutions()
        {
            SubmissionOperations ops = new SubmissionOperations();
            List<Submission> submissions = ops.GetFastestSubmissionsOfTask(Id);
            if (submissions == null)
            {
                MessageBox.Show("Fastest solutions request failed");
                NavigationService.Navigate(new LoginPage(MainWindowVm));
            }
            else
            {
                if (submissions.Count > 0)
                {
                    dgFastest.ItemsSource = submissions;
                }
                else
                {
                    spFastest.Children.Add(new TextBlock
                    {
                        Text = "No accepted solutions",
                    });
                }
                
            }
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
                // Show Edit Task button if needed
                bool taskBelongsToCurrentUser = Globals.LoggedInUser != null && task.User.Id == Globals.LoggedInUser.Id;
                if (taskBelongsToCurrentUser)
                {
                    btnEditTask.Visibility = Visibility.Visible;
                }

                tbkId.Text = task.Id.ToString();
                tbkName.Text = task.Name;
                tbkDesc.Text = task.Description;
                tbkTimelimit.Text = task.TimeLimit.ToString();
                tbkDatetime.Text = task.TimeSubmitted.ToString();

                btnSubmitter.Content = task.User.Username;
                btnSubmitter.Tag = task.Id;

                // Check if task.Origin is a valid HTTP or HTTPS URL, or just text
                bool originIsValidLink = Uri.TryCreate(task.Origin, UriKind.Absolute, out Uri uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                // Handle accordingly
                if (originIsValidLink)
                {
                    Hyperlink hl = new Hyperlink
                    {
                        NavigateUri = new Uri(task.Origin),
                    };
                    hl.Inlines.Add(new Run(task.Origin));
                    hl.RequestNavigate += Hyperlink_RequestNavigate;

                    tbkOrigin.Inlines.Add(hl);
                }
                else
                {
                    tbkOrigin.Text = task.Origin;
                }

                // Handle tag buttons if any
                if (task.Tags.Count > 0)
                {
                    // Add tag buttons
                    foreach (Tag t in task.Tags)
                    {
                        Button b = new Button
                        {
                            Content = t.Name,
                            Tag = t.Id,
                            Padding = new Thickness(5, 5, 5, 5),
                            Margin = new Thickness(0, 0, 5, 0),
                            Cursor = Cursors.Hand,
                        };
                        b.Click += GoToTaggedTasks_tb_Click;
                        spTags.Children.Add(b);
                    }
                }
                else
                {
                    spTags.Children.Add(new TextBlock
                    {
                        Text = "No tags attached",
                        Padding = new Thickness(5),
                    });
                }

            }

        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Username_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int id = (int)button.Tag;
            NavigationService.Navigate(new ProfilePage(MainWindowVm, id));
        }

        private void GoToTaggedTasks_tb_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int id = (int)button.Tag;
            NavigationService.Navigate(new TaskListPage(MainWindowVm, id));
        }

        private void ShowAvailableLangs()
        {
            ComputerLanguageOperations langOps = new ComputerLanguageOperations();
            List<ComputerLanguage> langs = langOps.GetLangs();

            List<ComboBoxItem> langItems = new List<ComboBoxItem>();
            foreach (ComputerLanguage cl in langs)
            {
                langItems.Add(new ComboBoxItem
                {
                    Tag = cl.Id,
                    Content = cl.Name,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalContentAlignment = VerticalAlignment.Center,
                });
            }
            cbLang.ItemsSource = langItems;
            cbLang.SelectedIndex = 0;
        }

        private void Submit_Solution(object sender, RoutedEventArgs e)
        {
            ComboBoxItem typeItem = (ComboBoxItem)cbLang.SelectedItem;
            int langId = (int)typeItem.Tag;
            string sourceCode = tbSourceCode.Text;

            SubmissionOperations ops = new SubmissionOperations();
            Submission submission = ops.PostSubmission(sourceCode, langId, Id);
            if (submission == null)
            {
                MessageBox.Show("Send submission request failed");
                NavigationService.Navigate(new LoginPage(MainWindowVm));
            }
            else
            {
                NavigationService.Navigate(new SubmissionListPage(MainWindowVm, submission.Id));
            }
        }

        private void Btn_Edit_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TaskFormPage(MainWindowVm, Id));
        }
    }
}
