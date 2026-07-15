using System.Linq;
using Content.Shared.Maps;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class TileType : IConstructionCondition
    {
        /// <summary>
        /// A set of tile IDs that this condition will accept.
        /// </summary>
        [DataField("targets")]
        public List<ProtoId<ContentTileDefinition>> TargetTiles { get; private set; } = new();

        /// <summary>
        /// A set of tags that this placement requires.
        /// The target tile must have all
        /// </summary>
        [DataField]
        public List<ProtoId<TagPrototype>> TargetTags { get; private set; } = new();

        [DataField]
        public string? GuideText;

        [DataField]
        public SpriteSpecifier? GuideIcon;

        public bool Condition(EntityUid user, EntityCoordinates location, Direction direction)
        {
            if (!IoCManager.Resolve<IEntityManager>().TrySystem<TurfSystem>(out var turfSystem))
                return false;

            if (!turfSystem.TryGetTileRef(location, out var tileFound))
                return false;

            var tile = turfSystem.GetContentTileDefinition(tileFound.Value);
            // Is this tile whitelisted explicitly?
            if (TargetTiles.Contains(tile.ID))
                return true;

            // If any tags are defined, does this tile have all of the ones we need?
            return TargetTags.Count > 0 && TargetTags.All(tile.Tags.Contains);
        }

        public ConstructionGuideEntry? GenerateGuideEntry()
        {
            if (GuideText == null)
                return null;

            return new ConstructionGuideEntry()
            {
                Localization = GuideText,
                Icon = GuideIcon,
            };
        }
    }
}
