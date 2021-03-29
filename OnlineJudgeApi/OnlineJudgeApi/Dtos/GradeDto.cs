using System;

namespace OnlineJudgeApi.Dtos
{
    public class GradeDto
    {
        public string Status { get; set; }

        public int ExecutionTime { get; set; }

        public int ExecutionMemory { get; set; }

        public string Error { get; set; }

        public string Output { get; set; }

        public DateTime SubmittedAt { get; set; }
    }
}
