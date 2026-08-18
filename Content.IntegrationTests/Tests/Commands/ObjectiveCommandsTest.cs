#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Objectives;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests.Commands;

public sealed class ObjectiveCommandsTest : GameTest
{

    private const string ObjectiveProtoId = "MindCommandsTestObjective";
    private const string DummyUsername = "MindCommandsTestUser";

    [TestPrototypes]
    private const string Prototypes = $"""
- type: entity
  id: {ObjectiveProtoId}
  components:
  - type: Objective
    difficulty: 1
    issuer: TheSyndicate
    icon:
      sprite: error.rsi
      state: error
  - type: DieCondition
""";

    public override PoolSettings PoolSettings => new ()
    {
        Connected = false
    };

    /// <summary>
    /// Creates a dummy session, and assigns it a mind, then
    /// tests using <c>addobjective</c>, <c>lsobjectives</c>,
    /// and <c>rmobjective</c> on it.
    /// </summary>
    [Test]
    public async Task AddListRemoveObjectiveTest()
    {
        var playerMan = Server.ResolveDependency<ISharedPlayerManager>();
        var mindSys = Server.System<SharedMindSystem>();
        var objectivesSystem = Server.System<ObjectivesSystem>();

        await Server.AddDummySession(DummyUsername);
        await Server.WaitRunTicks(5);

        var playerSession = playerMan.Sessions.Single();

        Entity<MindComponent>? mindEnt = null;
        await Server.WaitPost(() =>
        {
            mindEnt = mindSys.CreateMind(playerSession.UserId);
        });

        Assert.That(mindEnt, Is.Not.Null);
        var mindComp = mindEnt!.Value.Comp;
        Assert.That(mindComp.Objectives, Is.Empty, "Dummy player started with objectives.");

        await Pair.WaitCommand($"addobjective {playerSession.Name} {ObjectiveProtoId}");

        Assert.That(mindComp.Objectives, Has.Count.EqualTo(1), "addobjective failed to increase Objectives count.");

        await Pair.WaitCommand($"lsobjectives {playerSession.Name}");

        await Pair.WaitCommand($"rmobjective {playerSession.Name} 0");

        Assert.That(mindComp.Objectives, Is.Empty, "rmobjective failed to remove objective");

        await Server.WaitPost(() => { SEntMan.DeleteEntity(mindEnt); });
    }
}
