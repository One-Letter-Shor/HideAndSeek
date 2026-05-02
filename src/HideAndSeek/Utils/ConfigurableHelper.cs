using Menu.Remix;

namespace OneLetterShor.HideAndSeek.Utils;

internal static class ConfigurableHelper
{
    /// <summary>
    /// Creates a new <see cref="Configurable{T}"/> based on the value and
    /// <see cref="ConfigurableInfo"/> of the cloned <see cref="Configurable{T}"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Propagated by <see cref="ValueConverter.ConvertToValue{T}"/>
    /// if a valid <see cref="ValueConverter.Converter"/> could not
    /// be found for <typeparamref name="T"/>.
    /// </exception>
    internal static Configurable<T> Clone<T>(Configurable<T> configurable)
    {
        return new Configurable<T>(
            ValueConverter.ConvertToValue<T>(configurable.defaultValue),
            configurable.info.acceptable
        );
    }
}