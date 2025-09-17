using System.Numerics;
using System.Linq;
using Content.Client.Pinpointer.UI;
using Content.Shared.Silicons.StationAi;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Collections;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CameraStaticShader = "CameraStatic";
    private static readonly ProtoId<ShaderPrototype> StencilMaskShader = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilDrawShader = "StencilDraw";

    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly HashSet<Vector2i> _visibleTiles = new();
    private readonly NavMapControl _navMap = new();
    private IRenderTexture? _staticTexture;
    private IRenderTexture? _stencilTexture;
    private Dictionary<Color, Color> _sRGBLookUp = new();
    private float _updateRate = 1f / 30f;
    private float _accumulator;

    public StationAiOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_stencilTexture?.Texture.Size != args.Viewport.Size)
        {
            _staticTexture?.Dispose();
            _stencilTexture?.Dispose();
            _stencilTexture = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "station-ai-stencil");
            _staticTexture = _clyde.CreateRenderTarget(args.Viewport.Size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: "station-ai-static");
        }

        var worldHandle = args.WorldHandle;

        var worldBounds = args.WorldBounds;

        var playerEnt = _player.LocalEntity;
        _entManager.TryGetComponent(playerEnt, out TransformComponent? playerXform);
        var gridUid = playerXform?.GridUid ?? EntityUid.Invalid;
        _entManager.TryGetComponent(gridUid, out MapGridComponent? grid);
        _entManager.TryGetComponent(gridUid, out BroadphaseComponent? broadphase);

        var invMatrix = args.Viewport.GetWorldToLocalMatrix();
        _accumulator -= (float)_timing.FrameTime.TotalSeconds;

        if (grid != null && broadphase != null)
        {
            var maps = _entManager.System<SharedMapSystem>(); // ss220-mgs
            var lookups = _entManager.System<EntityLookupSystem>();
            var xforms = _entManager.System<SharedTransformSystem>();
            var vision = _entManager.System<StationAiVisionSystem>(); // ss220-mgs

            if (_accumulator <= 0f)
            {
                _visibleTiles.Clear();
                // _entManager.System<StationAiVisionSystem>().GetView((gridUid, broadphase, grid), worldBounds, _visibleTiles);
                vision.GetView((gridUid, broadphase, grid), worldBounds.Enlarged(1f), _visibleTiles, new HashSet<Vector2i>()); // ss220-mgs
            }

            var gridMatrix = xforms.GetWorldMatrix(gridUid);
            var matty = Matrix3x2.Multiply(gridMatrix, invMatrix);

            // Draw visible tiles to stencil
            worldHandle.RenderInRenderTarget(_stencilTexture!, () =>
            {
                // ss220-mgs bgn
                worldHandle.SetTransform(matty);
                DrawOverlay(worldHandle);
                foreach (var tile in _visibleTiles)
                {
                    var aabb = lookups.GetLocalBounds(tile, grid.TileSize);
                    worldHandle.DrawRect(aabb, Color.White);

                }
            },
            Color.Transparent);

            // Once this is gucci optimise rendering.
            worldHandle.RenderInRenderTarget(_staticTexture!,
            () =>
            {
                worldHandle.SetTransform(matty);
                var shader = _proto.Index(CameraStaticShader).Instance();
                worldHandle.UseShader(shader);
                var tiles = maps.GetTilesEnumerator(gridUid, grid, worldBounds.Enlarged(grid.TileSize / 2f));
                var gridEnt = new Entity<BroadphaseComponent, MapGridComponent>(gridUid, _entManager.GetComponent<BroadphaseComponent>(gridUid), grid);
                var airlockVertCache = new ValueList<Vector2>(9);
                var airlockColor = Color.Gold;
                var airlockVerts = new ValueList<Vector2>();

                while (tiles.MoveNext(out var tileRef))
                {
                    if (_visibleTiles.Contains(tileRef.GridIndices))
                        continue;

                    // TODO: GetView should do these.
                    var aabb = lookups.GetLocalBounds(tileRef.GridIndices, grid.TileSize);

                    if (vision.TryAirlock(gridEnt, tileRef.GridIndices, out var open))
                    {
                        var midBottom = (aabb.BottomRight - aabb.BottomLeft) / 2f + aabb.BottomLeft;
                        var midTop = (aabb.TopRight - aabb.TopLeft) / 2f + aabb.TopLeft;
                        const float IndentSize = 0.10f;
                        const float OpenOffset = 0.25f;

                        // Use triangle-fan and draw from the mid-vert

                        // Left half
                        {
                            airlockVertCache.Clear();
                            airlockVertCache.Add(aabb.Center with { X = aabb.Center.X - aabb.Width / 2f });
                            airlockVertCache.Add(aabb.BottomLeft);
                            airlockVertCache.Add(midBottom);
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(0f, grid.TileSize * 0.35f));
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(-grid.TileSize * IndentSize, grid.TileSize * 0.15f));
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(grid.TileSize * IndentSize, grid.TileSize * 0.15f));
                            airlockVertCache.Add(midTop);
                            airlockVertCache.Add(aabb.TopLeft);

                            if (open)
                            {
                                for (var i = 0; i < 8; i++)
                                {
                                    airlockVertCache[i] -= new Vector2(OpenOffset, 0f);
                                }
                            }

                            for (var i = 0; i < airlockVertCache.Count; i++)
                            {
                                airlockVerts.Add(airlockVertCache[i]);
                                var next = (airlockVertCache[(i + 1) % airlockVertCache.Count]);
                                airlockVerts.Add(next);
                            }

                            worldHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, airlockVertCache.Span, airlockColor.WithAlpha(0.05f));
                        }

                        // Right half
                        {
                            airlockVertCache.Clear();
                            airlockVertCache.Add(aabb.Center with { X = aabb.Center.X + aabb.Width / 2f });
                            airlockVertCache.Add(aabb.BottomRight);
                            airlockVertCache.Add(midBottom);
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(0f, grid.TileSize * 0.35f));
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(grid.TileSize * IndentSize, 0.15f));
                            airlockVertCache.Add(airlockVertCache[^1] + new Vector2(-grid.TileSize * IndentSize, grid.TileSize * 0.15f));
                            airlockVertCache.Add(midTop);
                            airlockVertCache.Add(aabb.TopRight);

                            if (open)
                            {
                                for (var i = 0; i < 8; i++)
                                {
                                    airlockVertCache[i] += new Vector2(OpenOffset, 0f);
                                }
                            }

                            for (var i = 0; i < airlockVertCache.Count; i++)
                            {
                                airlockVerts.Add(airlockVertCache[i]);
                                var next = (airlockVertCache[(i + 1) % airlockVertCache.Count]);
                                airlockVerts.Add(next);
                            }

                            worldHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, airlockVertCache.Span, airlockColor.WithAlpha(0.05f));
                            // ss220-mgs end
                        }

                        continue;
                    }
                    // var occluded = vision.IsOccluded(gridEnt, tileRef.GridIndices);
                    // Draw walls
                    // if (occluded)
                    // {
                    //     worldHandle.DrawRect(aabb, Color.White.WithAlpha(0.05f));
                    //     worldHandle.DrawRect(aabb, Color.White, filled: false);
                    // }
                    // Draw tiles
                    // else
                    // {
                    //     worldHandle.DrawRect(aabb, Color.Green.WithAlpha(0.35f), filled: false);
                    // }
                }

                worldHandle.DrawPrimitives(DrawPrimitiveTopology.LineList, airlockVerts.Span, Color.Gold);
            },
            Color.Black);
        }
        else
        {
            worldHandle.RenderInRenderTarget(_stencilTexture!, () =>
            {
            },
            Color.Transparent);
            worldHandle.RenderInRenderTarget(_staticTexture!,
            () =>
            {
                worldHandle.SetTransform(Matrix3x2.Identity);
                worldHandle.DrawRect(worldBounds, Color.Black);
            }, Color.Black);
        }

        if (_accumulator <= 0f)
        {
            _accumulator = MathF.Max(0f, _accumulator + _updateRate);
        }

        // Use the lighting as a mask
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilMask").Instance());
        worldHandle.DrawTextureRect(_stencilTexture!.Texture, worldBounds);
        // Draw the static
        worldHandle.UseShader(_proto.Index<ShaderPrototype>("StencilDraw").Instance());
        worldHandle.DrawTextureRect(_staticTexture!.Texture, worldBounds);
        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);
    }

    // ss220-mgs bgn
    protected void DrawOverlay(DrawingHandleWorld handle)
    {
        _navMap.WallColor = new(200, 200, 200);
        _navMap.TileColor = new(100, 100, 100);

        // Wall sRGB
        if (!_sRGBLookUp.TryGetValue(_navMap.WallColor, out var wallsRGB))
        {
            wallsRGB = Color.ToSrgb(_navMap.WallColor);
            _sRGBLookUp[_navMap.WallColor] = wallsRGB;
        }

        // Draw floor tiles
        if (_navMap.TilePolygons.Any())
        {
            foreach (var (polygonVerts, polygonColor) in _navMap.TilePolygons)
            {
                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, polygonVerts[..polygonVerts.Length], polygonColor);
            }
        }

        // Draw map lines
        if (_navMap.TileLines.Any())
        {
            var lines = new ValueList<Vector2>(_navMap.TileLines.Count * 2);

            if (lines.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, lines.Span, wallsRGB);
        }

        // Draw map rects
        if (_navMap.TileRects.Any())
        {
            var rects = new ValueList<Vector2>(_navMap.TileRects.Count * 8);

            foreach (var (lt, rb) in _navMap.TileRects)
            {
                var leftTop = new Vector2(lt.X, lt.Y);
                var rightBottom = new Vector2(rb.X, rb.Y);

                var rightTop = new Vector2(rightBottom.X, leftTop.Y);
                var leftBottom = new Vector2(leftTop.X, rightBottom.Y);

                rects.Add(leftTop);
                rects.Add(rightTop);
                rects.Add(rightTop);
                rects.Add(rightBottom);
                rects.Add(rightBottom);
                rects.Add(leftBottom);
                rects.Add(leftBottom);
                rects.Add(leftTop);
            }

            if (rects.Count > 0)
                handle.DrawPrimitives(DrawPrimitiveTopology.LineList, rects.Span, wallsRGB);
        }
    }
    // ss220-mgs end
}
