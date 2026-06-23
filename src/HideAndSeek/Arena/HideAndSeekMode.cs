using System.Diagnostics.CodeAnalysis;
using On.ArenaBehaviors;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed partial class HideAndSeekMode : ExternalArenaGameMode
{
    /// <summary>
    /// The duration, in ticks, where seekers
    /// are no longer allowed to tag hiders.
    /// </summary>
    public const int RainCycleEndGraceTicks = 300;
    public static ArenaSetup.GameTypeID Id { get; } = new(Plugin.Name);
    
    
    public ArenaOnlineGameMode ArenaOnline => (ArenaOnlineGameMode)OnlineManager.lobby!.gameMode;
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public HideAndSeekLobbyData LobbyData => OnlineManager.lobby!.GetData<HideAndSeekLobbyData>();
    
    /// <exception cref="KeyNotFoundException">Thrown if the client data is not registered.</exception>
    public HideAndSeekClientData MyClientData => OnlineManager.lobby!
                                                              .clientSettings[OnlineManager.mePlayer]
                                                              .GetData<HideAndSeekClientData>();
    public bool IsSeekingTimeOver => ArenaOnline.session.exitManager.world.rainCycle.TimeUntilRain <= RainCycleEndGraceTicks;
    
    public override int TimerDuration
    {
        get => throw new InvalidOperationException("This should not be used.");
        set => throw new InvalidOperationException("This should not be used.");
    }
    public override ArenaSetup.GameTypeID GetGameModeId => Id;
    
    /// <summary>Determines if the current game mode is Hide and Seek.</summary>
    /// <returns>
    /// <see langword="true"/> if a <see cref="Lobby"/> exists, the
    /// online game mode is <see cref="ArenaOnlineGameMode"/>, and
    /// the external game mode is <see cref="HideAndSeekMode"/>,
    /// otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="HideAndSeekMode"/> is not registered.</exception>
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
    /// Thrown when:<br/>
    /// - The me player is not the host.<br/>
    /// - <see cref="SeekerSelection"/> is not <see cref="SeekerSelection.Random"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void ChooseRandomSeekers()
    {
        if (!OnlineManager.lobby!.isOwner)
            throw new InvalidOperationException("You must be the host to choose random seekers.");
        if (LobbyData.EnabledSeekerSelection != SeekerSelection.Random)
            throw new InvalidOperationException($"{nameof(SeekerSelection)} '{LobbyData.EnabledSeekerSelection}' must be {nameof(SeekerSelection.Random)} to choose random seekers.");
        
        List<OnlinePlayer> readyPlayers = OnlineManager.players.Where(IsReady).ToList();
        readyPlayers.Shuffle();
        
        int seekerCount = Mathf.Min(LobbyData.SeekerCount, readyPlayers.Count);
        LobbyData.Seekers = readyPlayers
                                .Take(seekerCount)
                                .ToList();
        
        return;
        
        bool IsReady(OnlinePlayer oPlayer)
        {
            ArenaClientSettings? clientData = ArenaHelpers.GetDataSettings<ArenaClientSettings>(oPlayer);
            
            if (clientData is null)
            {
                Logger.Debug($"Could not find client data for {oPlayer}.");
                return false;
            }
            
            return clientData.ready;
        }
    }
    
    public override bool SpawnBatflies(FliesWorldAI fliesWorldAI, int spawnRoom) => false;
    
    public override string TimerText() => "Quickly, hide!";
    
    public override int SetTimer(ArenaOnlineGameMode __) => ArenaOnline.setupTime = LobbyData.HideDurationSeconds;
    
    public override bool IsExitsOpen(
        ArenaOnlineGameMode __,
        ExitManager.orig_ExitsOpen orig,
        ArenaBehaviors.ExitManager exitManager)
    {
        bool areAllPlayersSeekers = ArenaOnlineHelper.GetPlayingOPlayers(ArenaOnline)
                                                     .All(oPlayer => oPlayer.IsSeeker);
        
        return IsSeekingTimeOver || areAllPlayersSeekers;
    }
    
    public override void ArenaSessionCtor(
        ArenaOnlineGameMode __,
        On.ArenaGameSession.orig_ctor orig,
        ArenaGameSession arena,
        RainWorldGame game)
    {
        if (OnlineManager.lobby!.isOwner)
            LobbyData.InitialSeekers = LobbyData.Seekers.ToList(); // Clone it, don't set the reference!!!! (I made this mistake)
        
        base.ArenaSessionCtor(ArenaOnline, orig, arena, game);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void AddSeeker(OnlinePlayer oPlayer)
    {
        if (OnlineManager.lobby!.isOwner)
            MakeSeekerRpc(null, oPlayer);
        else
            OnlineManager.lobby.owner.InvokeRPC(MakeSeekerRpc, oPlayer);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void RemoveSeeker(OnlinePlayer oPlayer)
    {
        if (OnlineManager.lobby!.isOwner)
            RemoveSeekerRpc(null, oPlayer);
        else
            OnlineManager.lobby.owner.InvokeRPC(RemoveSeekerRpc, oPlayer);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void ToggleSeeker(OnlinePlayer oPlayer)
    {
        if (oPlayer.IsSeeker)
            RemoveSeeker(oPlayer);
        else
            AddSeeker(oPlayer);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
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
        
        Logger.Debug($"Making {oPlayer.id.name} a seeker. From: {rpcEvent?.from.id.name ?? "(self)"}");
        hideAndSeek.LobbyData.Seekers.Add(oPlayer);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
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
        
        Logger.Debug($"Removing {oPlayer.id.name} from seekers. From: {rpcEvent?.from.id.name ?? "(self)"}");
        hideAndSeek.LobbyData.Seekers.Remove(oPlayer);
    }
}