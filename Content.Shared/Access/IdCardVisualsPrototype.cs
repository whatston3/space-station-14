using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.StatusIcon;

/// <summary>
/// Parameters for configuring crew ID card sprite layers.
/// Expected to exist at department or job IDs.
/// </summary>
[Prototype]
public sealed partial class IdCardVisualsPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<IdCardVisualsPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The colour to render the top stripe.
    /// </summary>
    [DataField]
    public Color? TopStripeColor;

    /// <summary>
    /// The colour to render the bottom stripe.
    /// </summary>
    [DataField]
    public Color? BottomStripeColor;

    /// <summary>
    /// The colour to render the bottom stripe.
    /// </summary>
    [DataField]
    public string? TopStripeState;

    /// <summary>
    /// The state to render in the bottom stripe.
    /// </summary>
    [DataField]
    public string? BottomStripeState;

    /// <summary>
    /// The state to render for the base.
    /// Actually setting this requires an agent UI card.
    /// </summary>
    [DataField]
    public string? BaseState;

    /// <summary>
    /// The held prefix of this item.
    /// </summary>
    [DataField]
    public string? HeldPrefix;
}
