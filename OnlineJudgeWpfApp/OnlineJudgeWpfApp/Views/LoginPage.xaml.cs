using OnlineJudgeWpfApp.Models;
using OnlineJudgeWpfApp.Operations;
using OnlineJudgeWpfApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace OnlineJudgeWpfApp.Views
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        MainWindowViewModel MainWindowVm { get; set; }

        public LoginPage(MainWindowViewModel mainWindowVm)
        {
            InitializeComponent();

            MainWindowVm = mainWindowVm;
        }

        /**
         * Login Method to handle Login Button
         * @param  object sender
         * @param  RoutedEventArgs e
         */
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = tbxUsername.Text;
            string password = pbxPassword.Password;

            UserOperations ops = new UserOperations();
            User user = ops.AuthenticateUser(username, password);
            if (user == null)
            {
                MessageBox.Show("Invalid username or password");
            }
            else
            {
                Globals.LoggedInUser = user;
                MainWindowVm.LoggedIn = true;
                MainWindowVm.Username = user.Username;
                NavigationService.Navigate(new ProfilePage(MainWindowVm, user.Id));
            }
        }
    }
}
