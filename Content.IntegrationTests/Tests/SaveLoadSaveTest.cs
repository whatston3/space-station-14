#nullable enable
using System.IO;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Events;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests;

/// <summary>
/// Tests that a grid's yaml does not change when saved consecutively.
/// </summary>
public sealed partial class SaveLoadSaveTest : GameTest
{
    [SidedDependency(Side.Server)] private MapLoaderSystem _sMapLoader = default!;
    [SidedDependency(Side.Server)] private SharedMapSystem _sMap = default!;
    [SidedDependency(Side.Server)] private IConfigurationManager _sCfg = default!;
    [SidedDependency(Side.Server)] private SaveLoadSaveTestSystem _sTest = default!;

    [Test]
    public async Task CreateSaveLoadSaveGrid()
    {
        Assume.That(_sCfg.GetCVar(CCVars.GridFill), Is.False);

        _sTest.Enabled = true;

        Assume.That(SEntMan.EntityCount, Is.EqualTo(0), "Lingering entities at the start of CreateSaveLoadSaveGrid");

        var rp1 = new ResPath("/save load save 1.yml");
        var rp2 = new ResPath("/save load save 2.yml");

        MapId mapId0 = MapId.Nullspace;
        MapId mapId1 = MapId.Nullspace;

        await Server.WaitPost(() =>
        {
            _sMap.CreateMap(out mapId0);
            var grid0 = _sMap.CreateGridEntity(mapId0);
            SEntMan.RunMapInit(grid0.Owner, SEntMan.GetComponent<MetaDataComponent>(grid0));
            Assert.That(_sMapLoader.TrySaveGrid(grid0.Owner, rp1));
            _sMap.CreateMap(out mapId1);
            Assert.That(_sMapLoader.TryLoadGrid(mapId1, rp1, out var grid1));
            Assert.That(_sMapLoader.TrySaveGrid(grid1!.Value, rp2));
        });

        var userData = Server.ResolveDependency<IResourceManager>().UserData;

        string one;
        string two;

        await using (var stream = userData.Open(rp1, FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            one = await reader.ReadToEndAsync();
        }

        await using (var stream = userData.Open(rp2, FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            two = await reader.ReadToEndAsync();
        }

        Assert.Multiple(() =>
        {
            Assert.That(two, Is.EqualTo(one));
            var failed = TestContext.CurrentContext.Result.Assertions.FirstOrDefault();
            if (failed != null)
            {
                var oneTmp = Path.GetTempFileName();
                var twoTmp = Path.GetTempFileName();

                File.WriteAllText(oneTmp, one);
                File.WriteAllText(twoTmp, two);

                TestContext.AddTestAttachment(oneTmp, "First save file");
                TestContext.AddTestAttachment(twoTmp, "Second save file");
                TestContext.Error.WriteLine("Complete output:");
                TestContext.Error.WriteLine(oneTmp);
                TestContext.Error.WriteLine(twoTmp);
            }
        });
        _sTest.Enabled = false;
        await Server.WaitPost(() =>
        {
            _sMap.DeleteMap(mapId0);
            _sMap.DeleteMap(mapId1);
        });
        Assert.That(SEntMan.EntityCount.Equals(0), "Lingering entities at the end of CreateSaveLoadSaveGrid");
    }

    private new const string TestMap = "Maps/bagel.yml";

    /// <summary>
    /// Loads the default map, runs it for 5 ticks, then assert that it did not change.
    /// </summary>
    [Test]
    public async Task LoadSaveTicksSaveBagel()
    {
        _sTest.Enabled = true;

        Assume.That(SEntMan.EntityCount.Equals(0), "Lingering entities at the start of LoadSaveTicksSaveBagel");

        var rp1 = new ResPath("/load save ticks save 1.yml");
        var rp2 = new ResPath("/load save ticks save 2.yml");

        MapId mapId = default;
        Assert.That(_sCfg.GetCVar(CCVars.GridFill), Is.False);

        // Load bagel.yml as uninitialized map, and save it to ensure it's up to date.
        await Server.WaitPost(() =>
        {
            var path = new ResPath(TestMap);
            Assert.That(_sMapLoader.TryLoadMap(path, out var map, out _), $"Failed to load test map {TestMap}");
            mapId = map!.Value.Comp.MapId;
            Assert.That(_sMapLoader.TrySaveMap(mapId, rp1));

            // Run 5 ticks.
            Server.RunTicks(5);
        });

        await Server.WaitPost(() =>
        {
            Assert.That(_sMapLoader.TrySaveMap(mapId, rp2));
        });

        var userData = Server.ResolveDependency<IResourceManager>().UserData;

        string one;
        string two;

        await using (var stream = userData.Open(rp1, FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            one = await reader.ReadToEndAsync();
        }

        await using (var stream = userData.Open(rp2, FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            two = await reader.ReadToEndAsync();
        }

        Assert.Multiple(() =>
        {
            Assert.That(two, Is.EqualTo(one));
            var failed = TestContext.CurrentContext.Result.Assertions.FirstOrDefault();
            if (failed != null)
            {
                var oneTmp = Path.GetTempFileName();
                var twoTmp = Path.GetTempFileName();

                File.WriteAllText(oneTmp, one);
                File.WriteAllText(twoTmp, two);

                TestContext.AddTestAttachment(oneTmp, "First save file");
                TestContext.AddTestAttachment(twoTmp, "Second save file");
                TestContext.Error.WriteLine("Complete output:");
                TestContext.Error.WriteLine(oneTmp);
                TestContext.Error.WriteLine(twoTmp);
            }
        });

        _sTest.Enabled = false;
        await Server.WaitPost(() => _sMap.DeleteMap(mapId));
        Assert.That(SEntMan.EntityCount.Equals(0), "Lingering entities at the end of LoadSaveTicksSaveBagel");
    }

    /// <summary>
    /// Loads the same uninitialized map at slightly different times, and then checks that they are the same
    /// when getting saved.
    /// </summary>
    /// <remarks>
    /// Should ensure that entities do not perform randomization prior to initialization and should prevents
    /// bugs like the one discussed in github.com/space-wizards/RobustToolbox/issues/3870. This test is somewhat
    /// similar to <see cref="LoadSaveTicksSaveBagel"/> and <see cref="SaveLoadSave"/>, but neither of these
    /// caught the mentioned bug.
    /// </remarks>
    [Test]
    [Description("Saves Bagel multiple times, checking that the YAML is identical between the two.")]
    public async Task LoadTickLoadBagel()
    {
        var userData = Server.ResolveDependency<IResourceManager>().UserData;
        Assume.That(_sCfg.GetCVar(CCVars.GridFill), Is.False);
        _sTest.Enabled = true;

        Assume.That(SEntMan.EntityCount.Equals(0), "Lingering entities at the start of LoadTickLoadBagel");

        MapId mapId1 = default;
        MapId mapId2 = default;
        var fileA = new ResPath("/load tick load a.yml");
        var fileB = new ResPath("/load tick load b.yml");
        string yamlA;
        string yamlB;

        // Load & save the first map
        await Server.WaitPost(() =>
        {
            var path = new ResPath(TestMap);
            Assert.That(_sMapLoader.TryLoadMap(path, out var map, out _), $"Failed to load test map {TestMap}");
            mapId1 = map!.Value.Comp.MapId;
            Assert.That(_sMapLoader.TrySaveMap(mapId1, fileA));
        });

        await using (var stream = userData.Open(fileA, FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            yamlA = await reader.ReadToEndAsync();
        }

        // Load & save the second map
        await Server.WaitPost(() =>
        {
            Server.RunTicks(5);

            var path = new ResPath(TestMap);
            Assert.That(_sMapLoader.TryLoadMap(path, out var map, out _), $"Failed to load test map {TestMap}");
            mapId2 = map!.Value.Comp.MapId;
            Assert.That(_sMapLoader.TrySaveMap(mapId2, fileB));
        });

        await using (var stream = userData.Open(fileB, FileMode.Open))
        using (var reader = new StreamReader(stream))
        {
            yamlB = await reader.ReadToEndAsync();
        }

        Assert.That(yamlA, Is.EqualTo(yamlB));

        _sTest.Enabled = false;
        await Server.WaitPost(() =>
        {
            _sMap.DeleteMap(mapId1);
            _sMap.DeleteMap(mapId2);
        });
        Assert.That(SEntMan.EntityCount.Equals(0), "Lingering entities at the end of LoadTickLoadBagel");
    }

    /// <summary>
    /// Simple system that modifies the data saved to a yaml file by removing the timestamp.
    /// Required by some tests that validate that re-saving a map does not modify it.
    /// </summary>
    private sealed partial class SaveLoadSaveTestSystem : EntitySystem
    {
        public bool Enabled;
        public override void Initialize()
        {
            SubscribeLocalEvent<AfterSerializationEvent>(OnAfterSave);
        }

        private void OnAfterSave(AfterSerializationEvent ev)
        {
            if (!Enabled)
                return;

            // Remove timestamp.
            ((MappingDataNode)ev.Node["meta"]).Remove("time");
        }
    }
}
