// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.Language.EncryptionMethods;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Language;

[Prototype("language")]
public sealed partial class LanguagePrototype : IPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string? Name { get; private set; }

    [DataField]
    public string? Description { get; private set; }

    [DataField(required: true)]
    public string Key = string.Empty;

    /// <summary>
    ///  The color of the language in which messages will be recolored, 
    ///  an empty value will not be recolored
    /// </summary>
    [DataField]
    public Color? Color;

    [DataField]
    public ScrambleMethod ScrambleMethod = new RandomSyllablesScrambleMethod();
}
