using System.Numerics;
using Content.Client.Clickable;
using Content.IntegrationTests.Fixtures;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class ClickableTest : GameTest
    {
        private const double DirSouth = 0;
        private const double DirNorth = Math.PI;
        private const double DirEast = Math.PI / 2;
        private const double DirSouthEast = Math.PI / 4;
        private const double DirSouthEastJustShy = Math.PI / 4 - 0.1;

        [Test]
        [TestCase("ClickTestRotatingCornerVisible", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", 0.35f, 0.5f, DirSouth, 2, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisible", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisible", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", -0.25f, 0.25f, DirEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisible", 0, 0.25f, DirSouthEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0, 0.35f, DirSouthEastJustShy, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerVisibleNoRot", 0.25f, 0.25f, DirSouthEastJustShy, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", 0.35f, 0.5f, DirSouth, 2, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisible", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisible", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", -0.25f, 0.25f, DirEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisible", 0, 0.25f, DirSouthEast, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0.25f, 0.25f, DirSouth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", -0.25f, -0.25f, DirSouth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0.25f, 0.25f, DirNorth, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", -0.25f, -0.25f, DirNorth, 1, ExpectedResult = true)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0, 0.35f, DirSouthEastJustShy, 1, ExpectedResult = false)]
        [TestCase("ClickTestRotatingCornerInvisibleNoRot", 0.25f, 0.25f, DirSouthEastJustShy, 1, ExpectedResult = true)]
        public async Task<bool> Test(string prototype, float clickPosX, float clickPosY, double angle, float scale)
        {
            var pair = Pair;

            var eyeManager = Client.ResolveDependency<IEyeManager>();
            var spriteQuery = CEntMan.GetEntityQuery<SpriteComponent>();
            var eye = Client.ResolveDependency<IEyeManager>().CurrentEye;
            var spriteSys = CEntMan.System<SpriteSystem>();
            var map = SEntMan.System<SharedMapSystem>();

            var testMap = await pair.CreateTestMap();

            EntityUid serverEnt = default;

            await Server.WaitPost(() =>
            {
                serverEnt = SEntMan.SpawnEntity(prototype, testMap.GridCoords);
                SEntMan.System<SharedTransformSystem>().SetWorldRotation(serverEnt, angle);
            });

            // Let client sync up.
            await RunUntilSynced();

            var hit = false;
            var clientEnt = CEntMan.GetEntity(SEntMan.GetNetEntity(serverEnt));

            await Client.WaitPost(() =>
            {
                var sprite = spriteQuery.GetComponent(clientEnt);
                spriteSys.SetScale((clientEnt, sprite), new Vector2(scale, scale));

                // these tests currently all assume player eye is 0
                eyeManager.CurrentEye.Rotation = 0;

                var pos = CEntMan.System<SharedTransformSystem>().GetWorldPosition(clientEnt);

                hit = CEntMan.System<ClickableSystem>().CheckClick((clientEnt, null, sprite, null), new Vector2(clickPosX, clickPosY) + pos, eye, false, out _, out _, out _);
            });

            await Server.WaitPost(() =>
            {
                SEntMan.DeleteEntity(serverEnt);
                map.DeleteMap(testMap.MapId);
            });

            return hit;
        }
    }
}
