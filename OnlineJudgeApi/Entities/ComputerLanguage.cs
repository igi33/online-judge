using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineJudgeApi.Entities
{
    [Table("computerlanguages")]
    public class ComputerLanguage
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Extension { get; set; }

        public string CompilerPath { get; set; }

        public string CompilerFileName { get; set; }

        public string CompileCmd { get; set; }

        public string ExecuteCmd { get; set; }
    }
}
