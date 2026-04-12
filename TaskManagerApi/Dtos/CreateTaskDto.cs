namespace TaskManagerApi.Dtos
{
    using System.ComponentModel.DataAnnotations;

public class CreateTaskDto
{
    [Required(ErrorMessage = "Title zorunludur")]
    [MinLength(3, ErrorMessage = "En az 3 karakter olmalı")]
    [MaxLength(100, ErrorMessage = "En fazla 100 karakter olabilir")]
    public string Title { get; set; }
}
}
