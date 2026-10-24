# Blasphemous.FrameRateUnlimiter

Mod for Blasphemous1 that enables frame rate customization

---

## Configuration

Use config file `Modding/config/Frame Rate Unlimiter.cfg` to customize FPS:

- `Mode`: `Fixed` or `Variable`.
- `TargetFrameRate`: requested target in Fixed mode; the default is 60 FPS.
- `LowFrameRate` and `HighFrameRate`: inclusive Variable-mode range; defaults are 30 and 120 FPS respectively.

Variable mode starts at `HighFrameRate`. Every completed three-second window lowers the request by 5 FPS when average measured FPS is below 90% of the current target, or raises it by 5 FPS when it meets the target. Changes are clamped to the configured range, with no extra delay; VSync pauses adaptation.

`LowFrameRate` is the lowest requested target, not a guarantee of actual FPS.

## Installation

This mod is available for download through the [Blasphemous Mod Installer](https://github.com/BrandenEK/Blasphemous.Modding.Installer)

Required dependencies (dependencies are auto-downloaded if installed through Mod Installer):
- [Modding API](https://github.com/BrandenEK/Blasphemous.ModdingAPI)