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
        [DataField("targets")]
        public List<string> TargetTiles { get; private set; } = new();

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

            // Look for our ID, and failing that, if the tile type matches any tags.
            return TargetTiles.Contains(tile.ID) || TargetTags.Any(tile.Tags.Contains);
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
