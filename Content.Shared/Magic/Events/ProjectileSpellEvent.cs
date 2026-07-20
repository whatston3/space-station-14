using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Events;

public sealed partial class ProjectileSpellEvent : WorldTargetActionEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;

    /// <summary>
    /// How fast the projectile should travel
    /// </summary>
    [DataField]
    public float ProjectileSpeed = 25f;

    /// <summary>
    /// A coefficient to adjust for velocity behind you.
    /// Useful for slow projectiles to prevent them from feeling sluggish.
    /// </summary>
    [DataField]
    public float BehindCompensation;
}
