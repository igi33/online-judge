using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJudgeApi.Dtos
{
    public class TagDto
    {
        public TagDto()
        {
            TaskTags = new HashSet<TaskTagDto>();
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string Abbreviation { get; set; }

        public string Description { get; set; }

        public virtual ICollection<TaskTagDto> TaskTags { get; set; }
    }
}
