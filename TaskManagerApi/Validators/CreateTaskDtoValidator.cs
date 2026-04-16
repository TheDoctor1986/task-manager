namespace TaskManagerApi.Validators
{
    using FluentValidation;
    using TaskManagerApi.Dtos;

    public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
    {
        public CreateTaskDtoValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title cannot be empty ")
                .MaximumLength(100).WithMessage("Max 100 characters");
        }
    }
}
