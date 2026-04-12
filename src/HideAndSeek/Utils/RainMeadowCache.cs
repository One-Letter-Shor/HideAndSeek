using RainMeadow;

namespace OneLetterShor.HideAndSeek.Utils;

internal static class RainMeadowCache
{
    internal static void Initialize() { }
    
    internal static Type[] ParamTypes_ArenaOnlineGameMode_ctor { get; } = [ typeof(Lobby) ];
    
    internal static MethodInfo Lobby_ActivateImpl { get; } = typeof(Lobby).GetMethod(
                                                                 "ActivateImpl",
                                                                 BindingFlags.NonPublic | BindingFlags.Instance
                                                             )
                                                             ?? throw new MissingMethodException();
    
    
    internal static MethodInfo ArenaOnlineGameMode_AddClientData { get; } = typeof(ArenaOnlineGameMode).GetMethod(
                                                                                nameof(ArenaOnlineGameMode.AddClientData),
                                                                                BindingFlags.Public | BindingFlags.Instance
                                                                            )
                                                                            ?? throw new MissingMethodException();
    
    
    internal static ConstructorInfo ArenaOnlineGameMode_ctor { get; } = typeof(ArenaOnlineGameMode).GetConstructor(
                                                                            BindingFlags.Public | BindingFlags.Instance,
                                                                            null,
                                                                            ParamTypes_ArenaOnlineGameMode_ctor,
                                                                            null
                                                                        )
                                                                        ?? throw new MissingMethodException();
}