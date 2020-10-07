using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJudgeApi.Entities
{
    public class Grade
    {
        public string Status { get; set; }
        public int ExecutionTime { get; set; }
        public int ExecutionMemory { get; set; }
    }
}
