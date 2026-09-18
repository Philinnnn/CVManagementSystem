namespace CVManagementSystem.Services.Localization;

using System.Globalization;
using System.Text.Json;

public class JsonAppLocalizer(IWebHostEnvironment env) : IAppLocalizer
{
    private static readonly Dictionary<string, Dictionary<string, string>> Cache = new();
    private static readonly Lock SyncLock = new();

    public string this[string key]
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var dict = GetDictionary(culture);

            if (dict.TryGetValue(key, out var value))
                return value;

            if (culture != "en")
            {
                var fallback = GetDictionary("en");
                if (fallback.TryGetValue(key, out var fallbackValue))
                    return fallbackValue;
            }

            return key;
        }
    }

    private Dictionary<string, string> GetDictionary(string culture)
    {
        lock (SyncLock)
        {
            if (Cache.TryGetValue(culture, out var cached))
                return cached;

            var path = Path.Combine(env.ContentRootPath, "Resources", $"{culture}.json");
            var dict = File.Exists(path)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path)) ?? []
                : [];

            Cache[culture] = dict;
            return dict;
        }
    }
}