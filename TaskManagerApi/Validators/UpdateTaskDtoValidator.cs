namespace TaskManagerApi.Validators
{
    using FluentValidation;
    using TaskManagerApi.Dtos;

    public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
    {
        public UpdateTaskDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Görev başlığı boş olamaz")
                .MaximumLength(100);
        }
    }
}
