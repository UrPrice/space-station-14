// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking.Events;
using Content.Shared.Ghost;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Server.SS220.Language;

public sealed partial class LanguageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public readonly string UniversalLanguage = "Universal";
    public readonly string GalacticLanguage = "Galactic";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<LanguageComponent, MapInitEvent>(OnMapInit);

        // Verbs
        SubscribeLocalEvent<LanguageComponent, GetVerbsEvent<Verb>>(OnVerb);
    }

    private static readonly Dictionary<string, string> ScrambleCache = new Dictionary<string, string>();
    private const int SCRAMBLE_CACHE_LEN = 20;

    private void OnRoundStart(RoundStartingEvent args)
    {
        ScrambleCache.Clear();
    }

    /// <summary>
    ///     Initializes an entity with a language component,
    ///     either the first language in the LearnedLanguages list into the CurrentLanguage variable
    /// </summary>
    private void OnMapInit(Entity<LanguageComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.CurrentLanguage == null)
            ent.Comp.CurrentLanguage = ent.Comp.LearnedLanguages.FirstOrDefault(UniversalLanguage);
    }

    /// <summary>
    ///     A method of encrypting the original message into a message created
    ///     from the syllables of prototypes languages
    /// </summary>
    public string ScrambleText(EntityUid? ent, string input, LanguagePrototype proto)
    {
        input = RemoveColorTags(input);
        var cacheKey = $"{proto.ID}:{input}";

        // If the original message is already there earlier encrypted,
        // it is taken from the cache, it is necessary for the correct display when sending in the radio,
        // when the character whispers and transmits a message to the radio
        if (ScrambleCache.TryGetValue(cacheKey, out var cachedValue))
            return cachedValue;

        var scrambledText = proto.ScrambleMethod.ScrambleMessage(input);

        ScrambleCache[cacheKey] = scrambledText;
        // Removes the first message from the cache if it fills up
        if (ScrambleCache.Count > SCRAMBLE_CACHE_LEN)
        {
            var keysToRemove = ScrambleCache.Keys.Take(ScrambleCache.Count - SCRAMBLE_CACHE_LEN).ToList();
            foreach (var key in keysToRemove)
            {
                ScrambleCache.Remove(key);
            }
        }

        return scrambledText;
    }

    /// <summary>
    ///     Workaround for some message transmissions.
    ///     Removes BBCodes colors leaving only the original message.
    ///     (I couldn't think of anything cleverer)
    /// </summary>
    public string RemoveColorTags(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string pattern = @"\[color=#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})\](.*?)\[/color\]";
        string cleanedText = Regex.Replace(input, pattern, "$2");

        return cleanedText;
    }

    public string SanitizeMessage(EntityUid source, EntityUid listener, string message)
    {
        var languageProto = GetProto(source);
        if (languageProto == null || CheckLanguage(listener, languageProto))
            return message;

        var newMessage = ScrambleText(source, message, languageProto);
        newMessage = SetColor(newMessage, languageProto);
        return newMessage;
    }

    /// <summary>
    ///     Method that checks an entity for the presence of a prototype language
    ///     or for the presence of a universal language
    /// </summary>
    public bool CheckLanguage(EntityUid ent, LanguagePrototype? proto)
    {
        if (proto == null)
            return false;

        if (KnowsUniversalLanguage(ent))
            return true;

        return KnowsLanguages(ent, proto.ID);
    }

    public bool KnowsLanguages(EntityUid ent, string languageId)
    {
        if (!TryComp<LanguageComponent>(ent, out var comp))
            return false;

        if (comp.CurrentLanguage == languageId)
            return true;

        return comp.LearnedLanguages.Contains(languageId);
    }

    public bool KnowsUniversalLanguage(EntityUid ent)
    {
        if (HasComp<GhostComponent>(ent))
            return true;

        if (!TryComp<LanguageComponent>(ent, out var comp))
            return true;

        if (comp != null && comp.CurrentLanguage == UniversalLanguage)
            return true;

        if (comp != null && comp.LearnedLanguages.Contains(UniversalLanguage))
            return true;

        return false;
    }

    /// <summary>
    ///     A method to get a prototype language from an entity.
    ///     If the entity does not have a language component, a universal language is assigned.
    /// </summary>
    public LanguagePrototype? GetProto(EntityUid ent)
    {
        if (!TryComp<LanguageComponent>(ent, out var comp))
        {
            if (_proto.TryIndex<LanguagePrototype>(UniversalLanguage, out var universalProto))
                return universalProto;
        }

        var languageID = GetCurrentLanguage(ent);

        if (languageID == null)
            return null;

        if (_proto.TryIndex<LanguagePrototype>(languageID, out var proto))
            return proto;

        return null;
    }

    public string? GetCurrentLanguage(EntityUid ent)
    {
        if (!TryComp<LanguageComponent>(ent, out var comp))
            return null;

        return comp.CurrentLanguage;
    }

    public void AddLanguages(EntityUid uid, List<string> languages)
    {
        foreach (var language in languages)
        {
            AddLanguage(uid, language);
        }
    }

    public void AddLanguage(EntityUid uid, string languageId)
    {
        if (!TryComp<LanguageComponent>(uid, out var comp))
            return;

        if (!_proto.TryIndex<LanguagePrototype>(languageId, out var proto))
        {
            Log.Error($"Doesn't found a LanguagePrototype with id: {languageId}");
            return;
        }

        if (!comp.LearnedLanguages.Contains(proto))
            comp.LearnedLanguages.Add(proto);
    }

    /// <summary>
    ///     Sets the color of the prototype language to the message 
    /// </summary>
    public string SetColor(string message, LanguagePrototype proto)
    {
        if (proto.Color == null)
            return message;

        var color = proto.Color.Value.ToHex();
        message = $"[color={color}]{message}[/color]";
        return message;
    }

    public void AddLanguagesFromSource(EntityUid source, EntityUid target)
    {
        if (!TryComp<LanguageComponent>(source, out var sourceComp))
            return;

        var targetComp = EnsureComp<LanguageComponent>(target);
        foreach (var language in sourceComp.LearnedLanguages)
        {
            if (!targetComp.LearnedLanguages.Contains(language))
                targetComp.LearnedLanguages.Add(language);
        }
    }
}

