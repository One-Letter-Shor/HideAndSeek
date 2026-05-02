namespace OneLetterShor.HideAndSeek;

// TODO: Add documentation.
[Flags]
public enum TaggingMethods : byte
{
    None      = 0,
    Rock      = 1 << 0,
    Contact   = 1 << 1,
    Ascension = 1 << 2,
    All = Rock | Contact | Ascension
}