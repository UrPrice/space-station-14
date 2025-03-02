// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.Components;

namespace Content.Shared.SS220.Language.Systems;

public abstract class SharedLanguageSystem : EntitySystem
{
    [Dependency] private readonly LanguageManager _language = default!;

    #region Component
    /// <summary>
    /// Adds languages to <see cref="LanguageComponent.AvailableLanguages"/> from <paramref name="languageIds"/>.
    /// </summary>
    public void AddLanguages(Entity<LanguageComponent> ent, IEnumerable<string> languageIds)
    {
        foreach (var language in languageIds)
        {
            AddLanguage(ent, language);
        }
    }

    /// <summary>
    /// Adds language to the <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="languageId"/> successful added</returns>
    public bool AddLanguage(Entity<LanguageComponent> ent, string languageId)
    {
        if (ent.Comp.AvailableLanguages.Contains(languageId) ||
            !_language.TryGetLanguageById(languageId, out var language))
            return false;

        ent.Comp.AvailableLanguages.Add(languageId);
        ent.Comp.SelectedLanguage ??= languageId;
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Clears <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    public void ClearLanguages(Entity<LanguageComponent> ent)
    {
        ent.Comp.AvailableLanguages.Clear();
        ent.Comp.SelectedLanguage = null;
        Dirty(ent);
    }

    /// <summary>
    /// Removes language from <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="languageId"/> successful removed</returns>
    public bool RemoveLanguage(Entity<LanguageComponent> ent, string languageId)
    {
        if (ent.Comp.AvailableLanguages.Remove(languageId))
        {
            if (ent.Comp.SelectedLanguage == languageId && !TrySetLanguage(ent, 0))
                ent.Comp.SelectedLanguage = null;

            Dirty(ent);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if <see cref="LanguageComponent.AvailableLanguages"/> contains this <paramref name="languageId"/>
    /// </summary>
    public static bool HasLanguage(Entity<LanguageComponent> ent, string langageId)
    {
        return ent.Comp.AvailableLanguages.Contains(langageId);
    }

    /// <summary>
    /// Tries set <see cref="LanguageComponent.SelectedLanguage"/> by <paramref name="index"/> of <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    public bool TrySetLanguage(Entity<LanguageComponent> ent, int index)
    {
        if (ent.Comp.AvailableLanguages.Count <= index)
            return false;

        ent.Comp.SelectedLanguage = ent.Comp.AvailableLanguages[index];
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Tries set <see cref="LanguageComponent.SelectedLanguage"/> by language id.
    /// Doesn't set language if <see cref="LanguageComponent.AvailableLanguages"/> doesn't contain this <paramref name="languageId"/>
    /// </summary>
    public bool TrySetLanguage(Entity<LanguageComponent> ent, string languageId)
    {
        if (!HasLanguage(ent, languageId))
            return false;

        ent.Comp.SelectedLanguage = languageId;
        Dirty(ent);
        return true;
    }
    #endregion
}
