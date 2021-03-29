using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJudgeApi.Dtos
{
    public class GradeSubmissionDto
    {
        public int LangId { get; set; }

        public string SourceCode { get; set; }

        public string Input { get; set; }

        public string ExpectedOutput { get; set; }

        public int TimeLimit { get; set; }

        public int MemoryLimit { get; set; }
    }
}
