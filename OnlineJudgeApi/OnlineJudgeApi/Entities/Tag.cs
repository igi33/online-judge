using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJudgeApi.Entities
{
    public class Tag
    {
        public Tag()
        {
            TaskTags = new HashSet<TaskTag>();
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public virtual ICollection<TaskTag> TaskTags { get; set; }
    }
}
