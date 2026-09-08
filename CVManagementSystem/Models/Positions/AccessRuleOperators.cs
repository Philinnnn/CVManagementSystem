namespace CVManagementSystem.Models.Positions;

public static class AccessRuleOperators
{
    public const string GreaterThan = ">";
    public const string LessThan = "<";
    public const string GreaterOrEqual = ">=";
    public const string LessOrEqual = "<=";
    public const string Equal = "==";
    public const string NotEqual = "!=";

    public static readonly IReadOnlyCollection<string> All =
    [
        GreaterThan, LessThan, GreaterOrEqual, LessOrEqual, Equal, NotEqual
    ];

    public static bool IsValid(string op) => All.Contains(op);
}