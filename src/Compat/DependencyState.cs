namespace OneLetterShor.HideAndSeek.Compat;

public static class DependencyState
{
    public static SoftDependencies ActiveSoftDependencies { get; private set; } = SoftDependencies.None;
    
    /// <remarks>Primarily for assertions.</remarks>
    public static bool HasCheckedMods { get; private set; } = false;
    
    /// <summary>
    /// Assigns the proper <see cref="ActiveSoftDependencies"/> value based on
    /// mods found in <see cref="ModManager.ActiveMods"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if mods have already been checked.
    /// </exception>
    internal static void CheckMods()
    {
        if (HasCheckedMods) throw new InvalidOperationException("Mods may only be checked once.");
        
        ExtraInfo.RainMeadowMod = ModManager.ActiveMods.Find(mod => mod.id == ExtraInfo.RainMeadowGuidRainWorld);
        Assert(ExtraInfo.RainMeadowMod is not null);
        
        HasCheckedMods = true;
    }
}