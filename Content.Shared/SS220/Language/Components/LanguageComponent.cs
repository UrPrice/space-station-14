// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Language.Components;

/// <summary>
///     A component that allows an entity to speak and understand languages.
///     Language prototypes are taken from YML of <see cref="LanguagePrototype"/>
///     The absence of this component gives the entity “Universal” language
/// </summary>
[RegisterComponent, Access(typeof(SharedLanguageSystem), Other = AccessPermissions.Read)]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LanguageComponent : Component
{
    /// <summary>
    ///  Selected language in which the entity will speak.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<LanguagePrototype>? SelectedLanguage;

    /// <summary>
    ///  List of languages that the Entity speaks and understands.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<LanguagePrototype>> AvailableLanguages = new();
}
