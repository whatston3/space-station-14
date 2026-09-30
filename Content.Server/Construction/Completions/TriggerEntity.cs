using Content.Shared.Construction;
using Content.Shared.Trigger.Systems;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions;

/// <summary>
/// An construction graph action that invokes a named trigger on the entity under construction.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class TriggerEntity : IGraphAction
{
    /// <summary>
    /// The name of the trigger to invoke.
    /// </summary>
    [DataField(required: true)] public string Trigger = default!;

    /// <summary>
    /// Should the trigger run predicted?
    /// </summary>
    /// <remarks>
    /// Currently does nothing, only useful for API completeness if/when moved to shared.
    /// </remarks>
    [DataField] public bool Predicted = true;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        entityManager.System<TriggerSystem>().Trigger(uid, userUid, Trigger, Predicted);
    }
}
