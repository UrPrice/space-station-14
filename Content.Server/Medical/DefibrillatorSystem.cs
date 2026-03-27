using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Robust.Shared.Player;
using Robust.Shared.Random;//SS220 LimitationRevive
using Content.Server.SS220.DefibrillatorSkill; //SS220 LimitationRevive
using Content.Server.SS220.LimitationRevive; //SS220 LimitationRevive
using Content.Shared.Ghost; //SS220 LimitationRevive
using Content.Shared.Inventory;

namespace Content.Server.Medical;

public sealed class DefibrillatorSystem : SharedDefibrillatorSystem
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!; //SS220 LimitationRevive
    [Dependency] private readonly InventorySystem _inventory = default!; // SS220 NewDefib

    protected override void OpenReturnToBodyEui(Entity<MindComponent> mind, ICommonSession session)
    {
        _eui.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);
    }
}
