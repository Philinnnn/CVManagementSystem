namespace CVManagementSystem.Services.Localization;

public interface IAppLocalizer
{
    string this[string key] { get; }
}