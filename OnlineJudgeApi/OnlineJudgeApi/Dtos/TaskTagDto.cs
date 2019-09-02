using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJudgeApi.Dtos
{
    public class TaskTagDto
    {
        public virtual TaskDto Task { get; set; }

        public virtual TagDto Tag { get; set; }
    }
}
