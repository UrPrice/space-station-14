using Robust.Shared.GameStates;
namespace Content.Shared.Flash.Components;

/// <summary>
/// Upon being triggered will flash in an area around it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FlashOnTriggerComponent : Component
{
    [DataField]
    public float Range = 1.0f;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(8);

    [DataField]
    public float Probability = 1.0f;

    // start 220 flash grenade stun
    [DataField]
    public bool Stun = false;

    [DataField]
    public float StunDuration = 3.0f;
    // end 220 flash grenade stun
}
