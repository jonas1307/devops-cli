using DevOps.Options;
using FluentValidation;

namespace DevOps.Validators;

public class ConfigValidator : AbstractValidator<ConfigOptions>
{
    public ConfigValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Show || x.Reset || x.RefreshCache ||
                       !string.IsNullOrEmpty(x.OrgUrl) || !string.IsNullOrEmpty(x.Pat) ||
                       !string.IsNullOrEmpty(x.Project) || !string.IsNullOrEmpty(x.Team) ||
                       !string.IsNullOrEmpty(x.Email))
            .WithMessage("Provide at least one option: --org, --pat, --project, --team, --email, --show, --reset, or --refresh-cache.");

        When(x => !string.IsNullOrEmpty(x.OrgUrl), () =>
        {
            RuleFor(x => x.OrgUrl)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("--org must be a valid URL (e.g., https://dev.azure.com/myorg).");
        });

        When(x => !string.IsNullOrEmpty(x.Pat), () =>
        {
            RuleFor(x => x.Pat)
                .MinimumLength(10)
                .WithMessage("--pat seems too short. Please verify your Personal Access Token.");
        });
    }
}
