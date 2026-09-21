using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Content.Client.Lathe.UI;
using Content.IntegrationTests;
using Content.IntegrationTests.Pair;
using Content.Shared.Research.Prototypes;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Benchmarks;

/// <summary>
/// Benchmarks the performance of populating the LatheMenu.
/// </summary>
[Virtual]
[GcServer(true)]
[MemoryDiagnoser]
public class LatheMenuBenchmark
{
    [Params(1, 10, 100)]
    // [Params(1)]
    public int Iterations = 1;
    private TestPair _pair = default!;

    // Everything you can normally print in an autolathe.
    // Should really be gotten through the static packs and not defined statically.
    private readonly List<ProtoId<LatheRecipePrototype>> _protos = new()
    {
        "VoiceSensor",
        "Wirecutter",
        "Screwdriver",
        "Welder",
        "Wrench",
        "CrowbarGreen",
        "Multitool",
        "NetworkConfigurator",
        "Signaller",
        "SprayPainter",
        "SprayPainterAmmo",
        "FlashlightLantern",
        "HandheldGPSBasic",
        "TRayScanner",
        "UtilityBelt",
        "ClothingOuterVestTank",
        "HandheldStationMap",
        "ClothingHeadHatWelding",
        "ClothingHeadHatCone",
        "Igniter",
        "ModularReceiver",
        "MicroManipulatorStockPart",
        "ConveyorBeltAssembly",
        "AirTank",
        "GasAnalyzer",
        "CableStack",
        "CableMVStack",
        "CableHVStack",
        "BoxFolderClipboardEmpty",
        "BoxFolderPlasticClipboardEmpty",
        "TowelColorWhite",
        "AppraisalTool",
        "Pickaxe",
        "UtilityKnife",
        "SheetRGlass",
        "MaterialDurathread",
        "Beaker",
        "LargeBeaker",
        "Syringe",
        "PillCanister",
        "HandLabeler",
        "ChemistryEmptyVial",
        "ChemistryEmptyVialSmall",
        "Dropper",
        "RollerBedSpawnFolded",
        "CheapRollerBedSpawnFolded",
        "EmergencyRollerBedSpawnFolded",
        "LightTube",
        "LedLightTube",
        "SodiumLightTube",
        "ExteriorLightTube",
        "LightBulb",
        "LedLightBulb",
        "DimLightBulb",
        "WarmLightBulb",
        "Bucket",
        "Ashtray",
        "DrinkMug",
        "DrinkMugMetal",
        "DrinkGlass",
        "DrinkShotGlass",
        "DrinkGlassCoupeShaped",
        "CustomDrinkJug",
        "FoodPlate",
        "FoodPlateSmall",
        "FoodPlatePlastic",
        "FoodPlateSmallPlastic",
        "FoodBowlBig",
        "FoodPlateTin",
        "FoodPlateMuffinTin",
        "FoodKebabSkewer",
        "SprayBottle",
        "TrashBag",
        "LightReplacer",
        "MopItem",
        "Holoprojector",
        "WetFloorSign",
        "WireBrush",
        "PowerCellSmall",
        "PowerCellMedium",
        "IntercomElectronics",
        "FirelockElectronics",
        "DoorElectronics",
        "AirAlarmElectronics",
        "StationMapElectronics",
        "FireAlarmElectronics",
        "MailingUnitElectronics",
        "SignalTimerElectronics",
        "APCElectronics",
        "SMESMachineCircuitboard",
        "SubstationMachineCircuitboard",
        "WallmountSubstationElectronics",
        "CellRechargerCircuitboard",
        "WeaponCapacitorRechargerCircuitboard",
        "FreezerElectronics",
    };

    // The menu to populate.
    private LatheMenu _latheMenu = default!;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        ProgramShared.PathOffset = "../../../../";
        PoolManager.Startup();
        _pair = await PoolManager.GetServerClient(testContext: new ExternalTestContext("Benchmark", StreamWriter.Null));
        await _pair.Connect();
        var server = _pair.Server;

        // Create test map and grid
        var mapData = await _pair.CreateTestMap();
        var testGrid = mapData.Grid;

        NetEntity nLathe = default;

        await server.WaitPost(() =>
        {
            var entMan = server.ResolveDependency<IEntityManager>();
            var sLathe = server.EntMan.SpawnAttachedTo("Autolathe", new(testGrid, Vector2.Zero));
            nLathe = server.EntMan.GetNetEntity(sLathe);
        });

        await _pair.RunUntilSynced();

        await _pair.Client.WaitPost(() =>
        {
            var cLathe = _pair.Client.EntMan.GetEntity(nLathe);
            _latheMenu = new();
            _latheMenu.SetEntity(cLathe);
            _latheMenu.Recipes = _protos.ToList(); // Clone it.
        });
    }

    [Benchmark]
    public async Task TestPopulate()
    {
        await _pair.Client.WaitPost(() =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                _latheMenu.PopulateRecipes();
            }
        });
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        await _pair.DisposeAsync();
        PoolManager.Shutdown();
    }
}
