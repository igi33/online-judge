using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OnlineJudgeApi.Dtos
{
    public class ComputerLanguageDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CompilerPath { get; set; }
        public string CompileCmd { get; set; }
        public string ExecuteCmd { get; set; }
    }
}
