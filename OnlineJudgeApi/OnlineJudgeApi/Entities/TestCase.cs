using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineJudgeApi.Entities
{
    [Table("testcases")]
    public class TestCase
    {
        public int Id { get; set; }

        public string Input { get; set; }

        public string Output { get; set; }

        public int TaskId { get; set; }

        [ForeignKey("TaskId")]
        public virtual Task Task { get; set; }
    }
}
