using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;

namespace Content.Shared.Physics;

/// <summary>
///     Defined collision groups for the physics system.
///     Mask is what it collides with when moving. Layer is what CollisionGroup it is part of.
/// </summary>
[Flags, PublicAPI]
[FlagsFor(typeof(CollisionLayer)), FlagsFor(typeof(CollisionMask))]
public enum CollisionGroup
{
#pragma warning disable IDE0055 // Allow funny spaces.
    None                  = 0,
    Opaque                = 1 <<  0, // 1 Blocks light, can be hit by lasers
    Impassable            = 1 <<  1, // 2 Walls, objects impassable by any means
    MidImpassable         = 1 <<  2, // 4 Mobs, players, crabs, etc
    HighImpassable        = 1 <<  3, // 8 Things on top of tables and things that block tall/large mobs.
    LowImpassable         = 1 <<  4, // 16 For things that can fit under a table or squeeze under an airlock
    GhostImpassable       = 1 <<  5, // 32 Things impassible by ghosts/observers, ie blessed tiles or forcefields
    BulletImpassable      = 1 <<  6, // 64 Can be hit by bullets
    InteractImpassable    = 1 <<  7, // 128 Blocks interaction/InRangeUnobstructed
    WindoorOpener         = 1 <<  8, // 256 A layer for windoor collisions.  Anything with this in a layer should keep a windoor open.  Anything with this in a mask should check for windoor collisions.
    AirlockOpener         = 1 <<  9, // 512 A layer for airlock sensors.  Anything with this in a layer should keep an airlock open.  Anything with this in a mask should check for airlock collisions.
    ShutterOpener         = 1 << 10, // 1024 A layer for shutter sensors.  Anything with this in a layer should keep shutters open.  Anything with this in a mask should check for shutter collisions.
#pragma warning restore IDE0055

    MapGrid = MapGridHelpers.CollisionGroup, // Map grids, like shuttles. This is the actual grid itself, not the walls or other entities connected to the grid.

    // 32 possible groups
    // Why dis exist
    AllMask = -1,

    SingularityLayer = Opaque | Impassable | MidImpassable | HighImpassable | LowImpassable | BulletImpassable | InteractImpassable,

    // Humanoids, etc.
    MobMask = Impassable | HighImpassable | MidImpassable | LowImpassable,
    MobLayer = Opaque | BulletImpassable | WindoorOpener | AirlockOpener | ShutterOpener,
    // Mice, drones
    SmallMobMask = Impassable | LowImpassable,
    SmallMobLayer = Opaque | BulletImpassable,
    // Birds/other small flyers
    FlyingMobMask = Impassable | HighImpassable,
    FlyingMobLayer = Opaque | BulletImpassable | WindoorOpener | AirlockOpener | ShutterOpener,

    // Mechs
    LargeMobMask = Impassable | HighImpassable | MidImpassable | LowImpassable,
    LargeMobLayer = Opaque | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | WindoorOpener | AirlockOpener | ShutterOpener,

    // Machines, computers
    MachineMask = Impassable | MidImpassable | LowImpassable,
    MachineLayer = Opaque | MidImpassable | LowImpassable | BulletImpassable | AirlockOpener,
    ConveyorMask = Impassable | MidImpassable | LowImpassable,

    // Crates
    CrateMask = Impassable | HighImpassable | LowImpassable,

    // Tables that SmallMobs can go under
    TableMask = Impassable | MidImpassable,
    TableLayer = MidImpassable | AirlockOpener,

    // Tabletop machines, windoors, firelocks
    TabletopMachineMask = Impassable | HighImpassable,
    // Tabletop machines
    TabletopMachineLayer = Opaque | BulletImpassable | AirlockOpener,

    // Airlocks, windoors, firelocks
    GlassAirlockLayer = HighImpassable | MidImpassable | BulletImpassable | InteractImpassable,
    AirlockLayer = Opaque | GlassAirlockLayer,

    // Airlock assembly
    HumanoidBlockLayer = HighImpassable | MidImpassable | WindoorOpener | AirlockOpener | ShutterOpener,

    // Soap, spills
    SlipLayer = MidImpassable | LowImpassable,
    ItemMask = Impassable | HighImpassable,
    ThrownItem = Impassable | HighImpassable | BulletImpassable,
    WallLayer = Opaque | Impassable | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable | WindoorOpener | AirlockOpener | ShutterOpener,
    GlassLayer = Impassable | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable | WindoorOpener | AirlockOpener | ShutterOpener,
    HalfWallLayer = MidImpassable | LowImpassable | WindoorOpener | AirlockOpener | ShutterOpener,
    FlimsyLayer = Opaque | HighImpassable | MidImpassable | LowImpassable | InteractImpassable | WindoorOpener | AirlockOpener | ShutterOpener,

    // Allows people to interact past and target players inside of this
    SpecialWallLayer = Opaque | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | WindoorOpener | AirlockOpener | ShutterOpener,

    // Statue, monument, airlock, window
    FullTileMask = Impassable | HighImpassable | MidImpassable | LowImpassable | InteractImpassable,
    // FlyingMob can go past
    FullTileLayer = Opaque | HighImpassable | MidImpassable | LowImpassable | BulletImpassable | InteractImpassable | WindoorOpener | AirlockOpener | ShutterOpener,

    SubfloorMask = Impassable | LowImpassable
}
