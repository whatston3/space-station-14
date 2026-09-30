namespace Content.Shared.Construction;

/// <summary>
/// An interface for an action that runs after entering a node in a construction graph.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface IGraphAction
{
    /// <summary>
    /// Runs this action.
    /// </summary>
    /// <remarks>
    /// TODO: pass in node/edge & graph ID for better error logs.
    /// </remarks>
    void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager);
}
