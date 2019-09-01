using System;
using System.Collections.Generic;

namespace OnlineJudgeApi.Dtos
{
    public class TaskDto
    {
        public TaskDto()
        {
            TestCases = new HashSet<TestCaseDto>();
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MemoryLimit { get; set; }
        public int TimeLimit { get; set; }
        public DateTime TimeSubmitted { get; set; }
        public string Origin { get; set; }
        public virtual UserDto User { get; set; }
        public virtual ICollection<TestCaseDto> TestCases { get; set; }
    }
}
