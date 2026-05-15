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
                nameof(ArenaOnlineGameMode.ResourceAvailable),
                BindingFlags.Public | BindingFlags.Instance
            ),
            On_RainMeadow_ArenaOnlineGameMode_ResourceAvailable
        );
        
        _ = new Hook(
            typeof(ArenaOnlineGameMode).GetMethod(
                nameof(ArenaOnlineGameMode.AddClientData),
                BindingFlags.Public | BindingFlags.Instance
            ),
            On_RainMeadow_ArenaOnlineGameMode_AddClientData
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
    
    private static void On_RainMeadow_ArenaOnlineGameMode_ResourceAvailable(
        Action<ArenaOnlineGameMode, OnlineResource> orig,
        ArenaOnlineGameMode self,
        OnlineResource onlineResource)
    {
        Logger.Mark(onlineResource);
        
        orig(self, onlineResource);
        
        if (onlineResource is Lobby lobby && lobby.isOwner)
            HideAndSeekLobbyData.RegisterNewInstance(lobby);
    }
    
    private static void On_RainMeadow_ArenaOnlineGameMode_AddClientData(
        Action<ArenaOnlineGameMode> orig,
        ArenaOnlineGameMode self)
    {
        orig(self);
        HideAndSeekClientData.RegisterNewInstance(self);
    }
}