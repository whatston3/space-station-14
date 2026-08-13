using Robust.Shared.Prototypes;

namespace Content.Shared.StatusIcon;

/// <summary>
/// Parameters for configuring crew ID card sprite layers.
/// Expected to exist at department or job IDs.
/// </summary>
[Prototype]
public sealed partial class IdCardVisualsPrototype : IPrototype
{
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
}
