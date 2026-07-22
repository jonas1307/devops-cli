using DevOps.Options;
using FluentValidation;

namespace DevOps.Validators;

public class CreateValidator : AbstractValidator<CreateOptions>
{
    public CreateValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("--type cannot be empty.");

        When(x => x.Priority.HasValue, () =>
        {
            RuleFor(x => x.Priority!.Value)
                .InclusiveBetween(1, 4).WithMessage("--priority must be between 1 and 4.");
        });

        When(x => x.Estimate.HasValue, () =>
        {
            RuleFor(x => x.Estimate!.Value)
                .GreaterThan(0).WithMessage("--estimate must be greater than 0.");
        });

        RuleForEach(x => x.Fields)
            .Must(f => f.Contains('=') && f.IndexOf('=') > 0)
            .WithMessage("--field values must be in Key=Value format (e.g., Custom.MyField=value).");
    }
}
