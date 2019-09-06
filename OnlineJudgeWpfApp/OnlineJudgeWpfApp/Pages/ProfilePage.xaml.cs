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
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page
    {
        public int Id { get; set; }

        public ProfilePage(int id)
        {
            Id = id;
            InitializeComponent();
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
            ApiOperations ops = new ApiOperations();
            User user = ops.GetUserDetails(Id);
            if (user == null)
            {
                MessageBox.Show("Session expired");
                // Navigate back to login page
                NavigationService.Navigate(new LoginPage());
            }
            else
            {
                tbkId.Text = user.Id.ToString();
                tbkUsername.Text = user.Username;
                tbkEmail.Text = user.Email;
                tbkTimereg.Text = user.TimeRegistered.ToString();
            }
        }

        /**
         * Logout Method to be called on the logout Button
         * @param  object sender
         * @param  RoutedEventArgs e
         */
        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            Globals.LoggedInUser = null;
            NavigationService.Navigate(new LoginPage());
        }
    }
}
