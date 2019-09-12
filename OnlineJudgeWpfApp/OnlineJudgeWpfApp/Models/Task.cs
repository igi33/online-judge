using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineJudgeWpfApp.Models
{
    class Task
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int MemoryLimit { get; set; }

        public int TimeLimit { get; set; }

        public DateTime TimeSubmitted { get; set; }
        
        public string Origin { get; set; }

        public User User { get; set; }

        public List<TestCase> TestCases { get; set; }

        public List<Tag> Tags { get; set; }
    }
}
