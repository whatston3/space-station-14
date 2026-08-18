using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class ShuttleTest : GameTest
    {
        [Test]
        public async Task Test()
        {
            var physicsSystem = Server.System<SharedPhysicsSystem>();

            PhysicsComponent gridPhys = null;

            var map = await Pair.CreateTestMap();

            await Server.WaitAssertion(() =>
            {
                var mapId = map.MapId;
                var grid = map.Grid;

                Assert.Multiple(() =>
                {
                    Assert.That(SEntMan.HasComponent<ShuttleComponent>(grid));
                    Assert.That(SEntMan.TryGetComponent(grid, out gridPhys));
                });
                Assert.Multiple(() =>
                {
                    Assert.That(gridPhys.BodyType, Is.EqualTo(BodyType.Dynamic));
                    Assert.That(SEntMan.GetComponent<TransformComponent>(grid).LocalPosition, Is.EqualTo(Vector2.Zero));
                });
                physicsSystem.ApplyLinearImpulse(grid, Vector2.One, body: gridPhys);
            });

            await Server.WaitRunTicks(1);

            await Server.WaitAssertion(() =>
            {
                Assert.That(SEntMan.GetComponent<TransformComponent>(map.Grid).LocalPosition, Is.Not.EqualTo(Vector2.Zero));
                Server.System<SharedMapSystem>().DeleteMap(map.MapId);
            });
        }
    }
}
