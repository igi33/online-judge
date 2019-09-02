using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJudgeApi.Dtos
{
    public class SubmissionDto
    {
        public int Id { get; set; }

        public DateTime TimeSubmitted { get; set; }

        public string SourceCode { get; set; }

        public string Status { get; set; }

        public int ExecutionTime { get; set; }

        public int ExecutionMemory { get; set; }

        public virtual UserDto User { get; set; }

        public virtual TaskDto Task { get; set; }

        public virtual ComputerLanguageDto ComputerLanguage { get; set; }
    }
}
