using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OnlineJudgeWpfApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private bool loggedIn;

        public bool LoggedIn
        {
            get
            {
                return loggedIn;
            }
            set
            {
                if (value != loggedIn)
                {
                    loggedIn = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private string username;

        public string Username
        {
            get
            {
                return username;
            }
            set
            {
                if (!value.Equals(username))
                {
                    username = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel()
        {
            LoggedIn = false;
            Username = "guest";
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
