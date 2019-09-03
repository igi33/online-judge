using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJudgeApi.Entities
{
    public class Submission
    {
        public int Id { get; set; }

        public DateTime TimeSubmitted { get; set; }

        public string SourceCode { get; set; }

        public string Status { get; set; }

        public int ExecutionTime { get; set; }

        public int ExecutionMemory { get; set; }

        public int? UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public int? TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; }

        public int? LangId { get; set; }

        [ForeignKey("LangId")]
        public virtual ComputerLanguage ComputerLanguage { get; set; }
    }
}
