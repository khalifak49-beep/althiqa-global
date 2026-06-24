using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace HomeMaids.Services.Localization;

/// <summary>
/// Lightweight JSON-backed translation service.
/// Reads /wwwroot/locales/{culture}.json once, caches in memory, returns the key
/// itself if a translation is missing (so untranslated screens still render).
/// </summary>
public interface ITranslator
{
    string this[string key] { get; }
    string T(string key, params object[] args);
    bool IsArabic { get; }
    string CurrentCulture { get; }
}

public class Translator : ITranslator
{
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _cache = new();
    private static readonly object _loadLock = new();
    private const string DefaultCulture = "ar";

    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _httpCtx;

    public Translator(IWebHostEnvironment env, IHttpContextAccessor httpCtx)
    {
        _env = env;
        _httpCtx = httpCtx;
    }

    public string CurrentCulture
    {
        get
        {
            var name = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return name == "en" ? "en" : "ar";
        }
    }

    public bool IsArabic => CurrentCulture == "ar";

    public string this[string key] => T(key);

    public string T(string key, params object[] args)
    {
        var dict = Load(CurrentCulture);
        if (!dict.TryGetValue(key, out var value))
        {
            // Fallback to default culture before giving up
            if (CurrentCulture != DefaultCulture)
            {
                var fallback = Load(DefaultCulture);
                if (fallback.TryGetValue(key, out var fb)) value = fb;
            }
            value ??= key; // last-ditch: show the key
        }
        return args.Length > 0 ? string.Format(value, args) : value;
    }

    private IReadOnlyDictionary<string, string> Load(string culture)
    {
        return _cache.GetOrAdd(culture, c =>
        {
            lock (_loadLock)
            {
                var path = Path.Combine(_env.WebRootPath, "locales", $"{c}.json");
                if (!File.Exists(path))
                    return new Dictionary<string, string>();

                using var fs = File.OpenRead(path);
                var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(fs,
                    new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                return raw ?? new Dictionary<string, string>();
            }
        });
    }
}
