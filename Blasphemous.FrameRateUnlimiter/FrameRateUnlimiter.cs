using Blasphemous.ModdingAPI;

using Blasphemous.FrameRateUnlimiter.Configs;
using UnityEngine;

namespace Blasphemous.FrameRateUnlimiter;

public class FrameRateUnlimiter : BlasMod
{
    private FrameRateConfig _config = new();

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

        var result = FrameRatePolicy.Resolve(_config, null, QualitySettings.vSyncCount);
        if (result.Warning != null)
        {
            ModLog.Warn(result.Warning, this);
            _config = new FrameRateConfig();
        }
    }

    internal int ResolveTargetFrameRate(int vSyncCount) =>
        FrameRatePolicy.Resolve(_config, null, vSyncCount).TargetFrameRate;
}
