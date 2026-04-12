using System.ComponentModel.DataAnnotations;

namespace TaskManagerApi.Dtos
{
    public class UpdateTaskDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MinLength(3)]
        public string Title { get; set; }

        public bool IsDone { get; set; }
    }
}
