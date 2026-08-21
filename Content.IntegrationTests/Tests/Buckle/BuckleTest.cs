using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Buckle;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Buckle
{
    [TestFixture]
    [TestOf(typeof(BuckleComponent))]
    [TestOf(typeof(StrapComponent))]
    public sealed partial class BuckleTest : GameTest
    {
        private const string BuckleDummyId = "BuckleDummy";
        private const string StrapDummyId = "StrapDummy";
        private const string ItemDummyId = "ItemDummy";

        [TestPrototypes]
        private const string Prototypes = $@"
- type: entity
  name: {BuckleDummyId}
  id: {BuckleDummyId}
  components:
  - type: Buckle
  - type: Hands
  - type: ComplexInteraction
  - type: InputMover
  - type: Physics
    bodyType: KinematicController
  - type: Body
    prototype: Human
  - type: StandingState

- type: entity
  name: {StrapDummyId}
  id: {StrapDummyId}
  components:
  - type: Strap

- type: entity
  name: {ItemDummyId}
  id: {ItemDummyId}
  components:
  - type: Item
";

        [Test]
        public async Task BuckleUnbuckleCooldownRangeTest()
        {
            var testMap = await Pair.CreateTestMap();
            var coordinates = testMap.GridCoords;
            var actionBlocker = Server.System<ActionBlockerSystem>();
            var buckleSystem = Server.System<SharedBuckleSystem>();
            var standingState = Server.System<StandingStateSystem>();
            var xformSystem = Server.System<SharedTransformSystem>();

            EntityUid human = default;
            EntityUid chair = default;
            BuckleComponent buckle = null;
            StrapComponent strap = null;

            await Server.WaitAssertion(() =>
            {
                human = SSpawnAtPosition(BuckleDummyId, coordinates);
                chair = SSpawnAtPosition(StrapDummyId, coordinates);

                // Default state, unbuckled
                Assert.That(SEntMan.TryGetComponent(human, out buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle, Is.Not.Null);
                    Assert.That(buckle.BuckledTo, Is.Null);
                    Assert.That(buckle.Buckled, Is.False);
                    Assert.That(actionBlocker.CanMove(human));
                    Assert.That(actionBlocker.CanChangeDirection(human));
                    Assert.That(standingState.Down(human));
                    Assert.That(standingState.Stand(human));
                });

                // Default state, no buckled entities, strap
                Assert.That(SEntMan.TryGetComponent(chair, out strap));
                Assert.Multiple(() =>
                {
                    Assert.That(strap, Is.Not.Null);
                    Assert.That(strap.BuckledEntities, Is.Empty);
                });

                // Side effects of buckling
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Not.Null);
                    Assert.That(buckle.Buckled);

                    Assert.That(actionBlocker.CanMove(human), Is.False);
                    Assert.That(actionBlocker.CanChangeDirection(human));
                    Assert.That(standingState.Down(human), Is.False);
                    Assert.That(
                        (xformSystem.GetWorldPosition(human) - xformSystem.GetWorldPosition(chair)).LengthSquared,
                        Is.LessThanOrEqualTo(0)
                    );

                    // Side effects of buckling for the strap
                    Assert.That(strap.BuckledEntities, Does.Contain(human));
                });

#pragma warning disable NUnit2045 // Interdependent asserts.
                // Trying to buckle while already buckled fails
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckle), Is.False);

                // Trying to unbuckle too quickly fails
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
                Assert.That(buckle.Buckled);
                Assert.That(buckleSystem.TryUnbuckle(human, human), Is.False);
                Assert.That(buckle.Buckled);
#pragma warning restore NUnit2045
            });

            // Wait enough ticks for the unbuckling cooldown to run out
            await Server.WaitRunTicks(60);

            await Server.WaitAssertion(() =>
            {
#pragma warning disable NUnit2045 // Interdependent asserts.
                Assert.That(buckle.Buckled);
                // Still buckled
#pragma warning restore NUnit2045

                // Unbuckle
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Null);
                    Assert.That(buckle.Buckled, Is.False);
                    Assert.That(actionBlocker.CanMove(human));
                    Assert.That(actionBlocker.CanChangeDirection(human));
                    Assert.That(standingState.Down(human));

                    // Unbuckle, strap
                    Assert.That(strap.BuckledEntities, Is.Empty);
                });

#pragma warning disable NUnit2045 // Interdependent asserts.
                // Re-buckling has no cooldown
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.That(buckle.Buckled);

                // On cooldown
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
                Assert.That(buckle.Buckled);
                Assert.That(buckleSystem.TryUnbuckle(human, human), Is.False);
                Assert.That(buckle.Buckled);
                Assert.That(buckleSystem.TryUnbuckle(human, human), Is.False);
                Assert.That(buckle.Buckled);
#pragma warning restore NUnit2045
            });

            // Wait enough ticks for the unbuckling cooldown to run out
            await Server.WaitRunTicks(60);

            await Server.WaitAssertion(() =>
            {
#pragma warning disable NUnit2045 // Interdependent asserts.
                // Still buckled
                Assert.That(buckle.Buckled);

                // Unbuckle
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle));
                Assert.That(buckle.Buckled, Is.False);
#pragma warning restore NUnit2045

                // Move away from the chair
                var oldWorldPosition = xformSystem.GetWorldPosition(chair);
                xformSystem.SetWorldPosition(human, oldWorldPosition + new Vector2(1000, 1000));

                // Out of range
