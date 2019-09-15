using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineJudgeWpfApp.Models
{
    class Submission
    {
        public int Id { get; set; }

        public DateTime TimeSubmitted { get; set; }

        public string SourceCode { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }

        public int ExecutionTime { get; set; }

        public int ExecutionMemory { get; set; }

        public User User { get; set; }

        public Task Task { get; set; }

        public ComputerLanguage ComputerLanguage { get; set; }

        public bool Selected { get; set; } // Used to color selected row in DataGrid
    }
}
