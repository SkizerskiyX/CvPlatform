using System.Globalization;
using CvPlatform.Application.DTOs;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Enums;

namespace CvPlatform.Application.Common;

/// <summary>
/// The single attribute "engine": reads and writes values of both built-in ("Me") attributes
/// and library attributes of a profile, and formats them for display.
/// </summary>
public static class AttributeValues
{
    public static AttributeValueInput FromEntity(AttributeValueBase? value) => value is null
        ? AttributeValueInput.Empty
        : new AttributeValueInput(
            value.StringValue,
            value.NumericValue,
            value.DateValue,
            value.PeriodStart,
            value.PeriodEnd,
            value.BoolValue,
            value.SelectedOptionId,
            value.ImageUrl);

    public static AttributeValueInput Read(UserProfile profile, AttributeDefinition definition, IReadOnlyDictionary<Guid, ProfileAttributeValue> values)
    {
        if (definition.SystemKey is { } key)
        {
            var raw = profile.GetBuiltIn(key);
            return definition.DataType == AttributeDataType.Image
                ? AttributeValueInput.Empty with { ImageUrl = raw }
                : AttributeValueInput.Empty with { StringValue = raw };
        }

        return FromEntity(values.GetValueOrDefault(definition.Id));
    }

    public static void Write(UserProfile profile, AttributeDefinition definition, AttributeValueInput input)
    {
        Validate(definition, input);
        if (definition.SystemKey is { } key)
        {
            profile.SetBuiltIn(key, definition.DataType == AttributeDataType.Image ? input.ImageUrl : input.StringValue);
            return;
        }

        var target = profile.GetOrAddAttributeValue(definition.Id);
        switch (definition.DataType)
        {
            case AttributeDataType.String:
            case AttributeDataType.Text:
                target.SetString(input.StringValue);
                break;
            case AttributeDataType.Image:
                target.SetImage(input.ImageUrl);
                break;
            case AttributeDataType.Numeric:
                target.SetNumeric(input.NumericValue);
                break;
            case AttributeDataType.Date:
                target.SetDate(ToUtcDate(input.DateValue));
                break;
            case AttributeDataType.Period:
                target.SetPeriod(ToUtcDate(input.PeriodStart), ToUtcDate(input.PeriodEnd));
                break;
            case AttributeDataType.Boolean:
                target.SetBoolean(input.BoolValue);
                break;
            case AttributeDataType.Dropdown:
                target.SetSelectedOption(input.SelectedOptionId);
                break;
            default:
                throw new InvalidOperationException($"Unsupported data type {definition.DataType}.");
        }
    }

    public static bool IsEmpty(AttributeDataType dataType, AttributeValueInput value) => dataType switch
    {
        AttributeDataType.String or AttributeDataType.Text => string.IsNullOrWhiteSpace(value.StringValue),
        AttributeDataType.Image => string.IsNullOrWhiteSpace(value.ImageUrl),
        AttributeDataType.Numeric => value.NumericValue is null,
        AttributeDataType.Date => value.DateValue is null,
        AttributeDataType.Period => value.PeriodStart is null,
        AttributeDataType.Boolean => value.BoolValue is null,
        AttributeDataType.Dropdown => value.SelectedOptionId is null,
        _ => true
    };

    public static string? Display(AttributeDefinition definition, AttributeValueInput value)
    {
        if (IsEmpty(definition.DataType, value))
        {
            return null;
        }

        return definition.DataType switch
        {
            AttributeDataType.String or AttributeDataType.Text => value.StringValue,
            AttributeDataType.Image => value.ImageUrl,
            AttributeDataType.Numeric => value.NumericValue!.Value.ToString("0.##", CultureInfo.InvariantCulture),
            AttributeDataType.Date => value.DateValue!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            AttributeDataType.Period => $"{value.PeriodStart:yyyy-MM-dd} – {value.PeriodEnd?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "…"}",
            AttributeDataType.Boolean => value.BoolValue == true ? "Yes" : "No",
            AttributeDataType.Dropdown => definition.Options.FirstOrDefault(x => x.Id == value.SelectedOptionId)?.Value,
            _ => null
        };
    }

    public static AttributeValueView View(AttributeDefinition definition, AttributeValueInput value) =>
        new(definition.Id, value, Display(definition, value));

    private static void Validate(AttributeDefinition definition, AttributeValueInput input)
    {
        switch (definition.DataType)
        {
            case AttributeDataType.String when input.StringValue is { Length: > 500 }:
                throw new InvalidOperationException($"'{definition.Name}' must be at most 500 characters.");
            case AttributeDataType.String when input.StringValue is not null && input.StringValue.Contains('\n'):
                throw new InvalidOperationException($"'{definition.Name}' must be a single line.");
            case AttributeDataType.Text when input.StringValue is { Length: > 8000 }:
                throw new InvalidOperationException($"'{definition.Name}' must be at most 8000 characters.");
            case AttributeDataType.Image when !string.IsNullOrWhiteSpace(input.ImageUrl) && !IsExternalUrl(input.ImageUrl):
                // Images must live in external cloud storage; only links are stored.
                throw new InvalidOperationException($"'{definition.Name}' must be an http(s) link to an externally stored image.");
            case AttributeDataType.Period when input.PeriodStart is null && input.PeriodEnd is not null:
                throw new InvalidOperationException($"'{definition.Name}' requires a start date.");
            case AttributeDataType.Dropdown when input.SelectedOptionId is { } optionId && definition.Options.All(x => x.Id != optionId):
                throw new InvalidOperationException($"Unknown option for '{definition.Name}'.");
        }
    }

    private static bool IsExternalUrl(string url) =>
        url.Length <= 1000
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    private static DateTime? ToUtcDate(DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc) : null;
}
