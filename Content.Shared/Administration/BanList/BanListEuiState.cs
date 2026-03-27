using Content.Shared.Eui;
using Content.Shared.SS220.Administration.BanList;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.BanList;

[Serializable, NetSerializable]
public sealed class BanListEuiState : EuiStateBase
{
    public BanListEuiState(string banListPlayerName, List<SharedBan> bans, List<SharedBan> roleBans, List<SharedServerSpeciesBan> speciesBans /* UPSTREAM_TODO */)
    {
        BanListPlayerName = banListPlayerName;
        Bans = bans;
        RoleBans = roleBans;
        SpeciesBans = speciesBans; // SS220 Species bans
    }

    public string BanListPlayerName { get; }
    public List<SharedBan> Bans { get; }
    public List<SharedBan> RoleBans { get; }
    public List<SharedServerSpeciesBan> SpeciesBans { get; } // SS220 Species bans // UPSTREAM_TODO
}
