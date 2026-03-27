using Content.Shared.Power.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.PowerCell.Components;

/// <summary>
/// This component enables power-cell related interactions (e.g. EntityWhitelists, cell sizes, examine, rigging).
/// The actual power functionality is provided by the <see cref="BatteryComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PowerCellComponent : Component
{
    //ss220 add states for power cells start
    [DataField]
    public int PowerCellVisualsLevels = 2;
    //ss220 add states for power cells end
}
