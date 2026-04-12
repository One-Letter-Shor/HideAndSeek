namespace OneLetterShor.HideAndSeek.Compat;

/// <summary>
/// Represents mods that this mod can interact with but doesn't require.
/// </summary>
[Flags]
public enum SoftDependencies
{
    None = 0,
    All = None
}