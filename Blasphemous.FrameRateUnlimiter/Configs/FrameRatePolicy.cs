using System.Collections.Generic;

namespace Blasphemous.FrameRateUnlimiter.Configs;

/// <summary>
/// Resolves a frame-rate target from configuration and runtime observations.
/// </summary>
public static class FrameRatePolicy
{
    /// <summary>
    /// The frame rate used for first-run settings and invalid configuration.
    /// </summary>
    public const int DefaultTargetFrameRate = 60;

    /// <summary>
    /// Resolves the configured target. The Fixed mode is independent of performance observations and VSync state.
    /// </summary>
    /// <param name="config">The loaded user configuration.</param>
    /// <param name="performanceObservations">Runtime observations available to other policy modes.</param>
    /// <param name="vSyncCount">The current Unity VSync count.</param>
    /// <returns>The positive target, or Fixed 60 and a warning when configuration is invalid.</returns>
    public static FrameRatePolicyResult Resolve(
        FrameRateConfig config,
        IEnumerable<float> performanceObservations,
        int vSyncCount)
    {
        if (config == null || config.Mode != "Fixed" || config.TargetFrameRate <= 0)
        {
            return new FrameRatePolicyResult(
                DefaultTargetFrameRate,
                "Invalid frame-rate configuration. Using Fixed mode at 60 FPS.");
        }

        return new FrameRatePolicyResult(config.TargetFrameRate, null);
    }
}

/// <summary>
/// The target and optional warning produced by <see cref="FrameRatePolicy"/>.
/// </summary>
public sealed class FrameRatePolicyResult
{
    internal FrameRatePolicyResult(int targetFrameRate, string warning)
    {
        TargetFrameRate = targetFrameRate;
        Warning = warning;
    }

    /// <summary>
    /// Gets the resolved positive frame rate in frames per second.
    /// </summary>
    public int TargetFrameRate { get; }

    /// <summary>
    /// Gets a warning when invalid configuration was replaced with Fixed 60.
    /// </summary>
    public string Warning { get; }
}
