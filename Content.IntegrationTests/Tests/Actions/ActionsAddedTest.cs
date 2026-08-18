#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.GameTicking;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.CombatMode;
using Robust.Server.Player;

namespace Content.IntegrationTests.Tests.Actions;

/// <summary>
/// This tests checks that actions properly get added to an entity's actions component..
/// </summary>
[TestFixture]
public sealed class ActionsAddedTest : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings { Connected = true, DummyTicker = false };

    // TODO add magboot test (inventory action)
    // TODO add ghost toggle-fov test (client-side action)

    [Test]
    public async Task TestCombatActionsAdded()
    {
        var clientSession = Client.Session;
        var serverSession = Server.ResolveDependency<IPlayerManager>().Sessions.Single();
        var sActionSystem = Server.System<SharedActionsSystem>();
        var cActionSystem = Client.System<SharedActionsSystem>();

        // Dummy ticker is disabled - client should be in control of a normal mob.
        Assert.That(serverSession.AttachedEntity, Is.Not.Null);
        var serverEnt = serverSession.AttachedEntity!.Value;
        var clientEnt = clientSession!.AttachedEntity!.Value;
        Assert.That(SEntMan.EntityExists(serverEnt));
        Assert.That(CEntMan.EntityExists(clientEnt));
        Assert.That(SEntMan.HasComponent<ActionsComponent>(serverEnt));
        Assert.That(CEntMan.HasComponent<ActionsComponent>(clientEnt));
        Assert.That(SEntMan.HasComponent<CombatModeComponent>(serverEnt));
        Assert.That(CEntMan.HasComponent<CombatModeComponent>(clientEnt));

        var sComp = SEntMan.GetComponent<ActionsComponent>(serverEnt);
        var cComp = CEntMan.GetComponent<ActionsComponent>(clientEnt);

        // Mob should have a combat-mode action.
        // This action should have a non-null event both on the server & client.
        var evType = typeof(ToggleCombatActionEvent);

        var sQuery = SEntMan.GetEntityQuery<InstantActionComponent>();
        var cQuery = CEntMan.GetEntityQuery<InstantActionComponent>();
        var sActions = sActionSystem.GetActions(serverEnt).Where(
            ent => sQuery.CompOrNull(ent)?.Event?.GetType() == evType).ToArray();
        var cActions = cActionSystem.GetActions(clientEnt).Where(
            ent => cQuery.CompOrNull(ent)?.Event?.GetType() == evType).ToArray();

        Assert.That(sActions.Length, Is.EqualTo(1));
        Assert.That(cActions.Length, Is.EqualTo(1));

        var sAct = sActions[0];
        var cAct = cActions[0];

        Assert.That(sAct.Comp, Is.Not.Null);
        Assert.That(cAct.Comp, Is.Not.Null);

        // Finally, these two actions are not the same object
        // required, because integration tests do not respect the [NonSerialized] attribute and will simply events by reference.
        Assert.That(ReferenceEquals(sAct.Comp, cAct.Comp), Is.False);
        Assert.That(ReferenceEquals(sQuery.GetComponent(sAct).Event, cQuery.GetComponent(cAct).Event), Is.False);

        await Server.WaitPost(() => Server.System<GameTicker>().RestartRound());
    }
}