#pragma warning disable NUnit2045 // Interdependent asserts.
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle), Is.False);
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
#pragma warning restore NUnit2045

                // Move near the chair
                oldWorldPosition = xformSystem.GetWorldPosition(chair);
                xformSystem.SetWorldPosition(human, oldWorldPosition + new Vector2(0.5f, 0));

                // In range
#pragma warning disable NUnit2045 // Interdependent asserts.
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.That(buckle.Buckled);
                Assert.That(buckleSystem.TryUnbuckle(human, human, buckleComp: buckle), Is.False);
                Assert.That(buckle.Buckled);
#pragma warning restore NUnit2045

                // Force unbuckle
                buckleSystem.Unbuckle(human, human);
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.Buckled, Is.False);
                    Assert.That(actionBlocker.CanMove(human));
                    Assert.That(actionBlocker.CanChangeDirection(human));
                    Assert.That(standingState.Down(human));
                });

                // Re-buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));

                // Move away from the chair
                oldWorldPosition = xformSystem.GetWorldPosition(chair);
                xformSystem.SetWorldPosition(human, oldWorldPosition + new Vector2(1, 0));
            });

            await Server.WaitRunTicks(1);

            await Server.WaitAssertion(() =>
            {
                // No longer buckled
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.Buckled, Is.False);
                    Assert.That(buckle.BuckledTo, Is.Null);
                    Assert.That(strap.BuckledEntities, Is.Empty);
                });

                Server.System<SharedMapSystem>().DeleteMap(testMap.MapId);
            });
        }

        [Test]
        public async Task BuckledDyingDropItemsTest()
        {
            var testMap = await Pair.CreateTestMap();
            var coordinates = testMap.GridCoords;

            EntityUid human = default;
            BuckleComponent buckle = null;
            HandsComponent hands = null;

            await Server.WaitIdleAsync();

            var handsSys = Server.System<SharedHandsSystem>();
            var buckleSystem = Server.System<SharedBuckleSystem>();
            var xformSystem = Server.System<SharedTransformSystem>();

            await Server.WaitAssertion(() =>
            {
                human = SSpawnAtPosition(BuckleDummyId, coordinates);
                var chair = SSpawnAtPosition(StrapDummyId, coordinates);

                // Component sanity check
                Assert.Multiple(() =>
                {
                    Assert.That(SEntMan.TryGetComponent(human, out buckle));
                    Assert.That(SEntMan.HasComponent<StrapComponent>(chair));
                    Assert.That(SEntMan.TryGetComponent(human, out hands));
                });

                // Buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Not.Null);
                    Assert.That(buckle.Buckled);
                });

                // Put an item into every hand
                for (var i = 0; i < hands.Count; i++)
                {
                    var akms = SSpawnAtPosition(ItemDummyId, coordinates);

                    Assert.That(handsSys.TryPickupAnyHand(human, akms));
                }
            });

            await Server.WaitRunTicks(10);

            await Server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.That(buckle.Buckled);

                // Still with items in hand
                foreach (var hand in hands.Hands.Keys)
                {
                    Assert.That(handsSys.GetHeldItem((human, hands), hand), Is.Not.Null);
                }

                buckleSystem.Unbuckle(human, human);
                Assert.That(buckle.Buckled, Is.False);

                Server.System<SharedMapSystem>().DeleteMap(testMap.MapId);
            });
        }

        [Test]
        public async Task ForceUnbuckleBuckleTest()
        {
            var testMap = await Pair.CreateTestMap();
            var coordinates = testMap.GridCoords;
            var buckleSystem = Server.System<SharedBuckleSystem>();
            var xformSystem = Server.System<SharedTransformSystem>();

            EntityUid human = default;
            EntityUid chair = default;
            BuckleComponent buckle = null;

            await Server.WaitAssertion(() =>
            {
                human = SSpawnAtPosition(BuckleDummyId, coordinates);
                chair = SSpawnAtPosition(StrapDummyId, coordinates);

                // Component sanity check
                Assert.Multiple(() =>
                {
                    Assert.That(SEntMan.TryGetComponent(human, out buckle));
                    Assert.That(SEntMan.HasComponent<StrapComponent>(chair));
                });

                // Buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Not.Null);
                    Assert.That(buckle.Buckled);
                });

                // Move the buckled entity away
                var oldWorldPosition = xformSystem.GetWorldPosition(chair);
                xformSystem.SetWorldPosition(human, oldWorldPosition + new Vector2(100, 0));
            });

            await PoolManager.WaitUntil(Server, () => !buckle.Buckled, 10);

            Assert.That(buckle.Buckled, Is.False);

            await Server.WaitAssertion(() =>
            {
                // Move the now unbuckled entity back onto the chair
                var oldWorldPosition = xformSystem.GetWorldPosition(chair);
                xformSystem.SetWorldPosition(human, oldWorldPosition);

                // Buckle
                Assert.That(buckleSystem.TryBuckle(human, human, chair, buckleComp: buckle));
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Not.Null);
                    Assert.That(buckle.Buckled);
                });
            });

            await Server.WaitRunTicks(60);

            await Server.WaitAssertion(() =>
            {
                // Still buckled
                Assert.Multiple(() =>
                {
                    Assert.That(buckle.BuckledTo, Is.Not.Null);
                    Assert.That(buckle.Buckled);
                });

                Server.System<SharedMapSystem>().DeleteMap(testMap.MapId);
            });
        }
    }
}
