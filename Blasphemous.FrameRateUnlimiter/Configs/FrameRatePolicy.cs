using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Blasphemous.FrameRateUnlimiter.Tests")]

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
    /// Resolves a target without retaining state between calls.
    /// </summary>
    /// <param name="config">The loaded user configuration.</param>
    /// <param name="performanceObservations">Average FPS values, one for each completed three-second window.</param>
    /// <param name="vSyncCount">The current Unity VSync count.</param>
    /// <returns>The requested target and any configuration warning.</returns>
    public static FrameRatePolicyResult Resolve(
        FrameRateConfig config,
        IEnumerable<float> performanceObservations,
        int vSyncCount) =>
        new FrameRatePolicyState().Resolve(config, performanceObservations, vSyncCount);
}

internal sealed class FrameRatePolicyState
{
    private const float ObservationWindowSeconds = 3f;
    private const int TargetFrameRateStep = 5;

    private int _currentTargetFrameRate = FrameRatePolicy.DefaultTargetFrameRate;
    private bool _isVariableMode;
    private float _observationWindowDuration;
    private int _observationWindowFrames;

    /// <summary>
    /// Resolves the configured target and processes average-FPS observations from completed three-second windows.
    /// </summary>
    /// <param name="config">The loaded user configuration.</param>
    /// <param name="performanceObservations">Average FPS values, one for each completed three-second window.</param>
    /// <param name="vSyncCount">The current Unity VSync count.</param>
    /// <returns>The positive target, or Fixed 60 and a warning when configuration is invalid.</returns>
    internal FrameRatePolicyResult Resolve(
        FrameRateConfig config,
        IEnumerable<float> performanceObservations,
        int vSyncCount)
    {
        if (config != null && config.Mode == "Fixed" && config.TargetFrameRate > 0)
        {
            _currentTargetFrameRate = config.TargetFrameRate;
            ResetAdaptiveState();
            return new FrameRatePolicyResult(_currentTargetFrameRate, null, false);
        }

        if (config != null && config.Mode == "Variable" &&
            config.LowFrameRate > 0 && config.HighFrameRate > 0)
        {
            var configurationWasNormalized = config.LowFrameRate > config.HighFrameRate;
            if (configurationWasNormalized)
            {
                (config.HighFrameRate, config.LowFrameRate) = (config.LowFrameRate, config.HighFrameRate);
            }

            if (!_isVariableMode)
            {
                _currentTargetFrameRate = config.HighFrameRate;
            }

            _currentTargetFrameRate = System.Math.Max(
                config.LowFrameRate,
                System.Math.Min(config.HighFrameRate, _currentTargetFrameRate));
            _isVariableMode = true;
            if (vSyncCount == 0 && performanceObservations != null)
            {
                foreach (var averageFrameRate in performanceObservations)
                {
                    if (averageFrameRate < 0f || float.IsNaN(averageFrameRate) || float.IsInfinity(averageFrameRate))
                    {
                        continue;
                    }

                    if (averageFrameRate < _currentTargetFrameRate * 0.9f)
                    {
                        _currentTargetFrameRate = System.Math.Max(
                            config.LowFrameRate,
                            _currentTargetFrameRate - TargetFrameRateStep);
                    }
                    else if (averageFrameRate >= _currentTargetFrameRate)
                    {
                        _currentTargetFrameRate = _currentTargetFrameRate > config.HighFrameRate - TargetFrameRateStep
                            ? config.HighFrameRate
                            : _currentTargetFrameRate + TargetFrameRateStep;
                    }
                }
            }

            return new FrameRatePolicyResult(_currentTargetFrameRate, null, configurationWasNormalized);
        }

        _currentTargetFrameRate = FrameRatePolicy.DefaultTargetFrameRate;
        ResetAdaptiveState();
        return new FrameRatePolicyResult(
            FrameRatePolicy.DefaultTargetFrameRate,
            "Invalid frame-rate configuration. Using Fixed mode at 60 FPS.",
            false);
    }

    /// <summary>
    /// Adds one frame to the current measurement window and updates the policy when three seconds are available.
    /// </summary>
    /// <param name="config">The loaded user configuration.</param>
    /// <param name="unscaledDeltaTime">The elapsed unscaled time for this frame.</param>
    /// <param name="vSyncCount">The current Unity VSync count.</param>
    /// <returns>The requested target and any configuration warning.</returns>
    internal FrameRatePolicyResult ObserveFrame(FrameRateConfig config, float unscaledDeltaTime, int vSyncCount)
    {
        var currentResult = Resolve(config, null, vSyncCount);
        if (vSyncCount > 0 || config == null || config.Mode != "Variable" || currentResult.Warning != null)
        {
            ResetObservationWindow();
            return currentResult;
        }

        if (unscaledDeltaTime > 0f && !float.IsNaN(unscaledDeltaTime) && !float.IsInfinity(unscaledDeltaTime))
        {
            _observationWindowDuration += unscaledDeltaTime;
            _observationWindowFrames++;
        }

        if (_observationWindowDuration < ObservationWindowSeconds)
        {
            return currentResult;
        }

        var averageFrameRate = _observationWindowFrames / _observationWindowDuration;
        ResetObservationWindow();
        return Resolve(config, [averageFrameRate], vSyncCount);
    }

    private void ResetAdaptiveState()
    {
        _isVariableMode = false;
    }

    private void ResetObservationWindow()
    {
        _observationWindowDuration = 0f;
        _observationWindowFrames = 0;
    }
}

/// <summary>
/// The target and optional warning produced by <see cref="FrameRatePolicy"/>.
/// </summary>
public sealed class FrameRatePolicyResult
{
    internal FrameRatePolicyResult(int targetFrameRate, string warning, bool configurationWasNormalized)
    {
        TargetFrameRate = targetFrameRate;
        Warning = warning;
        ConfigurationWasNormalized = configurationWasNormalized;
    }

    /// <summary>
    /// Gets the resolved positive frame rate in frames per second.
    /// </summary>
    public int TargetFrameRate { get; }

    /// <summary>
    /// Gets a warning when invalid configuration was replaced with Fixed 60.
    /// </summary>
    public string Warning { get; }

    /// <summary>
    /// Gets whether the configuration was corrected and should be saved.
    /// </summary>
    public bool ConfigurationWasNormalized { get; }
}