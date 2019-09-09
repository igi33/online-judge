using OnlineJudgeWpfApp.Models;
using OnlineJudgeWpfApp.Operations;
using OnlineJudgeWpfApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
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
                MessageBox.Show("Session expired");
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
