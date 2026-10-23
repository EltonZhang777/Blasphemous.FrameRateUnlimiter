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

        var result = FrameRatePolicy.Resolve(config, Array.Empty<float>(), 0);

        Assert.Equal(60, result.TargetFrameRate);
        Assert.Null(result.Warning);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    [InlineData(int.MaxValue)]
    public void AcceptsAnyPositiveIntTarget(int targetFrameRate)
    {
        var result = FrameRatePolicy.Resolve(
            new FrameRateConfig { TargetFrameRate = targetFrameRate },
            new[] { 12f, 60f, 240f },
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
        };

        foreach (var config in invalidConfigurations)
        {
            var result = FrameRatePolicy.Resolve(config, Array.Empty<float>(), 0);

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

        var result = FrameRatePolicy.Resolve(config, observations, vSyncCount);

        Assert.Equal(144, result.TargetFrameRate);
        Assert.Null(result.Warning);
    }
}
