namespace CVManagementSystem.Services.Common;

public static class DateTimeExtensions
{
    public static DateTime? AsUtc(this DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
}