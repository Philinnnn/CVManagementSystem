namespace CVManagementSystem.Models.Attributes;

public static class AttributeDataTypes
{
    public const string String = "String";
    public const string Text = "Text";
    public const string Image = "Image";
    public const string Numeric = "Numeric";
    public const string Date = "Date";
    public const string Period = "Period";
    public const string Boolean = "Boolean";
    public const string Select = "Select";

    public static readonly IReadOnlyCollection<string> All =
    [
        String, Text, Image, Numeric, Date, Period, Boolean, Select
    ];

    public static bool IsValid(string dataType) => All.Contains(dataType);
}