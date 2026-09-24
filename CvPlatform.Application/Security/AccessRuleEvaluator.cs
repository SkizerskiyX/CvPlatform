using System.Globalization;
using CvPlatform.Application.Common;
using CvPlatform.Application.DTOs;
using CvPlatform.Domain.Entities;
using CvPlatform.Domain.Enums;

namespace CvPlatform.Application.Security;

/// <summary>
/// A position is either public (accessible to every authenticated user) or restricted:
/// then the candidate must satisfy ALL access rules. A restricted position without rules is closed.
/// </summary>
public static class AccessRuleEvaluator
{
    public static bool HasAccess(
        bool isPublic,
        IEnumerable<AccessRule> rules,
        IReadOnlyDictionary<Guid, AttributeDefinition> definitions,
        Func<AttributeDefinition, AttributeValueInput> valueOf)
    {
        if (isPublic)
        {
            return true;
        }

        var any = false;
        foreach (var rule in rules)
        {
            any = true;
            if (!definitions.TryGetValue(rule.AttributeDefinitionId, out var definition))
            {
                return false;
            }

            if (!Matches(definition, valueOf(definition), rule.Operator, rule.ComparisonValue))
            {
                return false;
            }
        }

        return any;
    }

    public static bool Matches(AttributeDefinition definition, AttributeValueInput value, ComparisonOperator op, string comparisonValue)
    {
        if (AttributeValues.IsEmpty(definition.DataType, value))
        {
            return false;
        }

        switch (definition.DataType)
        {
            case AttributeDataType.Numeric:
                if (!decimal.TryParse(comparisonValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                {
                    return false;
                }

                return Compare(value.NumericValue!.Value.CompareTo(number), op);

            case AttributeDataType.Date:
            case AttributeDataType.Period:
                if (!DateTime.TryParse(comparisonValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date))
                {
                    return false;
                }

                var actual = definition.DataType == AttributeDataType.Date ? value.DateValue!.Value : value.PeriodStart!.Value;
                return Compare(actual.Date.CompareTo(date.Date), op);

            case AttributeDataType.Boolean:
                return bool.TryParse(comparisonValue, out var expected) && op == ComparisonOperator.EqualTo && value.BoolValue == expected;

            case AttributeDataType.Dropdown:
                var selected = value.SelectedOptionId!.Value.ToString();
                var optionIds = Split(comparisonValue);
                return op switch
                {
                    ComparisonOperator.EqualTo => optionIds.Length == 1 && string.Equals(optionIds[0], selected, StringComparison.OrdinalIgnoreCase),
                    ComparisonOperator.NotEquals => !optionIds.Contains(selected, StringComparer.OrdinalIgnoreCase),
                    ComparisonOperator.In => optionIds.Contains(selected, StringComparer.OrdinalIgnoreCase),
                    _ => false
                };

            case AttributeDataType.String:
            case AttributeDataType.Text:
                var text = value.StringValue!.Trim();
                return op switch
                {
                    ComparisonOperator.EqualTo => string.Equals(text, comparisonValue.Trim(), StringComparison.OrdinalIgnoreCase),
                    ComparisonOperator.NotEquals => !string.Equals(text, comparisonValue.Trim(), StringComparison.OrdinalIgnoreCase),
                    ComparisonOperator.In => Split(comparisonValue).Contains(text, StringComparer.OrdinalIgnoreCase),
                    _ => false
                };

            default:
                return false;
        }
    }

    /// <summary>Human-readable comparison value (dropdown option ids are resolved to option names).</summary>
    public static string DisplayValue(AttributeDefinition definition, string comparisonValue) =>
        definition.DataType == AttributeDataType.Dropdown
            ? string.Join(", ", Split(comparisonValue).Select(id => definition.Options.FirstOrDefault(o => o.Id.ToString() == id)?.Value ?? id))
            : comparisonValue;

    private static bool Compare(int comparison, ComparisonOperator op) => op switch
    {
        ComparisonOperator.EqualTo => comparison == 0,
        ComparisonOperator.NotEquals => comparison != 0,
        ComparisonOperator.GreaterThan => comparison > 0,
        ComparisonOperator.GreaterOrEqual => comparison >= 0,
        ComparisonOperator.LessThan => comparison < 0,
        ComparisonOperator.LessOrEqual => comparison <= 0,
        _ => false
    };

    private static string[] Split(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
