using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineJudgeApi.Entities
{
    [Table("tasks")]
    public class Task
    {
        public Task()
        {
            TestCases = new HashSet<TestCase>();
            TaskTags = new HashSet<TaskTag>();
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int MemoryLimit { get; set; }

        public int TimeLimit { get; set; }

        public DateTime TimeSubmitted { get; set; }

        public string Origin { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public virtual ICollection<TestCase> TestCases { get; set; }

        public virtual ICollection<TaskTag> TaskTags { get; set; }
    }
}
