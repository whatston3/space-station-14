using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Components;

/// <summary>
/// A component to simplify YAML definitions for ID card visuals.
/// Users must use AppearanceComponent, and should use a SpriteComponent with known layers.
/// </summary>
/// <seealso cref="AppearanceComponent"/>
/// <seealso cref="IdCardVisualLayers"/>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class IdCardVisualsComponent : Component
{
    /// <summary>
    /// The state of the base layer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? BaseState;

    /// <summary>
    /// The state of the top stripe.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? StripeTopState;

    /// <summary>
    /// The top color of the top stripe.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color? StripeTopColor;

    /// <summary>
    /// The state of the bottom stripe.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? StripeBottomState;

    /// <summary>
    /// The color of the bottom stripe.
    /// A null value should imply white.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color? StripeBottomColor;

    /// <summary>
    /// The starting job icon.
    /// Useful for oddball roles (e.g. senior courier) or antag IDs.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? JobIconState;

    /// <summary>
    /// The starting visuals prototype to use.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<IdCardVisualsPrototype>? StartingVisuals;

    /// <summary>
    /// Whether or not this ID card should ever update its visuals.
    /// Useful for antag ID cards.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UpdateVisuals = true;

    /// <summary>
    /// The last prototype received.
    /// Prevents unnecessary sprite fiddling.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public string? LastPrototype;
}

/// <summary>
/// An enumeration for layers on an ID card sprite.
/// </summary>
[Serializable, NetSerializable]
public enum IdCardVisualLayers
{
    Base, // The main body of the card.
    TopStripe, // A light section of the stripe, if it has one.
    BottomStripe, // A dark section of the stripe, if it has one. Expected to render on top of TopStripe.
    JobIcon // The job icon of the card, if it has one.
}

/// <summary>
/// An enumeration for storing appearance data on an ID card.
/// </summary>
[Serializable, NetSerializable]
public enum IdCardVisuals
{
    JobProto // (string) the ProtoId of the JobPrototype to mimic.  Defaults to IdCardComponent.JobPrototype if not present.
}
