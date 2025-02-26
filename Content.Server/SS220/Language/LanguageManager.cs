// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.SS220.Language;

public sealed class LanguageManager
{
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public List<LanguagePrototype> Languages { get; private set; } = new();

    private ISawmill _sawmill = default!;

    public void Initialize()
    {
        Languages = new List<LanguagePrototype>();
        foreach (var language in _prototype.EnumeratePrototypes<LanguagePrototype>())
        {
            Languages.Add(language);
        }

        _sawmill = _logManager.GetSawmill("language.manager");
    }

    public bool TryGetLanguageById(string id, [NotNullWhen(true)] out LanguagePrototype? language)
    {
        language = Languages.Find(l => l.ID == id);

        if (language == null)
            _sawmill.Error($"Doesn't found a language with id: {id}");

        return language != null;
    }

    public bool TryGetLanguageByKey(string key, [NotNullWhen(true)] out LanguagePrototype? language)
    {
        language = Languages.Find(l => l.Key == key);

        if (language == null)
            _sawmill.Error($"Doesn't found a language with key: {key}");

        return language != null;
    }
}
