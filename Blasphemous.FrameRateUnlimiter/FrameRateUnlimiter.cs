using Blasphemous.FrameRateUnlimiter.Configs;
using Blasphemous.ModdingAPI;
using UnityEngine;

namespace Blasphemous.FrameRateUnlimiter;

public class FrameRateUnlimiter : BlasMod
{
    private FrameRateConfig _config = new();
    private readonly FrameRatePolicyState _policy = new();

    internal FrameRateUnlimiter() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    protected override void OnPreInitialize()
    {
        try
        {
            _config = ConfigHandler.Load<FrameRateConfig>();
        }
        catch (System.Exception exception)
        {
            _config = new FrameRateConfig();
            ModLog.Warn($"Unable to load frame-rate configuration. Using Fixed mode at 60 FPS. {exception.Message}", this);
            return;
        }

        var result = _policy.Resolve(_config, null, QualitySettings.vSyncCount);
        if (result.Warning != null)
        {
            ModLog.Warn(result.Warning, this);
            _config = new FrameRateConfig();
            _policy.Resolve(_config, null, QualitySettings.vSyncCount);
            return;
        }

        if (result.ConfigurationWasNormalized)
        {
            try
            {
                ConfigHandler.Save(_config);
            }
            catch (System.Exception exception)
            {
                ModLog.Warn($"Unable to save corrected frame-rate configuration. {exception.Message}", this);
            }
        }
    }

    internal int ResolveTargetFrameRate(int vSyncCount) =>
        _policy.Resolve(_config, null, vSyncCount).TargetFrameRate;

    internal int ObserveFrame(float unscaledDeltaTime, int vSyncCount) =>
        _policy.ObserveFrame(_config, unscaledDeltaTime, vSyncCount).TargetFrameRate;
}