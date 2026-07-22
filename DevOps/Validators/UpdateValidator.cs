using DevOps.Options;
using FluentValidation;

namespace DevOps.Validators;

public class UpdateValidator : AbstractValidator<UpdateOptions>
{
    public UpdateValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("--id must be a positive integer.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.Title) || !string.IsNullOrEmpty(x.State) ||
                       !string.IsNullOrEmpty(x.AssignedTo) || !string.IsNullOrEmpty(x.Description) ||
                       !string.IsNullOrEmpty(x.Iteration) || !string.IsNullOrEmpty(x.Area) ||
                       !string.IsNullOrEmpty(x.Comment) || x.Priority.HasValue ||
                       x.Estimate.HasValue || x.RelatedId.HasValue ||
                       (x.Fields != null && x.Fields.Any()))
            .WithMessage("Provide at least one field to update.");

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
