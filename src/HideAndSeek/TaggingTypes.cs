namespace OneLetterShor.HideAndSeek;

// TODO: Add documentation.
[Flags]
public enum TaggingTypes : byte
{
    None      = 0,
    Rock      = 1 << 0,
    Contact   = 1 << 1,
    Ascension = 1 << 3,
    All = Rock | Contact | Ascension
}