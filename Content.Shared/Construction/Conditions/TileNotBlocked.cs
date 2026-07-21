using Content.Shared.Maps;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Construction.Conditions;

/// <summary>
/// A construction conditions that checks entities on the tile for construction.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class TileNotBlocked : IConstructionCondition
{
    /// <summary>
    /// The collision mask that this condition should use to check for collisions.
    /// Any entity with hard fixtures of sufficient size whose layers overlap with this mask will prevent construction.
    /// </summary>
    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionMask>))]
    public int Mask { get; private set; } = (int)CollisionGroup.Impassable;

    /// <summary>
    /// If true, this condition will fail when placed on space tiles.
    /// </summary>
    [DataField]
    public bool FailIfSpace { get; private set; } = true;

    /// <summary>
    /// If true, this condition will fail when placed on non-sturdy tiles.
    /// </summary>
    [DataField]
    public bool FailIfNotSturdy { get; private set; } = true;

    public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
    {
        if (!IoCManager.Resolve<IEntityManager>().TrySystem<TurfSystem>(out var turfSystem))
            return false;

        if (!turfSystem.TryGetTileRef(location, out var tileRef))
            return false;

        if (FailIfSpace && turfSystem.IsSpace(tileRef.Value))
            return false;

        if (FailIfNotSturdy && !turfSystem.GetContentTileDefinition(tileRef.Value).Sturdy)
            return false;

        return !turfSystem.IsTileBlocked(tileRef.Value, (CollisionGroup)Mask);
    }

    public ConstructionGuideEntry GenerateGuideEntry()
    {
        return new ConstructionGuideEntry
        {
            Localization = "construction-step-condition-tile-not-blocked",
        };
    }
}
