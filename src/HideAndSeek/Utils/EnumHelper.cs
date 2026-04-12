namespace OneLetterShor.HideAndSeek.Utils;

// TODO: Add documentation for methods.
public static class EnumHelper
{
    internal static bool HasExactlyOneFlag<T>(T enumValue) where T : Enum
    {
#if DEBUG
        if (!HasFlagsAttribute(typeof(T))) Logger.Error($"Enum must have flag attribute. value: {enumValue}");
#endif
        
        ulong value = Convert.ToUInt64(enumValue); // TODO: Document exception thrown from negative numbers. (underflow)
        return value != 0 && (value & (value - 1)) == 0;
    }
    
    internal static bool HasExactlyOneOrZeroFlags<T>(T enumValue) where T : Enum
    {
#if DEBUG
        if (!HasFlagsAttribute(typeof(T))) Logger.Error($"Enum must have flag attribute. value: {enumValue}");
#endif
        
        ulong value = Convert.ToUInt64(enumValue); // TODO: Document exception thrown from negative numbers. (underflow)
        return (value & (value - 1)) == 0;
    }
    
    internal static bool HasFlagsAttribute(Type type) => type.IsDefined(typeof(FlagsAttribute), inherit: false);
}