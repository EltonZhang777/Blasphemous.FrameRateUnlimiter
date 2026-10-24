using Blasphemous.FrameRateUnlimiter.Configs;
using Xunit;

namespace Blasphemous.FrameRateUnlimiter.Tests;

public sealed class FrameRatePolicyTests
{
    [Fact]
    public void NewConfigurationDefaultsToFixed60()
    {
        var config = new FrameRateConfig();

        Assert.Equal("Fixed", config.Mode);
        Assert.Equal(60, config.TargetFrameRate);
        Assert.Equal(30, config.LowFrameRate);
        Assert.Equal(120, config.HighFrameRate);

        var result = Resolve(config, [], 0);

        Assert.Equal(60, result.TargetFrameRate);
        Assert.Null(result.Warning);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    [InlineData(int.MaxValue)]
    public void AcceptsAnyPositiveIntTarget(int targetFrameRate)
    {
        var result = Resolve(
            new FrameRateConfig { TargetFrameRate = targetFrameRate },
            [12f, 60f, 240f],
            1);

        Assert.Equal(targetFrameRate, result.TargetFrameRate);
        Assert.Null(result.Warning);
    }

    [Fact]
    public void InvalidCustomConfigurationsFallBackToFixed60WithWarning()
    {
        var invalidConfigurations = new[]
        {
            new FrameRateConfig { Mode = "Adaptive", TargetFrameRate = 120 },
            new FrameRateConfig { Mode = "Fixed", TargetFrameRate = 0 },
            new FrameRateConfig { Mode = "Fixed", TargetFrameRate = -1 },
            new FrameRateConfig { Mode = "Variable", LowFrameRate = 0, HighFrameRate = 60 },
            new FrameRateConfig { Mode = "Variable", LowFrameRate = 30, HighFrameRate = 0 },
        };

        foreach (var config in invalidConfigurations)
        {
            var result = Resolve(config, [], 0);

            Assert.Equal(60, result.TargetFrameRate);
            Assert.False(string.IsNullOrWhiteSpace(result.Warning));
        }
    }

    [Theory]
    [InlineData(0, 1f, 240f)]
    [InlineData(1, 240f, 1f)]
    [InlineData(2, 60f, 30f)]
    public void FixedTargetIgnoresPerformanceObservationsAndVSyncState(
        int vSyncCount,
        float firstObservation,
        float secondObservation)
    {
        var config = new FrameRateConfig { TargetFrameRate = 144 };
        var observations = new[] { firstObservation, secondObservation };

        var result = Resolve(config, observations, vSyncCount);

        Assert.Equal(144, result.TargetFrameRate);
        Assert.Null(result.Warning);
    }

    [Theory]
    [InlineData(30, 90)]
    [InlineData(45, 45)]
    public void VariableModeStartsAtHighIncludingWhenEndpointsAreEqual(int lowFrameRate, int highFrameRate)
    {
        var policy = new FrameRatePolicyState();
        var config = new FrameRateConfig
        {
            Mode = "Variable",
            LowFrameRate = lowFrameRate,
            HighFrameRate = highFrameRate,
        };

        var result = policy.Resolve(config, [], 0);

        Assert.Equal(highFrameRate, result.TargetFrameRate);
        Assert.Null(result.Warning);
    }

    [Fact]
    public void ReversedVariableBoundsAreSwappedAndMarkedForPersistence()
    {
        var policy = new FrameRatePolicyState();
        var config = new FrameRateConfig
        {
            Mode = "Variable",
            LowFrameRate = 90,
            HighFrameRate = 30,
        };

        var result = policy.Resolve(config, [], 0);

        Assert.Equal(30, config.LowFrameRate);
        Assert.Equal(90, config.HighFrameRate);
        Assert.Equal(90, result.TargetFrameRate);
        Assert.True(result.ConfigurationWasNormalized);
        Assert.Null(result.Warning);
    }

    [Fact]
    public void VariableModeChangesOnEveryThreeSecondWindowWithoutExtraWait()
    {
        var policy = new FrameRatePolicyState();
        var config = new FrameRateConfig { Mode = "Variable", LowFrameRate = 20, HighFrameRate = 50 };

        var result = policy.Resolve(config, [44f], 0);
        Assert.Equal(45, result.TargetFrameRate);

        result = policy.Resolve(config, [45f], 0);
        Assert.Equal(50, result.TargetFrameRate);

        result = policy.Resolve(config, [44f], 0);

        Assert.Equal(45, result.TargetFrameRate);
        Assert.Null(result.Warning);
    }

    [Fact]
    public void VariableModeDecreasesAfterOneThreeSecondWindow()
    {
        var policy = new FrameRatePolicyState();
        var config = new FrameRateConfig { Mode = "Variable", LowFrameRate = 20, HighFrameRate = 40 };
        var result = policy.Resolve(config, [35f], 0);

        Assert.Equal(35, result.TargetFrameRate);
        Assert.Null(result.Warning);
    }

    [Fact]
    public void VariableModeClampsChangesToBothInclusiveEndpoints()
    {
        var policy = new FrameRatePolicyState();
        var config = new FrameRateConfig { Mode = "Variable", LowFrameRate = 20, HighFrameRate = 22 };

        var result = policy.Resolve(config, [17f], 0);
        Assert.Equal(20, result.TargetFrameRate);

        result = policy.Resolve(config, [20f], 0);
        Assert.Equal(22, result.TargetFrameRate);

        result = policy.Resolve(config, [22f], 0);

        Assert.Equal(22, result.TargetFrameRate);
        Assert.InRange(result.TargetFrameRate, config.LowFrameRate, config.HighFrameRate);
    }

    [Fact]
    public void VariableModeCanReachIntMaxWithoutOverflow()
    {
        var policy = new FrameRatePolicyState();
        var config = new FrameRateConfig
        {
            Mode = "Variable",
            LowFrameRate = int.MaxValue - 10,
            HighFrameRate = int.MaxValue,
        };

        var result = policy.Resolve(config, [0f], 0);
        Assert.Equal(int.MaxValue - 5, result.TargetFrameRate);

        result = policy.Resolve(config, [float.MaxValue], 0);

        Assert.Equal(int.MaxValue, result.TargetFrameRate);
    }

    [Fact]
    public void VariableModePausesDuringVSyncAndResumesAfterItIsDisabled()
    {
        var policy = new FrameRatePolicyState();
        var config = new FrameRateConfig { Mode = "Variable", LowFrameRate = 20, HighFrameRate = 40 };
        var result = policy.Resolve(config, [35f], 0);
        Assert.Equal(35, result.TargetFrameRate);

        result = policy.Resolve(config, [0f], 1);
        Assert.Equal(35, result.TargetFrameRate);

        result = policy.Resolve(config, [30f], 0);

        Assert.Equal(30, result.TargetFrameRate);
    }

    [Fact]
    public void FrameSamplingUsesThreeSecondWindowsAndRestartsAfterVSync()
    {
        var policy = new FrameRatePolicyState();
        var config = new FrameRateConfig { Mode = "Variable", LowFrameRate = 2, HighFrameRate = 20 };
        var result = policy.ObserveFrame(config, 0.5f, 0);

        for (var frame = 1; frame < 6; frame++)
        {
            result = policy.ObserveFrame(config, 0.5f, 0);
        }
        Assert.Equal(15, result.TargetFrameRate);

        for (var frame = 0; frame < 20; frame++)
        {
            result = policy.ObserveFrame(config, 0.5f, 1);
        }
        Assert.Equal(15, result.TargetFrameRate);

        for (var frame = 0; frame < 5; frame++)
        {
            result = policy.ObserveFrame(config, 0.5f, 0);
        }
        Assert.Equal(15, result.TargetFrameRate);

        result = policy.ObserveFrame(config, 0.5f, 0);

        Assert.Equal(10, result.TargetFrameRate);
    }

    private static FrameRatePolicyResult Resolve(
        FrameRateConfig config,
        IEnumerable<float> observations,
        int vSyncCount) =>
        FrameRatePolicy.Resolve(config, observations, vSyncCount);
}