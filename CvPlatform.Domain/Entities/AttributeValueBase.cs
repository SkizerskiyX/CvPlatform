using Ardalis.GuardClauses;

namespace CvPlatform.Domain.Entities;

public abstract class AttributeValueBase : BaseEntity
{
    protected AttributeValueBase()
    {
    }

    protected AttributeValueBase(Guid attributeDefinitionId)
    {
        AttributeDefinitionId = Guard.Against.Default(attributeDefinitionId);
    }

    public Guid AttributeDefinitionId { get; private set; }
    public string? StringValue { get; private set; }
    public decimal? NumericValue { get; private set; } = null!;
    public DateTime? DateValue { get; private set; } = null!;
    public DateTime? PeriodStart { get; private set; } = null!;
    public DateTime? PeriodEnd { get; private set; } = null!;
    public bool? BoolValue { get; private set; } = null!;
    public Guid? SelectedOptionId { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public AttributeDefinition? AttributeDefinition { get; private set; }

    public void SetString(string? value)
    {
        Clear();
        StringValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        RefreshUpdatedAt();
    }

    public void SetNumeric(decimal? value)
    {
        Clear();
        NumericValue = value;
        RefreshUpdatedAt();
    }

    public void SetDate(DateTime? value)
    {
        Clear();
        DateValue = value;
        RefreshUpdatedAt();
    }

    public void SetPeriod(DateTime? start, DateTime? end)
    {
        Clear();
        if (start.HasValue && end.HasValue && start > end)
        {
            throw new InvalidOperationException("Period start must not be after period end.");
        }

        PeriodStart = start;
        PeriodEnd = end;
        RefreshUpdatedAt();
    }

    public void SetBoolean(bool? value)
    {
        Clear();
        BoolValue = value;
        RefreshUpdatedAt();
    }

    public void SetSelectedOption(Guid? optionId)
    {
        Clear();
        SelectedOptionId = optionId;
        RefreshUpdatedAt();
    }

    public void SetImage(string? url)
    {
        Clear();
        ImageUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        RefreshUpdatedAt();
    }

    public bool IsEmpty() => StringValue is null
        && NumericValue is null
        && DateValue is null
        && PeriodStart is null
        && PeriodEnd is null
        && BoolValue is null
        && SelectedOptionId is null
        && ImageUrl is null;

    private void Clear()
    {
        StringValue = null;
        NumericValue = null;
        DateValue = null;
        PeriodStart = null;
        PeriodEnd = null;
        BoolValue = null;
        SelectedOptionId = null;
        ImageUrl = null;
    }
}
