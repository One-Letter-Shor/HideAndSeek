using System.Diagnostics.CodeAnalysis;
using On.ArenaBehaviors;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed partial class HideAndSeekMode : ExternalArenaGameMode
{
    public static ArenaSetup.GameTypeID Id { get; } = new(Plugin.Name);
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered yet.</exception>
    public HideAndSeekLobbyData LobbyData => OnlineManager.lobby!.GetData<HideAndSeekLobbyData>();
    
    /// <exception cref="KeyNotFoundException">Thrown if the client data is not registered yet.</exception>
    public HideAndSeekClientData MyClientData => OnlineManager.lobby!
                                                              .clientSettings[OnlineManager.mePlayer]
                                                              .GetData<HideAndSeekClientData>();
    public override int TimerDuration
    {
        get => throw new InvalidOperationException("This should not be used.");
        set => throw new InvalidOperationException("This should not be used.");
    }
    public override ArenaSetup.GameTypeID GetGameModeId => Id;
    
    /// <exception cref="InvalidOperationException">Thrown if the game mode is not registered.</exception>
    public static bool IsHideAndSeekMode([NotNullWhen(true)] out HideAndSeekMode? hideAndSeek)
    {
        hideAndSeek = null;
        
        if (OnlineManager.lobby?.gameMode is not ArenaOnlineGameMode arenaOnline)
            return false;
        
        string modeName = Id.value;
        if (!arenaOnline.registeredGameModes.TryGetValue(modeName, out ExternalArenaGameMode registeredMode))
            throw new InvalidOperationException($"Could not find game mode. registered: [ {string.Join(", ", arenaOnline.registeredGameModes.Keys)} ]");
        
        hideAndSeek = null;
        if (arenaOnline.currentGameMode == modeName)
        {
            hideAndSeek = (HideAndSeekMode)registeredMode;
            return true;
        }
        
        return false;
    }
    
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="SeekerSelection"/> is not <see cref="SeekerSelection.Random"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered yet.</exception>
    public void ChooseRandomSeekers()
    {
        if (LobbyData.EnabledSeekerSelection != SeekerSelection.Random)
            throw new InvalidOperationException($"{nameof(SeekerSelection)} '{LobbyData.EnabledSeekerSelection}' must be {nameof(SeekerSelection.Random)} to choose random seekers.");
        
        List<OnlinePlayer> readyPlayers = OnlineManager.players.Where(IsReady).ToList();
        readyPlayers.Shuffle();
        
        int seekerCount = Mathf.Min(LobbyData.SeekerCount, readyPlayers.Count);
        LobbyData.Seekers = readyPlayers
                                .Take(seekerCount)
                                .ToList();
        
        Logger.Info($"Seekers: [ {string.Join(", ", LobbyData.Seekers)} ]");
        
        return;
        
        bool IsReady(OnlinePlayer oPlayer)
        {
            if (!OnlineManager.lobby!.clientSettings.TryGetValue(oPlayer, out ClientSettings clientSettings)
                || !clientSettings.TryGetData(out ArenaClientSettings data))
            {
                Logger.Debug($"Could not find client data for {oPlayer}.");
                return false;
            }
            
            return data.ready;
        }
    }
    
    public override bool SpawnBatflies(FliesWorldAI fliesWorldAI, int spawnRoom) => false;
    
    public override string TimerText() => "Quickly, hide!";
    
    public override int SetTimer(ArenaOnlineGameMode arenaOnline) => arenaOnline.setupTime = LobbyData.HideDurationSeconds;
    
    public override bool IsExitsOpen(
        ArenaOnlineGameMode arenaOnline,
        ExitManager.orig_ExitsOpen orig,
        ArenaBehaviors.ExitManager exitManager)
    {
        return orig(exitManager);
    }
    
    public override void ArenaSessionCtor(
        ArenaOnlineGameMode arenaOnline,
        On.ArenaGameSession.orig_ctor orig,
        ArenaGameSession arena,
        RainWorldGame game)
    {
        base.ArenaSessionCtor(arenaOnline, orig, arena, game);
        
        LogGameInfo(arena, arenaOnline);
    }
    
    private void LogGameInfo(ArenaGameSession arena, ArenaOnlineGameMode arenaOnline) // temp
    {
        Logger.Info(
            $"""
            INFO:
            - [arena]    players:  -  -  -  -  -  -  -  -  -  -  [ {string.Join(", ", arena.Players)} ]
            - [arena]    arena sitting players:   -  -  -  -  -  [ {string.Join(", ", arena.arenaSitting.players)} ]
            - [online]   waiting for next round count:  -  -  -  [ {string.Join(", ", arenaOnline.playersLateWaitingInLobbyForNextRound.Select(inLobbyId => ArenaHelpers.FindOnlinePlayerByLobbyId(inLobbyId)?.id.name ?? "null"))} ]
            - [online]   equal to online sitting: -  -  -  -  -  {arenaOnline.playersEqualToOnlineSitting}
            """
        );
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered yet.</exception>
    public void MakeSeeker(OnlinePlayer oPlayer)
    {
        Logger.Info($"Making {oPlayer} a seeker!");
        
        if (OnlineManager.lobby!.isOwner)
            MakeSeekerRpc(null, oPlayer);
        else
            OnlineManager.lobby.owner.InvokeRPC(MakeSeekerRpc, oPlayer);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered yet.</exception>
    public void RemoveSeeker(OnlinePlayer oPlayer)
    {
        Logger.Info($"Removing {oPlayer} from seekers!");
        if (OnlineManager.lobby!.isOwner)
            RemoveSeekerRpc(null, oPlayer);
        else
            OnlineManager.lobby.owner.InvokeRPC(RemoveSeekerRpc, oPlayer);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered yet.</exception>
    [RPCMethod]
    private static void MakeSeekerRpc(RPCEvent? rpcEvent, OnlinePlayer oPlayer)
    {
        Assert(rpcEvent?.from.isMe != true);
        
        if (!OnlineManager.lobby!.isOwner)
        {
            Logger.Warning($"Received {nameof(MakeSeekerRpc)} when not the host.");
            return;
        }
        if (OnlineManager.lobby.gameMode is not ArenaOnlineGameMode arenaOnline)
        {
            Logger.Warning($"Received {nameof(MakeSeekerRpc)} when the online game mode ({OnlineManager.lobby!.gameMode}) is not Arena Online.");
            return;
        }
        if (!IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            Logger.Warning($"Received {nameof(MakeSeekerRpc)} when the game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
            return;
        }
        if (oPlayer.IsSeeker)
        {
            Logger.Debug($"Received {nameof(MakeSeekerRpc)} when {oPlayer.id.name} is already a seeker.");
            return;
        }
        
        Logger.Info($"Making {oPlayer.id.name} a seeker. From: {rpcEvent?.from.id.name ?? "(self)"}");
        hideAndSeek.LobbyData.Seekers.Add(oPlayer);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered yet.</exception>
    [RPCMethod]
    private static void RemoveSeekerRpc(RPCEvent? rpcEvent, OnlinePlayer oPlayer)
    {
        Assert(rpcEvent?.from.isMe != true);
        
        if (!OnlineManager.lobby!.isOwner)
        {
            Logger.Warning($"Received {nameof(RemoveSeekerRpc)} when not the host.");
            return;
        }
        if (OnlineManager.lobby.gameMode is not ArenaOnlineGameMode arenaOnline)
        {
            Logger.Warning($"Received {nameof(RemoveSeekerRpc)} when the online game mode ({OnlineManager.lobby!.gameMode}) is not Arena Online.");
            return;
        }
        if (!IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            Logger.Warning($"Received {nameof(RemoveSeekerRpc)} when the game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
            return;
        }
        if (!oPlayer.IsSeeker)
        {
            Logger.Debug($"Received {nameof(RemoveSeekerRpc)} when {oPlayer.id.name} is already a Hider.");
            return;
        }
        
        Logger.Info($"Removing {oPlayer.id.name} from seekers. From: {rpcEvent?.from.id.name ?? "(self)"}");
        hideAndSeek.LobbyData.Seekers.Remove(oPlayer);
    }
}