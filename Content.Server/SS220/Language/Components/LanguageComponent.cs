// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;

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
    public string? SelectedLanguage = default!;

    /// <summary>
    ///  List of languages that the Entity speaks and understands.
    /// </summary>
    [DataField]
    public List<ProtoId<LanguagePrototype>> AvailableLanguages { get; set; } = new();
}
