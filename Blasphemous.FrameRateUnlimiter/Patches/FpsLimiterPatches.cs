using Blasphemous.FrameRateUnlimiter.Configs;
using Framework.FrameworkCore;
using HarmonyLib;
using UnityEngine;

namespace Blasphemous.FrameRateUnlimiter.Patches;

internal static class FpsLimiterTarget
{
    internal static int Resolve() => Main.FrameRateUnlimiter == null
        ? FrameRatePolicy.DefaultTargetFrameRate
        : Main.FrameRateUnlimiter.ResolveTargetFrameRate(QualitySettings.vSyncCount);

    internal static int ObserveFrame(float unscaledDeltaTime) => Main.FrameRateUnlimiter == null
        ? FrameRatePolicy.DefaultTargetFrameRate
        : Main.FrameRateUnlimiter.ObserveFrame(unscaledDeltaTime, QualitySettings.vSyncCount);
}

[HarmonyPatch(typeof(FpsLimiter), "UpdateTargetDeltaTime")]
internal static class FpsLimiter_UpdateTargetDeltaTime_Patch
{
    private static void Prefix(FpsLimiter __instance)
    {
        __instance.TargetFrameRate = FpsLimiterTarget.Resolve();
    }
}

[HarmonyPatch(typeof(FpsLimiter), "SetTargetFrameRate")]
internal static class FpsLimiter_SetTargetFrameRate_Patch
{
    private static void Prefix(ref int targetFrameRate)
    {
        if (QualitySettings.vSyncCount == 0)
        {
            targetFrameRate = FpsLimiterTarget.Resolve();
        }
    }
}

[HarmonyPatch(typeof(FpsLimiter), "LateUpdate")]
internal static class FpsLimiter_LateUpdate_Patch
{
    private static void Prefix(FpsLimiter __instance, ref float ___TargetDeltaTime)
    {
        var targetFrameRate = FpsLimiterTarget.ObserveFrame(Time.unscaledDeltaTime);
        __instance.TargetFrameRate = targetFrameRate;

        if (QualitySettings.vSyncCount > 0)
        {
            ___TargetDeltaTime = 1f / targetFrameRate;
        }
    }
}
