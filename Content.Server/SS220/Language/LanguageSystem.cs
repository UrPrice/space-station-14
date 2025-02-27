// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking.Events;
using Content.Shared.Ghost;
using Content.Shared.Verbs;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Server.SS220.Language;

public sealed partial class LanguageSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LanguageManager _languageManager = default!;

    public readonly string UniversalLanguage = "Universal";
    public readonly string GalacticLanguage = "Galactic";

    // Cached values for one tick
    private static readonly Dictionary<string, string> ScrambleCache = new Dictionary<string, string>();

    private static int Seed = 0;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<LanguageComponent, MapInitEvent>(OnMapInit);

        // Verbs
        SubscribeLocalEvent<LanguageComponent, GetVerbsEvent<Verb>>(OnVerb);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ScrambleCache.Clear();
    }

    private void OnRoundStart(RoundStartingEvent args)
    {
        Seed = _random.Next();
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
        var saveEndWhitespace = char.IsWhiteSpace(input[^0]);

        input = RemoveColorTags(input);
        var cacheKey = $"{proto.ID}:{input}";

        // If the original message is already there earlier encrypted,
        // it is taken from the cache, it is necessary for the correct display when sending in the radio,
        // when the character whispers and transmits a message to the radio
        if (ScrambleCache.TryGetValue(cacheKey, out var cachedValue))
            return cachedValue;

        var scrambledText = proto.ScrambleMethod.ScrambleMessage(input, Seed);

        ScrambleCache[cacheKey] = scrambledText;

        if (saveEndWhitespace)
            scrambledText += " ";

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
        if (languageProto == null)
            return message;

        var languageStrings = SplitStringByLanguages(source, message, languageProto);
        var sanitizedMessage = new StringBuilder();
        foreach (var languageString in languageStrings)
        {
            if (CheckLanguage(listener, languageString.Item2))
            {
                sanitizedMessage.Append(languageString.Item1);
            }
            else
            {
                var scrambledString = ScrambleText(listener, languageString.Item1, languageString.Item2);
                scrambledString = SetColor(scrambledString, languageString.Item2);
                sanitizedMessage.Append(scrambledString);
            }
        }

        return sanitizedMessage.ToString();
    }

    public List<(string, LanguagePrototype)> SplitStringByLanguages(EntityUid source, string message, LanguagePrototype defaultLanguage)
    {
        var list = new List<(string, LanguagePrototype)>();
        var p = _languageManager.KeyPrefix;
        var textWithKeyPattern = $@"^{p}(.*?)\s(?={p}\w+\s)|(?<=\s){p}(.*?)\s(?={p}\w+\s)|(?<=\s){p}(.*)|^{p}(.*)"; // pizdec

        var matches = Regex.Matches(message, textWithKeyPattern);
        if (matches.Count <= 0)
        {
            list.Add((message, defaultLanguage));
            return list;
        }

        var textBeforeFirstTag = message.Substring(0, matches[0].Index);
        (string, LanguagePrototype?) buffer = (string.Empty, null);
        if (textBeforeFirstTag != string.Empty)
            buffer = (textBeforeFirstTag, defaultLanguage);

        foreach (Match m in matches)
        {
            if (!TryGetLanguageFromString(m.Value, out var messageWithoutTags, out var language) ||
                !CheckLanguage(source, language))
            {
                if (buffer.Item2 == null)
                {
                    buffer = (m.Value, defaultLanguage);
                }
                else
                {
                    buffer.Item1 += m.Value;
                }

                continue;
            }

            if (buffer.Item2 == language)
            {
                buffer.Item1 += messageWithoutTags;
                continue;
            }
            else if (buffer.Item2 != null)
            {
                list.Add((buffer.Item1, buffer.Item2));
            }

            buffer = (messageWithoutTags, language);
        }

        if (buffer.Item2 != null)
        {
            list.Add((buffer.Item1, buffer.Item2));
        }

        return list;
    }

    public bool TryGetLanguageFromString(string message,
        [NotNullWhen(true)] out string? messageWithoutTags,
        [NotNullWhen(true)] out LanguagePrototype? language)
    {
        messageWithoutTags = null;
        language = null;

        var keyPatern = $@"{_languageManager.KeyPrefix}\w+\s+";

        var m = Regex.Match(message, keyPatern);
        if (m == null || !_languageManager.TryGetLanguageByKey(m.Value.Trim(), out language))
            return false;

        messageWithoutTags = Regex.Replace(message, keyPatern, string.Empty);
        return messageWithoutTags != null && language != null;
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
            if (_languageManager.TryGetLanguageById(UniversalLanguage, out var universalProto))
                return universalProto;
        }

        var languageID = GetCurrentLanguage(ent);

        if (languageID == null)
            return null;

        if (_languageManager.TryGetLanguageById(languageID, out var proto))
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

        if (!_languageManager.TryGetLanguageById(languageId, out var proto))
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

