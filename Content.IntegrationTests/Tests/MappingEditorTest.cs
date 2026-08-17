using Content.Client.Gameplay;
using Content.Client.Mapping;
using Content.IntegrationTests.Fixtures;
using Robust.Client.State;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class MappingEditorTest : GameTest
{
    [Test]
    public async Task StopHardCodingWidgetsJesusChristTest()
    {
        var state = Client.ResolveDependency<IStateManager>();

        await Client.WaitPost(() =>
        {
            Assert.DoesNotThrow(() =>
            {
                state.RequestStateChange<MappingState>();
            });
        });

        // arbitrary short time
        await Client.WaitRunTicks(30);

        await Client.WaitPost(() =>
        {
            Assert.DoesNotThrow(() =>
            {
                state.RequestStateChange<GameplayState>();
            });
        });
    }
}
