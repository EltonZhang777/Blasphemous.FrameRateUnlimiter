using BepInEx;

namespace Blasphemous.FrameRateUnlimiter;

[BepInPlugin(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_VERSION)]
[BepInDependency("Blasphemous.ModdingAPI", "0.1.0")]
internal class Main : BaseUnityPlugin
{
    public static FrameRateUnlimiter FrameRateUnlimiter { get; private set; }

    private void Start()
    {
        FrameRateUnlimiter = new FrameRateUnlimiter();
    }
}
