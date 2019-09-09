using OnlineJudgeWpfApp.Models;
using OnlineJudgeWpfApp.Operations;
using OnlineJudgeWpfApp.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace OnlineJudgeWpfApp.Views
{
    /// <summary>
    /// Interaction logic for RegisterPage.xaml
    /// </summary>
    public partial class RegisterPage : Page
    {
        MainWindowViewModel MainWindowVm { get; set; }

        public RegisterPage(MainWindowViewModel mainWindowVm)
        {
            InitializeComponent();

            MainWindowVm = mainWindowVm;
        }

        /**
         * Register method to handle the Register Button
         * @param object sender
         * @param RoutedEventArgs e
         */
        private void btnReg_Click(object sender, RoutedEventArgs e)
        {
            string username = tbxUsername.Text;
            string password = pbxPassword.Password;
            string email = tbxEmail.Text;

            UserOperations ops = new UserOperations();
            User user = ops.RegisterUser(username, password, email);
            if (user == null)
            {
                MessageBox.Show("Registration failed");
                return;
            }

            MessageBox.Show("Registration successful");
            NavigationService.Navigate(new LoginPage(MainWindowVm));
        }
    }
}
