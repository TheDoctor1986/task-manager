namespace TaskManagerApi.Validators
{
    using FluentValidation;
    using TaskManagerApi.Dtos;

    public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
    {
        public CreateTaskDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Görev başlığı boş olamaz")
                .MaximumLength(100).WithMessage("Max 100 karakter olabilir");
        }
    }
}
