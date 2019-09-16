using OnlineJudgeWpfApp.Models;
using OnlineJudgeWpfApp.ViewModels;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using OnlineJudgeWpfApp.Operations;

namespace OnlineJudgeWpfApp.Views
{
    /// <summary>
    /// Interaction logic for TaskFormPage.xaml
    /// </summary>
    public partial class TaskFormPage : Page
    {
        MainWindowViewModel MainWindowVm { get; set; }

        public int Id { get; set; } // If Id > 0, editing task with that ID; else adding new task

        public TaskFormPage(MainWindowViewModel mainWindowVm, int id = 0)
        {
            InitializeComponent();

            MainWindowVm = mainWindowVm;
            Id = id;
        }

        private bool IsAddPage()
        {
            return Id == 0;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsAddPage())
            {
                tbkHeading.Text += " - Create Task";
                btnSubmit.Content = "Add Task";
            }
            else
            {
                tbkHeading.Text += " - Edit Task";
                btnSubmit.Content = "Edit Task";
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^1-9]\\d*");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Handles creation of a new task or edit of existing one
        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            // Populate Task model
            Task task = new Task
            {
                Name = tbName.Text,
                Description = tbDesc.Text,
                TimeLimit = int.Parse(tbTimelimit.Text),
                Origin = tbOrigin.Text,
            };

            // Attach ID if edit mode
            if (!IsAddPage())
            {
                task.Id = Id;
            }

            // Explode tags string and populate array of Tag models
            List<Tag> tags = new List<Tag>();

            if (!string.IsNullOrEmpty(tbTags.Text))
            {
                string[] parts = tbTags.Text.Split(',');
                foreach (string part in parts)
                {
                    string tagName = part.Trim();
                    if (!string.IsNullOrEmpty(tagName))
                    {
                        tags.Add(new Tag
                        {
                            Name = tagName,
                        });
                    }
                }
            }

            task.Tags = tags;

            // Gather test case data and populate array of TestCase models
            List<TestCase> testCases = new List<TestCase>();

            int numRows = gridTcs.RowDefinitions.Count;
            for (int i = 1; i < numRows; ++i) // Skip first row of Grid
            {
                TextBox tbInput = (TextBox)gridTcs.Children.Cast<UIElement>().First(el => Grid.GetRow(el) == i && Grid.GetColumn(el) == 0);
                string input = tbInput.Text.Trim();

                TextBox tbOutput = (TextBox)gridTcs.Children.Cast<UIElement>().First(el => Grid.GetRow(el) == i && Grid.GetColumn(el) == 1);
                string output = tbOutput.Text.Trim();

                if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(output))
                {
                    testCases.Add(new TestCase
                    {
                        Input = string.Format("{0}\n", input),
                        Output = string.Format("{0}\n", output),
                    });
                }
            }

            task.TestCases = testCases;

            // Send request
            TaskOperations ops = new TaskOperations();

            if (IsAddPage())
            {
                Task t = ops.PostTask(task);
                NavigationService.Navigate(new TaskDetailsPage(MainWindowVm, t.Id));
            }
            else
            {
                if (ops.PutTask(task, Id))
                {
                    NavigationService.Navigate(new TaskDetailsPage(MainWindowVm, Id));
                }
                else
                {
                    MessageBox.Show("There's been an error editing the task");
                }
            }
        }

        private void Btn_Back_Click(object sender, RoutedEventArgs e)
        {
            if (IsAddPage())
            {
                NavigationService.Navigate(new TaskListPage(MainWindowVm));
            }
            else
            {
                NavigationService.Navigate(new TaskDetailsPage(MainWindowVm, Id));
            }
        }

        private void TbTcNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Handle the number of grid rows for the test cases in the UI

            if (gridTcs != null && gridTcs.IsLoaded)
            {
                int oldRowCount = gridTcs.RowDefinitions.Count;
                int oldTcCount = oldRowCount - 1;

                TextBox tb = sender as TextBox;
                int newTcCount = int.Parse(tb.Text); // Text is guaranteed to be parsable

                if (newTcCount != oldTcCount)
                {
                    if (newTcCount > oldTcCount)
                    {
                        // Adding rows

                        int rowsToAdd = newTcCount - oldTcCount;

                        for (int i = 0; i < rowsToAdd; ++i)
                        {
                            // Add RowDefinition
                            gridTcs.RowDefinitions.Add(new RowDefinition
                            {
                                Height = GridLength.Auto,
                            });

                            // Add TC input and output TextBoxes

                            TextBox tcInput = new TextBox
                            {
                                Margin = new Thickness(5),
                                Padding = new Thickness(5),
                                AcceptsReturn = true,
                                AcceptsTab = true,
                                TextWrapping = TextWrapping.Wrap,
                                Height = 100,
                                Text = "",
                            };

                            gridTcs.Children.Add(tcInput);
                            Grid.SetRow(tcInput, gridTcs.RowDefinitions.Count - 1);
                            Grid.SetColumn(tcInput, 0);

                            TextBox tcOutput = new TextBox
                            {
                                Margin = new Thickness(5),
                                Padding = new Thickness(5),
                                AcceptsReturn = true,
                                AcceptsTab = true,
                                TextWrapping = TextWrapping.Wrap,
                                Height = 100,
                                Text = "",
                            };

                            gridTcs.Children.Add(tcOutput);
                            Grid.SetRow(tcOutput, gridTcs.RowDefinitions.Count - 1);
                            Grid.SetColumn(tcOutput, 1);
                        }
                    }
                    else
                    {
                        // Removing rows

                        int rowsToRemove = oldTcCount - newTcCount;

                        gridTcs.RowDefinitions.RemoveRange(gridTcs.RowDefinitions.Count - rowsToRemove, rowsToRemove);
                        gridTcs.Children.RemoveRange(gridTcs.Children.Count - rowsToRemove, rowsToRemove);
                    }
                }
            }
        }
    }
}
