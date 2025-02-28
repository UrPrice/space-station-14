// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.SS220.Language.Components;

/// <summary>
///     A component that allows an entity to speak and understand languages.
///     Language prototypes are taken from YML of <see cref="LanguagePrototype"/>
///     The absence of this component gives the entity “Universal” language
/// </summary>
[RegisterComponent]
public sealed partial class LanguageComponent : Component
{
    /// <summary>
    ///  Selected language in which the entity will speak.
    /// </summary>
    [DataField]
    public ProtoId<LanguagePrototype>? SelectedLanguage { get; private set; }

    /// <summary>
    ///  List of languages that the Entity speaks and understands.
    /// </summary>
    [DataField, Access(Other = AccessPermissions.Read)]
    public List<ProtoId<LanguagePrototype>> AvailableLanguages { get; private set; } = new();

    #region Utilites
    /// <summary>
    /// Adds languages from <paramref name="languages"/>
    /// </summary>
    public void AddLanguages(List<string> languages)
    {
        foreach (var language in languages)
        {
            TryAddLanguage(language);
        }
    }

    /// <summary>
    /// Adds language to the <see cref="AvailableLanguages"/>
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="languageId"/> successful added</returns>
    public bool TryAddLanguage(string languageId)
    {
        var languageManager = IoCManager.Resolve<LanguageManager>();
        if (!languageManager.TryGetLanguageById(languageId, out _) ||
            AvailableLanguages.Contains(languageId))
            return false;

        AvailableLanguages.Add(languageId);
        SelectedLanguage ??= AvailableLanguages[0];
        return true;
    }

    /// <summary>
    /// Clears <see cref="AvailableLanguages"/>
    /// </summary>
    public void ClearLanguages()
    {
        AvailableLanguages.Clear();
        SelectedLanguage = null;
    }

    /// <summary>
    /// Removes language from <see cref="AvailableLanguages"/>
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="languageId"/> successful removed</returns>
    public bool RemoveLanguage(string languageId)
    {
        if (AvailableLanguages.Remove(languageId))
        {
            if (SelectedLanguage == languageId)
            {
                if (AvailableLanguages.Count > 0)
                    SelectedLanguage = AvailableLanguages.First();
                else
                    SelectedLanguage = null;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if <see cref="AvailableLanguages"/> contains this <paramref name="languageId"/>
    /// </summary>
    /// <returns></returns>
    public bool HasLanguage(string languageId)
    {
        return AvailableLanguages.Contains(languageId);
    }

    /// <summary>
    /// Tries set <see cref="SelectedLanguage"/> by <paramref name="index"/> of <see cref="AvailableLanguages"/>
    /// </summary>
    /// <returns></returns>
    public bool TrySetLanguage(int index)
    {
        if (AvailableLanguages.Count < index - 1)
            return false;

        SelectedLanguage = AvailableLanguages[index];
        return true;
    }

    /// <summary>
    /// Tries set <see cref="SelectedLanguage"/> by language id.
    /// Doesn't set language if <see cref="AvailableLanguages"/> doesn't contain this <paramref name="languageId"/>
    /// </summary>
    public bool TrySetLanguage(string languageId)
    {
        if (!HasLanguage(languageId))
            return false;

        SelectedLanguage = languageId;
        return true;
    }
    #endregion
}
