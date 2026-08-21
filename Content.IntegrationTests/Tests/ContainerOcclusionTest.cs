using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.Storage.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests
{
    public sealed class ContainerOcclusionTest : GameTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  id: ContainerOcclusionA
  components:
  - type: EntityStorage
    occludesLight: true

- type: entity
  id: ContainerOcclusionB
  components:
  - type: EntityStorage
    showContents: true
    occludesLight: false

- type: entity
  id: ContainerOcclusionDummy
  components:
  - type: Sprite
  - type: PointLight
";

        [Test]
        public async Task TestA()
        {
            EntityUid dummy = default;
            var map = await Pair.CreateTestMap();
            var mapSys = SEntMan.System<SharedMapSystem>();

            await Server.WaitPost(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, map.MapId);
                var entStorage = SEntMan.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var container = SEntMan.SpawnEntity("ContainerOcclusionA", pos);
                dummy = SEntMan.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(dummy, container);
            });

            await Pair.RunTicksSync(5);

            var clientEnt = CEntMan.GetEntity(SEntMan.GetNetEntity(dummy));

            await Client.WaitAssertion(() =>
            {
                var sprite = CEntMan.GetComponent<SpriteComponent>(clientEnt);
                var light = CEntMan.GetComponent<PointLightComponent>(clientEnt);
                Assert.Multiple(() =>
                {
                    Assert.That(sprite.ContainerOccluded);
                    Assert.That(light.ContainerOccluded);
                });
            });

            await Server.WaitPost(() =>
            {
                mapSys.DeleteMap(map.MapId);
            });
        }

        [Test]
        public async Task TestB()
        {
            EntityUid dummy = default;
            var mapSys = SEntMan.System<SharedMapSystem>();

            var map = await Pair.CreateTestMap();

            await Server.WaitPost(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, map.MapId);
                var entStorage = SEntMan.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var container = SEntMan.SpawnEntity("ContainerOcclusionB", pos);
                dummy = SEntMan.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(dummy, container);
            });

            await Pair.RunTicksSync(5);

            var clientEnt = CEntMan.GetEntity(SEntMan.GetNetEntity(dummy));

            await Client.WaitAssertion(() =>
            {
                var sprite = CEntMan.GetComponent<SpriteComponent>(clientEnt);
                var light = CEntMan.GetComponent<PointLightComponent>(clientEnt);
                Assert.Multiple(() =>
                {
                    Assert.That(sprite.ContainerOccluded, Is.False);
                    Assert.That(light.ContainerOccluded, Is.False);
                });
            });

            await Server.WaitPost(() =>
            {
                mapSys.DeleteMap(map.MapId);
            });
        }

        [Test]
        public async Task TestAb()
        {
            EntityUid dummy = default;
            var mapSys = SEntMan.System<SharedMapSystem>();

            var map = await Pair.CreateTestMap();

            await Server.WaitPost(() =>
            {
                var pos = new MapCoordinates(Vector2.Zero, map.MapId);
                var entStorage = SEntMan.EntitySysManager.GetEntitySystem<EntityStorageSystem>();
                var containerA = SEntMan.SpawnEntity("ContainerOcclusionA", pos);
                var containerB = SEntMan.SpawnEntity("ContainerOcclusionB", pos);
                dummy = SEntMan.SpawnEntity("ContainerOcclusionDummy", pos);

                entStorage.Insert(containerB, containerA);
                entStorage.Insert(dummy, containerB);
            });

            await Pair.RunTicksSync(5);

            var clientEnt = CEntMan.GetEntity(SEntMan.GetNetEntity(dummy));

            await Client.WaitAssertion(() =>
            {
                var sprite = CEntMan.GetComponent<SpriteComponent>(clientEnt);
                var light = CEntMan.GetComponent<PointLightComponent>(clientEnt);
                Assert.Multiple(() =>
                {
                    Assert.That(sprite.ContainerOccluded);
                    Assert.That(light.ContainerOccluded);
                });
            });

            await Server.WaitPost(() =>
            {
                mapSys.DeleteMap(map.MapId);
            });
        }
    }
}
