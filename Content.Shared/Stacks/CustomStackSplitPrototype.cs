using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Shared.Stacks;

[Prototype]
public sealed partial class CustomStackSplitPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     UI parameters for custom stack splitting.
    /// </summary>
    [DataField(required: true)]
    public InterfaceData Interface { get; private set; } = default!;
}
