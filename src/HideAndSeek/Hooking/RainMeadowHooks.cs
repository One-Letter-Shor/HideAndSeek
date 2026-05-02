using MonoMod.RuntimeDetour;
using OneLetterShor.HideAndSeek.Arena;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Hooking;

public static class RainMeadowHooks
{
    internal static void Apply()
    {
        _ = new Hook(
            typeof(ArenaOnlineGameMode).GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [ typeof(Lobby) ],
                null
            ),
            On_RainMeadow_ArenaOnlineGameMode_ctor
        );
        
        _ = new Hook(
            typeof(ArenaOnlineGameMode).GetMethod(
                nameof(ArenaOnlineGameMode.AddClientData),
                BindingFlags.Public | BindingFlags.Instance
            ),
            On_RainMeadow_ArenaOnlineGameMode_AddClientData
        );
        
        _ = new Hook(
            typeof(Lobby).GetMethod(
                "ActivateImpl",
                BindingFlags.NonPublic | BindingFlags.Instance
            ),
            On_RainMeadow_Lobby_ActivateImpl
        );
    }
    
    
    private static void On_RainMeadow_ArenaOnlineGameMode_ctor(
        Action<ArenaOnlineGameMode, Lobby> orig,
        ArenaOnlineGameMode self,
        Lobby lobby)
    {
        orig(self, lobby);
        HideAndSeekMode.RegisterNewInstance(self);
    }
    
    private static void On_RainMeadow_Lobby_ActivateImpl(Action<Lobby> orig, Lobby self)
    {
        orig(self);
        HideAndSeekLobbyData.RegisterNewInstance(self);
    }
    
    private static void On_RainMeadow_ArenaOnlineGameMode_AddClientData(Action<ArenaOnlineGameMode> orig, ArenaOnlineGameMode self)
    {
        orig(self);
        HideAndSeekClientData.RegisterNewInstance(self);
    }
}