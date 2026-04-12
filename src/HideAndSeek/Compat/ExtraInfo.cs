namespace OneLetterShor.HideAndSeek.Compat;

/// <summary>
/// Contains info such as GUIDs and <see cref="ModManager.Mod"/>s that hard dependencies neglect to provide publicly.
/// </summary>
internal static class ExtraInfo
{
    internal const string RainMeadowGuidRainWorld = "henpemaz_rainmeadow";
    internal const string RainMeadowGuidBepInEx = "henpemaz.rainmeadow";
    internal static ModManager.Mod RainMeadowMod { get; set; } = null!;
}