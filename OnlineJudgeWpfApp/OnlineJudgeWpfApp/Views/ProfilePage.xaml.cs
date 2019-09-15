using OnlineJudgeWpfApp.Models;
using OnlineJudgeWpfApp.Operations;
using OnlineJudgeWpfApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace OnlineJudgeWpfApp.Views
{
    /// <summary>
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page
    {
        MainWindowViewModel MainWindowVm { get; set; }

        public int Id { get; set; }

        public ProfilePage(MainWindowViewModel mainWindowVm, int id)
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
        private void profilePage_Loaded(object sender, RoutedEventArgs e)
        {
            ShowUserDetails();
            ShowCompletedTasks();
        }

        private void ShowCompletedTasks()
        {
            TaskOperations ops = new TaskOperations();
            List<Models.Task> completedTasks = ops.GetSolvedTasksByUser(Id);
            if (completedTasks == null)
            {
                MessageBox.Show("Completed tasks request failed");
                NavigationService.Navigate(new LoginPage(MainWindowVm));
            }
            else
            {
                if (completedTasks.Count > 0)
                {
                    foreach (Models.Task t in completedTasks)
                    {
                        Button b = new Button
                        {
                            Content = t.Name,
                            Tag = t.Id,
                            Margin = new Thickness(0, 0, 5, 0),
                            Padding = new Thickness(5),
                            BorderThickness = new Thickness(0),
                            Background = Brushes.Transparent,
                            Cursor = Cursors.Hand,
                        };
                        b.Click += GoToTask_tb_Click;
                        spCompletedTasks.Children.Add(b);
                    }
                }
                else
                {
                    spCompletedTasks.Children.Add(new TextBlock
                    {
                        Text = "No completed tasks",
                    });
                }

            }
        }

        private void GoToTask_tb_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int id = int.Parse(button.Tag.ToString());
            NavigationService.Navigate(new TaskDetailsPage(MainWindowVm, id));
        }

        /**
         * Fetch User Details
         */
        private void ShowUserDetails()
        {
            UserOperations ops = new UserOperations();
            User user = ops.GetUserDetails(Id);
            if (user == null)
            {
                MessageBox.Show("User details request failed");
                NavigationService.Navigate(new LoginPage(MainWindowVm));
            }
            else
            {
                tbkId.Text = user.Id.ToString();
                tbkUsername.Text = user.Username;
                tbkEmail.Text = user.Email;
                tbkTimereg.Text = user.TimeRegistered.ToString();
            }
        }
    }
}
