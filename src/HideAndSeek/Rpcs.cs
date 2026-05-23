using OneLetterShor.HideAndSeek.Arena;
using RainMeadow;

namespace OneLetterShor.HideAndSeek;

public static class Rpcs
{
    /// <exception cref="NullReferenceException">Thrown if there is no <see cref="Lobby"/>.</exception>
    [RPCMethod]
    public static void MakeSeeker(RPCEvent rpcEvent, OnlinePlayer oPlayer)
    {
        if (!OnlineManager.lobby!.isOwner)
        {
            Logger.Warning($"Received {nameof(MakeSeeker)} RPC when not the host.");
            return;
        }
        if (OnlineManager.lobby.gameMode is not ArenaOnlineGameMode arenaOnline)
        {
            Logger.Warning($"Received {nameof(MakeSeeker)} RPC when the online game mode ({OnlineManager.lobby!.gameMode}) is not Arena Online.");
            return;
        }
        if (!HideAndSeekMode.IsHideAndSeekMode(arenaOnline, out HideAndSeekMode? hideAndSeek))
        {
            Logger.Warning($"Received {nameof(MakeSeeker)} RPC when the game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
            return;
        }
        if (hideAndSeek.LobbyData.Seekers.Contains(oPlayer))
        {
            Logger.Info($"Received {nameof(MakeSeeker)} RPC when {oPlayer.id.name} is already a seeker.");
            return;
        }
        
        Logger.Info($"Making {oPlayer.id.name} a seeker due to RPC from {rpcEvent.from.id.name}");
        hideAndSeek.LobbyData.Seekers.Add(oPlayer);
    }
    
    /// <exception cref="NullReferenceException">Thrown if there is no <see cref="Lobby"/>.</exception>
    [RPCMethod]
    public static void MakeHider(RPCEvent rpcEvent, OnlinePlayer oPlayer)
    {
        if (!OnlineManager.lobby!.isOwner)
        {
            Logger.Warning($"Received {nameof(MakeHider)} RPC when not the host.");
            return;
        }
        if (OnlineManager.lobby.gameMode is not ArenaOnlineGameMode arenaOnline)
        {
            Logger.Warning($"Received {nameof(MakeHider)} RPC when the online game mode ({OnlineManager.lobby!.gameMode}) is not Arena Online.");
            return;
        }
        if (!HideAndSeekMode.IsHideAndSeekMode(arenaOnline, out HideAndSeekMode? hideAndSeek))
        {
            Logger.Warning($"Received {nameof(MakeHider)} RPC when the game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
            return;
        }
        if (!hideAndSeek.LobbyData.Seekers.Contains(oPlayer))
        {
            Logger.Info($"Received {nameof(MakeHider)} RPC when {oPlayer.id.name} is already a Hider.");
            return;
        }
        
        Logger.Info($"Making {oPlayer.id.name} a hider due to RPC from {rpcEvent.from.id.name}");
        hideAndSeek.LobbyData.Seekers.Remove(oPlayer);
    }
}