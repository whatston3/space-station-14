namespace Content.Shared.Payload.Components;

/// <summary>
/// Component that enables payloads and payload triggers to function.
/// </summary>
/// <remarks>
/// If an entity with a <see cref="PayloadTriggerComponent"/> is installed into a an entity with a <see
/// cref="PayloadCaseComponent"/>, the trigger will grant components to the case-entity. If the case entity is
/// triggered, it will forward the trigger onto any contained payload entity.
/// </remarks>
[RegisterComponent]
public sealed partial class PayloadCaseComponent : Component
{
    /// <summary>
    /// If true, the payload can be examined.
    /// </summary>
    /// <remarks>
    /// Should be false for anything that cannot accept a payload (e.g. detonator caps)
    /// </remarks>
    [DataField]
    public bool Examinable = true;
}
