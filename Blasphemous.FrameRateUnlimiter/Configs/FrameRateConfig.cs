namespace Blasphemous.FrameRateUnlimiter.Configs;

/// <summary>
/// User settings for the frame-rate policy.
/// </summary>
public sealed class FrameRateConfig
{
    /// <summary>
    /// Gets or sets the selected frame-rate mode.
    /// </summary>
    public string Mode { get; set; } = "Fixed";

    /// <summary>
    /// Gets or sets the requested frame rate in frames per second.
    /// </summary>
    public int TargetFrameRate { get; set; } = 60;
}
