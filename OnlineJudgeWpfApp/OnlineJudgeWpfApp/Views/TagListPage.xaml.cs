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
    /// Interaction logic for TagListPage.xaml
    /// </summary>
    public partial class TagListPage : Page
    {
        public MainWindowViewModel MainWindowVm { get; set; }

        public TagListPage(MainWindowViewModel mainWindowVm)
        {
            InitializeComponent();

            MainWindowVm = mainWindowVm;
        }

        private void tagListPage_Loaded(object sender, RoutedEventArgs e)
        {
            ShowTags();
        }

        private void ShowTags()
        {
            TagOperations ops = new TagOperations();
            List<Tag> tags = ops.GetTags();
            if (tags == null)
            {
                MessageBox.Show("Tags request failed");
            }
            else
            {
                // Add tag buttons to stackpanel
                foreach (Tag t in tags)
                {
                    Button b = new Button
                    {
                        Content = t.Name,
                        Tag = t.Id,
                        Padding = new Thickness(5, 5, 5, 5),
                        Margin = new Thickness(0, 0, 5, 0),
                        Height = 40
                    };
                    b.Click += GoToTaggedTasks_tb_Click;
                    spTagList.Children.Add(b);
                }
            }
        }

        private void GoToTaggedTasks_tb_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            int id = int.Parse(button.Tag.ToString());
            NavigationService.Navigate(new TaskListPage(MainWindowVm, id));
        }
    }
}
