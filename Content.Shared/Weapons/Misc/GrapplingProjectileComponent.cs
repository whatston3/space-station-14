using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Weapons.Misc;

/// <summary>
/// A component for projectiles shot from a grappling gun.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class GrapplingProjectileComponent : Component
{
    /// <summary>
    /// The time to despawn if this doesn't hit a wall.
    /// </summary>
    /// <remarks>
    /// If null, will not despawn.
    /// </remarks>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? DespawnTime;

    /// <summary>
    /// The gun entity that shot this projectile.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Gun;

    /// <summary>
    /// The player entity that shot this projectile.
    /// </summary>
    /// <remarks>
    /// Should have a PVS override if set.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public EntityUid? Shooter;
}
