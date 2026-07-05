namespace OneLetterShor.HideAndSeek.Compat;

/// <summary>
/// Contains information such as GUIDs and <see cref="ModManager.Mod"/>s that other mods do not to provide publicly.
/// </summary>
internal static class ExtraInfo
{
    internal const string RainMeadowGuidRainWorld = "henpemaz_rainmeadow";
    internal const string RainMeadowGuidBepInEx = "henpemaz.rainmeadow";
    internal static ModManager.Mod RainMeadowMod { get; set; } = null!;
}