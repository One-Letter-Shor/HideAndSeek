namespace OneLetterShor.HideAndSeek.Utils;

internal static class EnumHelper
{
    /// <summary>
    /// Determines whether <paramref name="enumValue"/> has exactly one bit set.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the value has one
    /// bit set, otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="OverflowException">
    /// Propagated by <see langword="Convert.ToUInt64"/> if
    /// <paramref name="enumValue"/>'s underlying value is negative.
    /// </exception>
    internal static bool HasExactlyOneFlag(Enum enumValue)
    {
#if DEBUG
        if (!HasFlagsAttribute(enumValue.GetType())) Logger.Error($"Enum should have flag attribute. value: {enumValue}");
#endif
        
        ulong value = Convert.ToUInt64(enumValue);
        return value != 0 && (value & (value - 1)) == 0;
    }
    
    /// <summary>
    /// Determines whether <paramref name="enumValue"/> has exactly one or zero bits set.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the value has one or zero
    /// bits set, otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="OverflowException">
    /// Propagated by <see langword="Convert.ToUInt64"/> if
    /// <paramref name="enumValue"/>'s underlying value is negative.
    /// </exception>
    internal static bool HasExactlyOneOrZeroFlags(Enum enumValue)
    {
#if DEBUG
        if (!HasFlagsAttribute(enumValue.GetType())) Logger.Error($"Enum should have flag attribute. value: {enumValue}");
#endif
        
        ulong value = Convert.ToUInt64(enumValue);
        return (value & (value - 1)) == 0;
    }
    
    internal static bool HasFlagsAttribute(Type type) => type.IsDefined(typeof(FlagsAttribute), false);
}