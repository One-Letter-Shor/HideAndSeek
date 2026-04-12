using UnityEngine.Assertions;

namespace OneLetterShor.HideAndSeek.Compat;

public static class DependencyState
{
    public static SoftDependencies ActiveSoftDependencies { get; private set; } = SoftDependencies.None;
    
    /// <remarks>Primarily for assertions.</remarks>
    public static bool HasCheckedMods { get; private set; } = false;
    
    /// <summary>
    /// Assigns the proper <see cref="ActiveSoftDependencies"/> value based on
    /// mods found in <see cref="ModManager.ActiveMods"/>. Marks mods as checked.
    /// </summary>
    /// <exception cref="AssertionException">
    /// Thrown if mods have already been checked
    /// or if any hard dependency was not found.
    /// </exception>
    internal static void CheckMods()
    {
        Assert(!HasCheckedMods, "Mods may only be checked once.");
        
        ExtraInfo.RainMeadowMod = ModManager.ActiveMods.Find(mod => mod.id == ExtraInfo.RainMeadowGuidRainWorld);
        Assert(ExtraInfo.RainMeadowMod is not null);
        
        HasCheckedMods = true;
    }
}