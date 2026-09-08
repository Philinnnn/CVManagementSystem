namespace CVManagementSystem.Services.Positions;

using Models.Attributes;
using Models.Candidates;
using Models.Positions;

public static class PositionAccessEvaluator
{
    public static bool CandidateMatchesRule(PositionAccessRule rule, CandidateAttributeValue? value)
    {
        if (value is null)
            return false;

        return rule.Attribute.DataType switch
        {
            AttributeDataTypes.Numeric => CompareNumeric(value.NumericValue, rule.ExpectedValue, rule.Operator),
            AttributeDataTypes.Date => CompareDate(value.DateValue, rule.ExpectedValue, rule.Operator),
            AttributeDataTypes.Boolean => CompareBoolean(value.BooleanValue, rule.ExpectedValue, rule.Operator),
            AttributeDataTypes.Select => CompareText(value.TextValue, rule.ExpectedValue, rule.Operator),
            AttributeDataTypes.String => CompareText(value.TextValue, rule.ExpectedValue, rule.Operator),
            _ => false
        };
    }

    private static bool CompareNumeric(double? actual, string expectedRaw, string op)
    {
        if (actual is null || !double.TryParse(expectedRaw, out var expected))
            return false;

        return op switch
        {
            AccessRuleOperators.GreaterThan => actual > expected,
            AccessRuleOperators.LessThan => actual < expected,
            AccessRuleOperators.GreaterOrEqual => actual >= expected,
            AccessRuleOperators.LessOrEqual => actual <= expected,
            AccessRuleOperators.Equal => Math.Abs(actual.Value - expected) < 0.0001,
            AccessRuleOperators.NotEqual => Math.Abs(actual.Value - expected) >= 0.0001,
            _ => false
        };
    }

    private static bool CompareDate(DateTime? actual, string expectedRaw, string op)
    {
        if (actual is null || !DateTime.TryParse(expectedRaw, out var expected))
            return false;

        return op switch
        {
            AccessRuleOperators.GreaterThan => actual > expected,
            AccessRuleOperators.LessThan => actual < expected,
            AccessRuleOperators.GreaterOrEqual => actual >= expected,
            AccessRuleOperators.LessOrEqual => actual <= expected,
            AccessRuleOperators.Equal => actual.Value.Date == expected.Date,
            AccessRuleOperators.NotEqual => actual.Value.Date != expected.Date,
            _ => false
        };
    }

    private static bool CompareBoolean(bool? actual, string expectedRaw, string op)
    {
        if (actual is null || !bool.TryParse(expectedRaw, out var expected))
            return false;

        return op switch
        {
            AccessRuleOperators.Equal => actual == expected,
            AccessRuleOperators.NotEqual => actual != expected,
            _ => false
        };
    }

    private static bool CompareText(string? actual, string expected, string op)
    {
        if (actual is null)
            return false;

        return op switch
        {
            AccessRuleOperators.Equal => actual == expected,
            AccessRuleOperators.NotEqual => actual != expected,
            _ => false
        };
    }
}