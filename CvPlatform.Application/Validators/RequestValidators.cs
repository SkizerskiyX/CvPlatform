using CvPlatform.Application.DTOs;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Enums;
using FluentValidation;

namespace CvPlatform.Application.Validators;

public sealed class SaveAttributeDefinitionRequestValidator : AbstractValidator<SaveAttributeDefinitionRequest>
{
    public SaveAttributeDefinitionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DataType).IsInEnum();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.DropdownOptions)
            .Must(options => options is not null && options.Any(o => !string.IsNullOrWhiteSpace(o)))
            .When(x => x.DataType == AttributeDataType.Dropdown)
            .WithMessage("Dropdown attribute requires at least one option.");
        RuleForEach(x => x.DropdownOptions).MaximumLength(200);
    }
}

public sealed class SavePositionRequestValidator : AbstractValidator<SavePositionRequest>
{
    public SavePositionRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ShortDescription).MaximumLength(2000);
        RuleFor(x => x.Company).MaximumLength(200);
        RuleFor(x => x.Level).IsInEnum().When(x => x.Level.HasValue);
        RuleFor(x => x.MaxProjects).InclusiveBetween(0, Position.MaxProjectsLimit);
        RuleFor(x => x.AttributeIds).Must(ids => ids is null || ids.Count <= 100);
        RuleFor(x => x.AccessRules).Must(rules => rules is null || rules.Count <= 30);
        RuleForEach(x => x.AccessRules).ChildRules(rule =>
        {
            rule.RuleFor(x => x.AttributeDefinitionId).NotEmpty();
            rule.RuleFor(x => x.Operator).IsInEnum();
            rule.RuleFor(x => x.ComparisonValue).NotEmpty().MaximumLength(1000);
        });
    }
}

public sealed class SaveProjectRequestValidator : AbstractValidator<SaveProjectRequest>
{
    public SaveProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(8000);
        RuleFor(x => x.PeriodEnd).GreaterThanOrEqualTo(x => x.PeriodStart).When(x => x.PeriodEnd.HasValue);
        RuleFor(x => x.Tags).Must(tags => tags is null || tags.Count <= 30);
    }
}

public sealed class ProfileAutosaveRequestValidator : AbstractValidator<ProfileAutosaveRequest>
{
    public ProfileAutosaveRequestValidator()
    {
        RuleFor(x => x.Changes).Must(changes => changes is null || changes.Count <= 200);
        RuleForEach(x => x.Changes).ChildRules(change =>
        {
            change.RuleFor(x => x.AttributeDefinitionId).NotEmpty();
            change.RuleFor(x => x.Value).NotNull();
        });
    }
}

public sealed class CreateDiscussionPostRequestValidator : AbstractValidator<CreateDiscussionPostRequest>
{
    public CreateDiscussionPostRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
    }
}
