using OnlineJudgeWpfApp.Models;
using OnlineJudgeWpfApp.Operations;
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

namespace OnlineJudgeWpfApp.Pages
{
    /// <summary>
    /// Interaction logic for RegisterPage.xaml
    /// </summary>
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
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

            ApiOperations ops = new ApiOperations();
            User user = ops.RegisterUser(username, password, email);
            if (user == null)
            {
                MessageBox.Show("Registration failed");
                return;
            }

            MessageBox.Show("Registration successful");
            NavigationService.Navigate(new LoginPage());
        }

        /**
         * Method to handle going back to the previous screen
         * @param object  sender
         * @param RoutedEventArgs e
         */
        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
