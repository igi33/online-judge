using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJudgeApi.Entities
{
    public class TaskTag
    {
        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; }

        public int TagId { get; set; }

        [ForeignKey("TagId")]
        public virtual Tag Tag { get; set; }
    }
}
