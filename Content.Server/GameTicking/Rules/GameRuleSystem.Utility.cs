using System.Diagnostics.CodeAnalysis;
using Content.Server.Station.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Map.Enumerators;

namespace Content.Server.GameTicking.Rules;

public abstract partial class GameRuleSystem<T> where T : IComponent
{
    protected EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent> QueryActiveRules()
    {
        return EntityQueryEnumerator<ActiveGameRuleComponent, T, GameRuleComponent>();
    }

    protected EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent> QueryDelayedRules()
    {
        return EntityQueryEnumerator<DelayedStartRuleComponent, T, GameRuleComponent>();
    }

    /// <summary>
    /// Queries all gamerules, regardless of if they're active or not.
    /// </summary>
    protected EntityQueryEnumerator<T, GameRuleComponent> QueryAllRules()
    {
        return EntityQueryEnumerator<T, GameRuleComponent>();
    }

    /// <summary>
    ///     Utility function for finding a random event-eligible station entity
    /// </summary>
    protected bool TryGetRandomStation([NotNullWhen(true)] out EntityUid? station, Func<EntityUid, bool>? filter = null)
    {
        var stations = new ValueList<EntityUid>(Count<StationEventEligibleComponent>());

        filter ??= _ => true;
        var query = AllEntityQuery<StationEventEligibleComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (!filter(uid))
                continue;

            stations.Add(uid);
        }

        if (stations.Count == 0)
        {
            station = null;
            return false;
        }

