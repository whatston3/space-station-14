using Content.IntegrationTests.Fixtures;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class DeleteInventoryTest : GameTest
    {
        // Test that when deleting an entity with an InventoryComponent,
        // any equipped items also get deleted.
        [Test]
        public async Task Test()
        {
            var testMap = await Pair.CreateTestMap();
            var coordinates = testMap.GridCoords;

            await Server.WaitAssertion(() =>
            {
                // Spawn everything.
                var invSystem = SEntMan.System<InventorySystem>();

                var container = SEntMan.SpawnEntity(null, coordinates);
                SEntMan.EnsureComponent<InventoryComponent>(container);
                SEntMan.EnsureComponent<ContainerManagerComponent>(container);

                var child = SEntMan.SpawnEntity(null, coordinates);
                var item = SEntMan.EnsureComponent<ClothingComponent>(child);

                SEntMan.System<ClothingSystem>().SetSlots(child, SlotFlags.HEAD, item);

                // Equip item.
                Assert.That(invSystem.TryEquip(container, child, "head"), Is.True);

                // Delete parent.
                SEntMan.DeleteEntity(container);

                // Assert that child item was also deleted.
                Assert.That(item.Deleted, Is.True);

                SEntMan.System<SharedMapSystem>().DeleteMap(testMap.MapId);
            });
        }
    }
}
