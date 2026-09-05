using Content.Shared.Research.Components;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Client.UserInterface;
using JetBrains.Annotations;

namespace Content.Client.Xenoarchaeology.Ui;

/// <summary>
/// A BUI for the artifact analysis console. Wraps a <see cref="AnalysisConsoleMenu"/>.
/// </summary>
/// <remarks>
/// Proxies server-provided UI updates related to the console, a connected artifact analyzer, and an artifact lying on it.
/// </remarks>
/// <seealso cref="ArtifactAnalyzerComponent"/>
[UsedImplicitly]
public sealed class AnalysisConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private AnalysisConsoleMenu? _consoleMenu;

    /// <inheritdoc />
    protected override void Open()
    {
        base.Open();

        _consoleMenu = this.CreateWindow<AnalysisConsoleMenu>();

        if (!EntMan.TryGetComponent<AnalysisConsoleComponent>(Owner, out var comp))
        {
            Close();
            return;
        }

        _consoleMenu.SetOwner((Owner, comp));

        _consoleMenu.OnServerSelectionButtonPressed += () =>
        {
            SendMessage(new ConsoleServerSelectionMessage());
        };
        _consoleMenu.OnExtractButtonPressed += () =>
        {
            SendMessage(new AnalysisConsoleExtractButtonPressedMessage());
        };
    }

    /// <summary>
    /// Update UI state based on corresponding component.
    /// </summary>
    public void Update(Entity<AnalysisConsoleComponent> ent)
    {
        _consoleMenu?.Update(ent);
    }
}