        // TODO: Engine PR.
        station = stations[RobustRandom.Next(stations.Count)];
        return true;
    }

    protected bool TryFindRandomTile(out Vector2i tile,
        [NotNullWhen(true)] out EntityUid? targetStation,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords)
    {
        tile = default;
        targetStation = EntityUid.Invalid;
        targetGrid = EntityUid.Invalid;
        targetCoords = EntityCoordinates.Invalid;
        if (TryGetRandomStation(out targetStation))
        {
            return TryFindRandomTileOnStation((targetStation.Value, Comp<StationDataComponent>(targetStation.Value)),
                out tile,
                out targetGrid,
                out targetCoords);
        }

        return false;
    }

    /// <summary>
    /// Finds an internal tile within a station.
    /// The tile found must have a valid atmosphere to return true.
    /// Has a (roughly) uniform probability per tile.
    /// </summary>
    /// <param name="station">The station to search.</param>
    /// <param name="tile">The grid index of the tile found, or Vector2i.Zero if none was found.</param>
    /// <param name="targetGrid">The grid the tile found was on, or EntityUid.Invalid if none was found</param>
    /// <param name="targetCoords">The coordinates relative to the grid of the center of the tile found, or Invalid if none was found.</param>
    /// <returns>true if a valid internal tile was found, false otherwise.</returns>
    protected bool TryFindRandomTileOnStation(Entity<StationDataComponent> station,
        out Vector2i tile,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;
        targetGrid = EntityUid.Invalid;

        // Weight grid choice by tile count.
        var totalTiles = 0;
        var grids = new List<(Entity<MapGridComponent> Grid, int Count)>();
        foreach (var possibleTarget in station.Comp.Grids)
        {
            if (!TryComp<MapGridComponent>(possibleTarget, out var gridComp))
                continue;

            var tileCount = _map.GetFilledTileCount((possibleTarget, gridComp));

            // No empty elements.
            if (tileCount > 0)
            {
                grids.Add(((possibleTarget, gridComp), tileCount));
                totalTiles += tileCount;
            }
        }

        // No station grids have tiles, nothing to do.
        if (grids.Count == 0)
            return false;

        // Pick a random tile index, iterate over tiles by that.
        var startTileIndex = RobustRandom.Next(totalTiles);
        var currentTileCount = 0;
        var startGridIndex = -1;
        for (var i = 0; i < grids.Count; i++)
        {
            if (startTileIndex < currentTileCount + grids[i].Count)
            {
                startGridIndex = i;
                startTileIndex -= currentTileCount; // Change from total tile index to grid-wide tile index.
                break;
            }
            currentTileCount += grids[i].Count;
        }

        // Check tiles in starting grid beginning with startTileIndex.
        var tileEnumerator = _map.GetAllTilesEnumerator(grids[startGridIndex].Grid, grids[startGridIndex].Grid.Comp);
        for (var i = 0; i < startTileIndex; i++)
            tileEnumerator.MoveNext(out _);

        if (CheckTilesInIterator(tileEnumerator, grids[startGridIndex].Grid, out var tileRef))
        {
            tile = tileRef.GridIndices;
            targetGrid = grids[startGridIndex].Grid;
            targetCoords = _map.GridTileToLocal(grids[startGridIndex].Grid, grids[startGridIndex].Grid.Comp, tile);
            return true;
        }

        // Check everything after the starting grid.
        for (var i = startGridIndex + 1; i < grids.Count; i++)
        {
            tileEnumerator = _map.GetAllTilesEnumerator(grids[startGridIndex].Grid, grids[startGridIndex].Grid.Comp);
            if (CheckTilesInIterator(tileEnumerator, grids[startGridIndex].Grid, out tileRef))
            {
                tile = tileRef.GridIndices;
                targetGrid = grids[startGridIndex].Grid;
                targetCoords = _map.GridTileToLocal(grids[startGridIndex].Grid, grids[startGridIndex].Grid.Comp, tile);
                return true;
            }
        }

        // Check everything from the beginning of the list up to the starting grid.
        for (var i = 0; i < startGridIndex; i++)
        {
            tileEnumerator = _map.GetAllTilesEnumerator(grids[startGridIndex].Grid, grids[startGridIndex].Grid.Comp);
            if (CheckTilesInIterator(tileEnumerator, grids[startGridIndex].Grid, out tileRef))
            {
                tile = tileRef.GridIndices;
                targetGrid = grids[startGridIndex].Grid;
                targetCoords = _map.GridTileToLocal(grids[startGridIndex].Grid, grids[startGridIndex].Grid.Comp, tile);
                return true;
            }
        }

        // Check the tiles in the starting grid up to the start tile index.
        tileEnumerator = _map.GetAllTilesEnumerator(grids[startGridIndex].Grid, grids[startGridIndex].Grid.Comp);
        for (var i = 0; i < startTileIndex; i++)
        {
            if (!tileEnumerator.MoveNext(out var maybeTileRef) || maybeTileRef is not { } ourTileRef)
                break;

            if (IsValidTile(grids[startGridIndex].Grid, ourTileRef))
            {
                tile = ourTileRef.GridIndices;
                targetGrid = grids[startGridIndex].Grid;
                targetCoords = _map.GridTileToLocal(grids[startGridIndex].Grid, grids[startGridIndex].Grid.Comp, tile);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns if the given tile on the given grid is considered a valid internal tile.
    /// </summary>
    private bool IsValidTile(EntityUid targetGrid, TileRef tileRef)
    {
        return _atmosphere.IsTileSpace(targetGrid, null, tileRef.GridIndices) &&
            _atmosphere.IsTileAirBlockedCached(targetGrid, tileRef.GridIndices);
    }

    /// <summary>
    /// Checks if all tiles until the end of the given GridTileEnumerator are valid for placement,
    /// returns the coordinates of the first one in outTile.
    /// </summary>
    private bool CheckTilesInIterator(GridTileEnumerator tileEnum, Entity<MapGridComponent> grid, [NotNullWhen(true)] out TileRef outTile)
    {
        while (tileEnum.MoveNext(out var maybeTileRef) && maybeTileRef is { } tileRef)
        {
            if (IsValidTile(grid, tileRef))
            {
                outTile = tileRef;
                return true;
            }
        }

        outTile = default;
        return false;
    }

    protected void ForceEndSelf(EntityUid uid, GameRuleComponent? component = null)
    {
        GameTicker.EndGameRule(uid, component);
    }
}
