// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.SS220.Language;

public sealed class LanguageManager
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public List<LanguagePrototype> Languages { get; private set; } = new();

    public readonly string KeyPrefix = ":";

    public void Initialize()
    {
        Languages = new List<LanguagePrototype>();
        foreach (var language in _prototype.EnumeratePrototypes<LanguagePrototype>())
        {
            Languages.Add(language);
        }
    }

    public bool TryGetLanguageById(string id, [NotNullWhen(true)] out LanguagePrototype? language)
    {
        language = Languages.Find(l => l.ID == id);
        return language != null;
    }

    public bool TryGetLanguageByKey(string key, [NotNullWhen(true)] out LanguagePrototype? language)
    {
        language = Languages.Find(l => KeyPrefix + l.Key == key);
        return language != null;
    }
}
