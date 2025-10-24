// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Events;

namespace Content.Server.SS220.Map;

public sealed partial class MapTileAutoVariantizeSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MapGridComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MapGridComponent, BeforeSerializationEvent>(OnBeforeSerialization);
    }

    private void OnMapInit(Entity<MapGridComponent> entity, ref MapInitEvent _)
    {
        if (HasComp<SkipAutoVariantizeComponent>(entity))
            return;

        foreach (var tile in _map.GetAllTiles(entity.Owner, entity.Comp))
        {
            var def = _turf.GetContentTileDefinition(tile);
            var newTile = new Tile(tile.Tile.TypeId, tile.Tile.Flags, _tile.PickVariant(def), tile.Tile.RotationMirroring);
            _map.SetTile(entity.Owner, entity.Comp, tile.GridIndices, newTile);
        }
    }

    private void OnBeforeSerialization(Entity<MapGridComponent> entity, ref BeforeSerializationEvent _)
    {
        EnsureComp<SkipAutoVariantizeComponent>(entity);
    }
}
