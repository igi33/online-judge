﻿using OnlineJudgeWpfApp.Models;
using OnlineJudgeWpfApp.ViewModels;
using OnlineJudgeWpfApp.Views;
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

namespace OnlineJudgeWpfApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowViewModel MainWindowVm { get { return DataContext as MainWindowViewModel; } }

        public MainWindow()
        {
            InitializeComponent();
            frame.NavigationService.Navigate(new TaskListPage(MainWindowVm));
        }

        private void Nav_Login_Click(object sender, RoutedEventArgs e)
        {
            frame.NavigationService.Navigate(new LoginPage(MainWindowVm));
        }

        private void Nav_Reg_Click(object sender, RoutedEventArgs e)
        {
            frame.NavigationService.Navigate(new RegisterPage(MainWindowVm));
        }

        private void Nav_Profile_Click(object sender, RoutedEventArgs e)
        {
            frame.NavigationService.Navigate(new ProfilePage(MainWindowVm, Globals.LoggedInUser.Id));
        }

        /**
         * Logout Method to be called on the logout Button
         * @param  object sender
         * @param  RoutedEventArgs e
         */
        private void Nav_Logout_Click(object sender, RoutedEventArgs e)
        {
            Globals.LoggedInUser = null;
            MainWindowVm.LoggedIn = false;
            MainWindowVm.Username = "guest";
            frame.NavigationService.Navigate(new LoginPage(MainWindowVm));
        }

        private void Nav_Tasks_Click(object sender, RoutedEventArgs e)
        {
            frame.NavigationService.Navigate(new TaskListPage(MainWindowVm));
        }

        private void Nav_Tags_Click(object sender, RoutedEventArgs e)
        {
            frame.NavigationService.Navigate(new TagListPage(MainWindowVm));
        }

        private void Nav_Submissions_Click(object sender, RoutedEventArgs e)
        {
            frame.NavigationService.Navigate(new SubmissionListPage(MainWindowVm));
        }

        // Methods to ensure that Pages inherit the DataContext of frame
        private void frame_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateFrameDataContext(sender);
        }

        private void frame_LoadCompleted(object sender, NavigationEventArgs e)
        {
            UpdateFrameDataContext(sender);
        }

        private void UpdateFrameDataContext(object sender)
        {
            FrameworkElement content = frame.Content as FrameworkElement;
            if (content == null)
            {
                return;
            }
            content.DataContext = frame.DataContext;
        }
    }
}