using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Buckle;

public sealed partial class BuckleTest
{
    [Test]
    public async Task BuckleInteractUnbuckleOther()
    {
        var buckleSystem = Server.System<SharedBuckleSystem>();

        EntityUid user = default;
        EntityUid victim = default;
        EntityUid chair = default;
        BuckleComponent buckle = null;
        StrapComponent strap = null;

        await Server.WaitAssertion(() =>
        {
            user = SSpawn(BuckleDummyId);
            victim = SSpawn(BuckleDummyId);
            chair = SSpawn(StrapDummyId);

            Assert.That(SEntMan.TryGetComponent(victim, out buckle));
            Assert.That(SEntMan.TryGetComponent(chair, out strap));

#pragma warning disable RA0002
            buckle.Delay = TimeSpan.Zero;
#pragma warning restore RA0002

            // Buckle victim to chair
            Assert.That(buckleSystem.TryBuckle(victim, user, chair, buckle));
            Assert.Multiple(() =>
            {
                Assert.That(buckle.BuckledTo, Is.EqualTo(chair), "Victim did not get buckled to the chair.");
                Assert.That(buckle.Buckled, "Victim is not buckled.");
                Assert.That(strap.BuckledEntities, Does.Contain(victim), "Chair does not have victim buckled to it.");
            });

            // InteractHand with chair to unbuckle victim
            SEntMan.EventBus.RaiseLocalEvent(chair, new InteractHandEvent(user, chair));
            Assert.Multiple(() =>
            {
                Assert.That(buckle.BuckledTo, Is.Null);
                Assert.That(buckle.Buckled, Is.False);
                Assert.That(strap.BuckledEntities, Does.Not.Contain(victim));
            });
        });
    }

    [Test]
    public async Task BuckleInteractBuckleUnbuckleSelf()
    {
        EntityUid user = default;
        EntityUid chair = default;
        BuckleComponent buckle = null;
        StrapComponent strap = null;

        await Server.WaitAssertion(() =>
        {
            user = SSpawn(BuckleDummyId);
            chair = SSpawn(StrapDummyId);

            Assert.That(SEntMan.TryGetComponent(user, out buckle));
            Assert.That(SEntMan.TryGetComponent(chair, out strap));

#pragma warning disable RA0002
            buckle.Delay = TimeSpan.Zero;
#pragma warning restore RA0002

            // Buckle user to chair
            SEntMan.EventBus.RaiseLocalEvent(chair, new InteractHandEvent(user, chair));
            Assert.Multiple(() =>
            {
                Assert.That(buckle.BuckledTo, Is.EqualTo(chair), "Victim did not get buckled to the chair.");
                Assert.That(buckle.Buckled, "Victim is not buckled.");
                Assert.That(strap.BuckledEntities, Does.Contain(user), "Chair does not have victim buckled to it.");
            });

            // InteractHand with chair to unbuckle
            SEntMan.EventBus.RaiseLocalEvent(chair, new InteractHandEvent(user, chair));
            Assert.Multiple(() =>
            {
                Assert.That(buckle.BuckledTo, Is.Null);
                Assert.That(buckle.Buckled, Is.False);
                Assert.That(strap.BuckledEntities, Does.Not.Contain(user));
            });
        });
    }
}
