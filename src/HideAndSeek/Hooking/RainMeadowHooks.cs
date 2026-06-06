using MonoMod.RuntimeDetour;
using OneLetterShor.HideAndSeek.Arena;
using RainMeadow;
using RainMeadow.UI;

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
        
        _ = new Hook(
            typeof(ArenaOnlineLobbyMenu).GetMethod(
                nameof(ArenaOnlineLobbyMenu.StartGame),
                BindingFlags.Public | BindingFlags.Instance
            ),
            On_RainMeadow_ArenaOnlineLobbyMenu_StartGame
        );
    }
    
    
    private static void On_RainMeadow_ArenaOnlineGameMode_ctor(
        Action<ArenaOnlineGameMode, Lobby> orig,
        ArenaOnlineGameMode self,
        Lobby lobby)
    {
        orig(self, lobby);
        self.AddExternalGameModes(HideAndSeekMode.Id, new HideAndSeekMode());
    }
    
    private static void On_RainMeadow_ArenaOnlineGameMode_ResourceAvailable(
        Action<ArenaOnlineGameMode, OnlineResource> orig,
        ArenaOnlineGameMode self,
        OnlineResource onlineResource)
    {
        orig(self, onlineResource);
        
        if (onlineResource is Lobby lobby && lobby.isOwner)
            lobby.AddData(new HideAndSeekLobbyData());
    }
    
    private static void On_RainMeadow_ArenaOnlineGameMode_AddClientData(
        Action<ArenaOnlineGameMode> orig,
        ArenaOnlineGameMode self)
    {
        orig(self);
        self.clientSettings.AddData(new HideAndSeekClientData { IsMine = true });
    }
    
    /// <exception cref="NullReferenceException">Thrown if there is no <see cref="Lobby"/>.</exception>
    private static void On_RainMeadow_ArenaOnlineLobbyMenu_StartGame(
        Action<ArenaOnlineLobbyMenu> orig,
        ArenaOnlineLobbyMenu self)
    {
        AssertIs(OnlineManager.lobby!.gameMode, out ArenaOnlineGameMode arenaOnline);
        
        if (HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            if (OnlineManager.lobby.isOwner && arenaOnline.lobbyCountDown > 0)
                hideAndSeek.ChooseRandomSeekers();
        }
        
        orig(self);
    }

}