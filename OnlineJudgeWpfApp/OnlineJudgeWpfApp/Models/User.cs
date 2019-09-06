using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineJudgeWpfApp.Models
{
    class User
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string Email { get; set; }

        public DateTime TimeRegistered { get; set; }

        public string Token { get; set; }
    }
}
