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
    
    public override ArenaSetup.GameTypeID GetGameModeId => Id;
    public ArenaOnlineGameMode ArenaOnline => (ArenaOnlineGameMode)OnlineManager.lobby!.gameMode;
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public HideAndSeekLobbyData LobbyData => OnlineManager.lobby!.GetData<HideAndSeekLobbyData>();
    
    /// <exception cref="KeyNotFoundException">Thrown if the client data is not registered.</exception>
    public HideAndSeekClientData MyClientData => OnlineManager.lobby!
                                                              .clientSettings[OnlineManager.mePlayer]
                                                              .GetData<HideAndSeekClientData>();
    
    /// <summary>
    /// Determines if the host is able to start a new game
    /// </summary>
    /// <remarks>
    /// This property only considers gameplay-related Hide
    /// and Seek data. Debug settings, such as
    /// <see cref="HideAndSeekLobbyData.AreSeekerDebugToolsEnabled"/>,
    /// are ignored.
    /// </remarks>
    public bool CanStartNewGame
    {
        get
        {
            // If this logic or the functions below change, ensure SelectRandomSeekers is updated too.
            if (LobbyData.EnabledSeekerSelection == SeekerSelection.Random)
            {
                int selectablePlayerCount = OnlineManager.players
                    .Count(player => IsReady(player) && IsWillingToSeek(player));
                
                return selectablePlayerCount >= LobbyData.SeekerCount + 1; // Enough players for all seekers + 1 hider
            }
            
            return LobbyData.Seekers.Count > 0 && LobbyData.Seekers.Count < OnlineManager.players.Count;
            
            static bool IsReady(OnlinePlayer oPlayer)
            {
                ArenaClientSettings? clientData = ArenaHelpers.GetDataSettings<ArenaClientSettings>(oPlayer);
                
                if (clientData is null)
                {
                    Logger.Debug($"Could not find client data for {oPlayer}.");
                    return false;
                }
                
                return clientData.ready;
            }
            
            static bool IsWillingToSeek(OnlinePlayer oPlayer)
            {
                HideAndSeekClientData? clientData = ArenaHelpers.GetDataSettings<HideAndSeekClientData>(oPlayer);
                
                if (clientData is null)
                {
                    Logger.Debug($"Could not find client data for {oPlayer}.");
                    return false;
                }
                
                return clientData.IsWillingToSeek;
            }
        }
    }
    
    /// <exception cref="InvalidOperationException">Thrown if not in a game.</exception>
    public bool IsSeekingTimeOver
    {
        get
        {
            if (!GameHelper.IsInGame)
                throw new InvalidOperationException("Not in a game.");
            
            return ArenaOnline.session!.exitManager.world.rainCycle.TimeUntilRain <= RainCycleEndGraceTicks;
        }
    }
    
    public override int TimerDuration
    {
        get => throw new InvalidOperationException("This should not be used.");
        set => throw new InvalidOperationException("This should not be used.");
    }
    
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
    
    public HideAndSeekMode()
    {
        MatchmakingManager.OnPlayerListReceived += OnPlayerListReceived;
        MatchmakingManager.OnLobbyLeaving += OnLobbyLeaving;
    }
    
    private void OnPlayerListReceived(PlayerInfo[] playerList)
    {
        if (OnlineManager.lobby!.isOwner)
        {
            LobbyData.Seekers.RemoveAll(
                seeker => !OnlineManager.players.Contains(seeker)
            );
            LobbyData.InitialSeekers.RemoveAll(
                initialSeeker => !OnlineManager.players.Contains(initialSeeker)
            );
        }
    }
    
    private void OnLobbyLeaving()
    {
        MatchmakingManager.OnPlayerListReceived -= OnPlayerListReceived;
        MatchmakingManager.OnLobbyLeaving -= OnLobbyLeaving;
    }
    
    /// <inheritdoc cref="CanSelectSeeker(SeekerSelection, OnlinePlayer, OnlinePlayer, out string)"/>
    public bool CanSelectSeeker(
        SeekerSelection seekerSelection,
        OnlinePlayer selector,
        OnlinePlayer target)
    {
        return CanSelectSeeker(seekerSelection, selector, target, out _);
    }
    
    /// <summary>
    /// Determines whether an <see cref="OnlinePlayer"/> can select
    /// the other specified <see cref="OnlinePlayer"/> to be a seeker.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the following are all true:<br/>
    /// - <paramref name="seekerSelection"/> is not <see cref="SeekerSelection.Random"/>.<br/>
    /// - The <paramref name="selector"/> and <paramref name="target"/>
    /// are valid based on the value of <paramref name="seekerSelection"/>.<br/>
    /// Otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="seekerSelection"/> is not <see cref="SeekerSelection.Random"/>,
    /// <see cref="SeekerSelection.Host"/>, or <see cref="SeekerSelection.Self"/>.
    /// (Update this method if a new <see cref="SeekerSelection"/> is added.)
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public bool CanSelectSeeker(
        SeekerSelection seekerSelection,
        OnlinePlayer selector,
        OnlinePlayer target,
        [NotNullWhen(false)] out string? failureReason)
    {
        bool isSelectorHost = selector == OnlineManager.lobby!.owner;
        failureReason = null;
        
        switch (seekerSelection)
        {
            case SeekerSelection.Random:
                failureReason = "Seeker selection is random. No one can select seekers.";
                return false;
            
            case SeekerSelection.Host:
                if (!isSelectorHost)
                {
                    failureReason = $"Selector ({selector}) is not the host.";
                    return false;
                }
                
                if (!IsWillingToSeek(target))
                {
                    failureReason = $"Target ({target}) is not willing to seek.";
                    return false;
                }
                
                return true;
            
            case SeekerSelection.Self:
                if (selector != target)
                {
                    failureReason = $"Selector ({selector}) can only select themselves. (target is {target}).";
                    return false;
                }
                
                return true;
            
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(seekerSelection),
                    seekerSelection,
                    $"Unrecognized {nameof(SeekerSelection)}."
                );
        }
        
        static bool IsWillingToSeek(OnlinePlayer oPlayer)
        {
            HideAndSeekClientData? clientData = ArenaHelpers.GetDataSettings<HideAndSeekClientData>(oPlayer);
            
            if (clientData is null)
            {
                Logger.Debug($"Could not find client data for {oPlayer}.");
                return false;
            }
            
            return clientData.IsWillingToSeek;
        }
    }
    
    /// <inheritdoc cref="CanTagPlayer(OnlinePlayer, OnlinePlayer, out string)"/>
    public bool CanTagPlayer(OnlinePlayer tagger, OnlinePlayer target)
    {
        return CanTagPlayer(tagger, target, out _);
    }
    
    /// <summary>
    /// Determines whether an <see cref="OnlinePlayer"/>
    /// can tag the other specified <see cref="OnlinePlayer"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the following are all true:<br/>
    /// - There is still time for seekers to seek. (It is not about to rain)<br/>
    /// - <paramref name="tagger"/> is not trying to tag themself.<br/>
    /// - <paramref name="tagger"/> is a seeker.<br/>
    /// - <paramref name="target"/> is not a seeker.<br/>
    /// Otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if not in a game.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public bool CanTagPlayer(
        OnlinePlayer tagger,
        OnlinePlayer target,
        [NotNullWhen(false)] out string? failureReason)
    {
        failureReason = null;
        List<string> failureReasons = [];
        
        if (IsSeekingTimeOver)
            failureReasons.Add("Seeking time is over.");
        if (tagger == target)
            failureReasons.Add($"Tagger ({tagger}) is trying to tag themself.");
        if (!tagger.IsSeeker)
            failureReasons.Add($"Tagger ({tagger}) is not a seeker.");
        if (target.IsSeeker)
            failureReasons.Add($"Target ({target}) is a seeker.");
        
        if (failureReasons.Count > 0)
        {
            failureReason = string.Join(" ", failureReasons);
            return false;
        }
        
        return true;
    }
    
    public override void ArenaSessionNextLevel(
        ArenaOnlineGameMode __,
        On.ArenaSitting.orig_NextLevel orig,
        ArenaSitting self,
        ProcessManager process)
    {
        if (OnlineManager.lobby!.isOwner)
            LobbyData.Seekers = LobbyData.InitialSeekers.ToList();
        
        base.ArenaSessionNextLevel(ArenaOnline, orig, self, process);
    }
    
    public override bool SpawnBatflies(FliesWorldAI fliesWorldAI, int spawnRoom) => false;
    
    public override string TimerText() => "Quickly, hide!";
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public override int SetTimer(ArenaOnlineGameMode __) => ArenaOnline.setupTime = LobbyData.HideDurationSeconds;
    
    /// <exception cref="InvalidOperationException">Thrown if not in a game.</exception>
    public override bool IsExitsOpen(
        ArenaOnlineGameMode __,
        ExitManager.orig_ExitsOpen orig,
        ArenaBehaviors.ExitManager exitManager)
    {
        bool areAllPlayersSeekers = ArenaOnlineHelper.GetPlayingOPlayers(ArenaOnline)
                                                     .All(oPlayer => oPlayer.IsSeeker);
        
        return IsSeekingTimeOver || areAllPlayersSeekers;
    }
    
    public override void Killing(
        ArenaOnlineGameMode __,
        On.ArenaGameSession.orig_Killing orig,
        ArenaGameSession self,
        Player player,
        Creature killedCreature)
    {
        Logger.Error("This should not run in Hide and Seek.");
    }
    
    public override void LandSpear(
        ArenaOnlineGameMode __,
        ArenaGameSession self,
        Player player,
        Creature target,
        ArenaSitting.ArenaPlayer arenaPlayer)
    {
        Logger.Error("This should not run in Hide and Seek.");
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void TagPlayer(OnlinePlayer oPlayer)
    {
        if (OnlineManager.lobby!.isOwner)
            TagPlayerRpc(null, oPlayer);
        else
            OnlineManager.lobby.owner.InvokeRPC(TagPlayerRpc, oPlayer);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    [RPCMethod]
    public static void TagPlayerRpc(RPCEvent? rpcEvent, OnlinePlayer target)
    {
        Assert(rpcEvent?.from.isMe != true);
        string fromText = "From: " + (rpcEvent?.from.ToString() ?? "(local)") + ".";
        OnlinePlayer tagger = rpcEvent?.from ?? OnlineManager.mePlayer;
        
        if (!OnlineManager.lobby!.isOwner)
        {
            Logger.Warning($"Me player is not the host. {fromText}");
            return;
        }
        if (!IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            Logger.Warning($"The game mode is not Hide and Seek. {fromText}");
            return;
        }
        if (!hideAndSeek.CanTagPlayer(tagger, target, out string? failureReason))
        {
            Logger.Debug($"{tagger} is trying to tag {target} when they cannot. Reason: {failureReason} {fromText}");
            return;
        }
        
        Logger.Debug($"Making {target} a seeker. {fromText}");
        hideAndSeek.LobbyData.Seekers.Add(target);
    }
    
    /// <exception cref="InvalidOperationException">
    /// Thrown when:<br/>
    /// - The me player is not the host.<br/>
    /// - <see cref="SeekerSelection"/> is not <see cref="SeekerSelection.Random"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void SelectRandomSeekers()
    {
        if (!OnlineManager.lobby!.isOwner)
            throw new InvalidOperationException("You must be the host to select random seekers.");
        if (LobbyData.EnabledSeekerSelection != SeekerSelection.Random)
            throw new InvalidOperationException($"Cannot select random seekers when selection mode is '{LobbyData.EnabledSeekerSelection}'.");
        
        // If this logic or the functions below change, ensure CanStartNewGame and SelectSeekerRpc are updated too.
        
        List<OnlinePlayer> selectablePlayers = OnlineManager.players
                                                            .Where(player => IsReady(player) &&
                                                                             IsWillingToSeek(player)
                                                            ).ToList();
        selectablePlayers.Shuffle();
        
        int seekerCount = Mathf.Min(LobbyData.SeekerCount, selectablePlayers.Count);
        List<OnlinePlayer> selectedSeekers = selectablePlayers
                                                .Take(seekerCount)
                                                .ToList();
        
        if (selectedSeekers.Count != seekerCount)
        {
            Logger.Error(
                $"Desired seeker count is '{seekerCount}' but there " +
                $"are only {selectedSeekers.Count} selectable players." +
                $"\n- All players:        [ {string.Join(", ", OnlineManager.players)} ]." + 
                $"\n- Selectable players: [ {string.Join(", ", selectablePlayers)} ]." 
            );
        }
        
        LobbyData.Seekers        = selectedSeekers.ToList();
        LobbyData.InitialSeekers = selectedSeekers.ToList();
        
        return;
        
        static bool IsReady(OnlinePlayer oPlayer)
        {
            ArenaClientSettings? clientData = ArenaHelpers.GetDataSettings<ArenaClientSettings>(oPlayer);
            
            if (clientData is null)
            {
                Logger.Debug($"Could not find client data for {oPlayer}.");
                return false;
            }
            
            return clientData.ready;
        }
        
        static bool IsWillingToSeek(OnlinePlayer oPlayer)
        {
            HideAndSeekClientData? clientData = ArenaHelpers.GetDataSettings<HideAndSeekClientData>(oPlayer);
            
            if (clientData is null)
            {
                Logger.Debug($"Could not find client data for {oPlayer}.");
                return false;
            }
            
            return clientData.IsWillingToSeek;
        }
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void SelectSeeker(OnlinePlayer target)
    {
        if (OnlineManager.lobby!.isOwner)
            SelectSeekerRpc(null, target);
        else
            OnlineManager.lobby.owner.InvokeRPC(SelectSeekerRpc, target);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void DeselectSeeker(OnlinePlayer target)
    {
        if (OnlineManager.lobby!.isOwner)
            DeselectSeekerRpc(null, target);
        else
            OnlineManager.lobby.owner.InvokeRPC(DeselectSeekerRpc, target);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void ToggleSelectSeeker(OnlinePlayer target)
    {
        if (target.IsSeeker)
            DeselectSeeker(target);
        else
            SelectSeeker(target);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    [RPCMethod]
    public static void SelectSeekerRpc(RPCEvent? rpcEvent, OnlinePlayer target)
    {
        Assert(rpcEvent?.from.isMe != true);
        string fromText = "From: " + (rpcEvent?.from.ToString() ?? "(local)") + ".";
        OnlinePlayer selector = rpcEvent?.from ?? OnlineManager.mePlayer;
        
        if (!OnlineManager.lobby!.isOwner)
        {
            Logger.Warning($"Me player is not the host. {fromText}");
            return;
        }
        if (!IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            Logger.Warning($"The game mode is not Hide and Seek. {fromText}");
            return;
        }
        if (!hideAndSeek.CanSelectSeeker(hideAndSeek.LobbyData.EnabledSeekerSelection, selector, target, out string? failureReason))
        {
            Logger.Debug($"{selector} cannot select {target}. Reason: {failureReason}");
            return;
        }
        if (target.IsSeeker != target.IsAnInitialSeeker)
        {
            Logger.Warning(
                "Player has inconsistent seeker flags during selection. " +
                $"Is seeker: {target.IsSeeker}. Is an initial seeker: {target.IsAnInitialSeeker}. {fromText}"
            );
        }
        
        // If this logic changes, ensure SelectRandomSeekers is updated too.
        if (!target.IsSeeker)
        {
            Logger.Debug($"Selecting {target} to be seeker. {fromText}");
            hideAndSeek.LobbyData.Seekers.Add(target);
        }
        if (!target.IsAnInitialSeeker)
            hideAndSeek.LobbyData.InitialSeekers.Add(target);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    [RPCMethod]
    public static void DeselectSeekerRpc(RPCEvent? rpcEvent, OnlinePlayer target)
    {
        Assert(rpcEvent?.from.isMe != true);
        string fromText = "From: " + (rpcEvent?.from.ToString() ?? "(local)") + ".";
        OnlinePlayer selector = rpcEvent?.from ?? OnlineManager.mePlayer;
        
        if (!OnlineManager.lobby!.isOwner)
        {
            Logger.Warning($"Me player is not the host. {fromText}");
            return;
        }
        if (!IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            Logger.Warning($"The game mode is not Hide and Seek. {fromText}");
            return;
        }
        if (!hideAndSeek.CanSelectSeeker(hideAndSeek.LobbyData.EnabledSeekerSelection, selector, target, out string? failureReason))
        {
            Logger.Debug($"{selector} cannot select {target}. Reason: {failureReason} {fromText}");
            return;
        }
        if (target.IsSeeker != target.IsAnInitialSeeker)
        {
            Logger.Error(
                "Player has inconsistent seeker flags during selection. " +
                $"Is seeker: {target.IsSeeker}. Is an initial seeker: {target.IsAnInitialSeeker}. {fromText}"
            );
        }
        
        if (target.IsSeeker)
        {
            Logger.Debug($"Deselecting {target} from being a seeker. {fromText}");
            hideAndSeek.LobbyData.Seekers.Remove(target);
        }
        
        if (target.IsAnInitialSeeker)
            hideAndSeek.LobbyData.InitialSeekers.Remove(target);
    }
    
    
    
    
    /// <exception cref="InvalidOperationException">Thrown if seeker debug tools are not enabled.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void DebugAddSeeker(OnlinePlayer target)
    {
        if (!LobbyData.AreSeekerDebugToolsEnabled)
            throw new InvalidOperationException("Seeker debug tools are not enabled.");
        
        if (OnlineManager.lobby!.isOwner)
            DebugAddSeekerRpc(null, target);
        else
            OnlineManager.lobby.owner.InvokeRPC(DebugAddSeekerRpc, target);
    }
    
    /// <exception cref="InvalidOperationException">Thrown if seeker debug tools are not enabled.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void DebugRemoveSeeker(OnlinePlayer oPlayer)
    {
        if (!LobbyData.AreSeekerDebugToolsEnabled)
            throw new InvalidOperationException("Seeker debug tools are not enabled.");
        
        if (OnlineManager.lobby!.isOwner)
            DebugRemoveSeekerRpc(null, oPlayer);
        else
            OnlineManager.lobby.owner.InvokeRPC(DebugRemoveSeekerRpc, oPlayer);
    }
    
    /// <exception cref="InvalidOperationException">Thrown if seeker debug tools are not enabled.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void DebugToggleSeeker(OnlinePlayer oPlayer)
    {
        if (!LobbyData.AreSeekerDebugToolsEnabled)
            throw new InvalidOperationException("Seeker debug tools are not enabled.");
        
        if (oPlayer.IsSeeker)
            DebugRemoveSeeker(oPlayer);
        else
            DebugAddSeeker(oPlayer);
    }
    
    /// <exception cref="InvalidOperationException">Thrown if seeker debug tools are not enabled.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void DebugAddInitialSeeker(OnlinePlayer oPlayer)
    {
        if (!LobbyData.AreSeekerDebugToolsEnabled)
            throw new InvalidOperationException("Seeker debug tools are not enabled.");
        
        if (OnlineManager.lobby!.isOwner)
            DebugAddInitialSeekerRpc(null, oPlayer);
        else
            OnlineManager.lobby.owner.InvokeRPC(DebugAddInitialSeekerRpc, oPlayer);
    }
    
    /// <exception cref="InvalidOperationException">Thrown if seeker debug tools are not enabled.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void DebugRemoveInitialSeeker(OnlinePlayer oPlayer)
    {
        if (!LobbyData.AreSeekerDebugToolsEnabled)
            throw new InvalidOperationException("Seeker debug tools are not enabled.");
        
        if (OnlineManager.lobby!.isOwner)
            DebugRemoveInitialSeekerRpc(null, oPlayer);
        else
            OnlineManager.lobby.owner.InvokeRPC(DebugRemoveInitialSeekerRpc, oPlayer);
    }
    
    /// <exception cref="InvalidOperationException">Thrown if seeker debug tools are not enabled.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public void DebugToggleInitialSeeker(OnlinePlayer oPlayer)
    {
        if (!LobbyData.AreSeekerDebugToolsEnabled)
            throw new InvalidOperationException("Seeker debug tools are not enabled.");
        
        if (oPlayer.IsAnInitialSeeker)
            DebugRemoveInitialSeeker(oPlayer);
        else
            DebugAddInitialSeeker(oPlayer);
    }
    
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    [RPCMethod]
    public static void DebugAddSeekerRpc(RPCEvent? rpcEvent, OnlinePlayer oPlayer)
    {
        Assert(rpcEvent?.from.isMe != true);
        string fromText = "From: " + (rpcEvent?.from.ToString() ?? "(local)") + ".";
        
        if (!OnlineManager.lobby!.isOwner) return;
        if (!IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek)) return;
        if (!hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled) return;
        if (oPlayer.IsSeeker) return;
        
        Logger.Debug($"(Debug) Adding {oPlayer} to seekers. {fromText}");
        hideAndSeek.LobbyData.Seekers.Add(oPlayer);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    [RPCMethod]
    public static void DebugRemoveSeekerRpc(RPCEvent? rpcEvent, OnlinePlayer oPlayer)
    {
        Assert(rpcEvent?.from.isMe != true);
        string fromText = "From: " + (rpcEvent?.from.ToString() ?? "(local)") + ".";
        
        if (!OnlineManager.lobby!.isOwner) return;
        if (!IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek)) return;
        if (!hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled) return;
        if (!oPlayer.IsSeeker) return;
        
        Logger.Debug($"(Debug) Removing {oPlayer} from seekers. {fromText}");
        hideAndSeek.LobbyData.Seekers.Remove(oPlayer);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    [RPCMethod]
    public static void DebugAddInitialSeekerRpc(RPCEvent? rpcEvent, OnlinePlayer oPlayer)
    {
        Assert(rpcEvent?.from.isMe != true);
        string fromText = "From: " + (rpcEvent?.from.ToString() ?? "(local)") + ".";
        
        if (!OnlineManager.lobby!.isOwner) return;
        if (!IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek)) return;
        if (!hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled) return;
        if (oPlayer.IsAnInitialSeeker) return;
        
        Logger.Debug($"(Debug) Adding {oPlayer} to initial seekers. {fromText}");
        hideAndSeek.LobbyData.InitialSeekers.Add(oPlayer);
    }
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    [RPCMethod]
    public static void DebugRemoveInitialSeekerRpc(RPCEvent? rpcEvent, OnlinePlayer oPlayer)
    {
        Assert(rpcEvent?.from.isMe != true);
        string fromText = "From: " + (rpcEvent?.from.ToString() ?? "(local)") + ".";
        
        if (!OnlineManager.lobby!.isOwner) return;
        if (!IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek)) return;
        if (!hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled) return;
        if (!oPlayer.IsAnInitialSeeker) return;
        
        Logger.Debug($"(Debug) Removing {oPlayer} from initial seekers. {fromText}");
        hideAndSeek.LobbyData.InitialSeekers.Remove(oPlayer);
    }
}