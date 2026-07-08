using System.Diagnostics.CodeAnalysis;
using Menu;
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
    /// Indicates whether seekers' winning has been recognized.
    /// </summary>
    /// <remarks>
    /// Meaningless if not the host. Also, there
    /// is no custom handling for host transfers.
    /// </remarks>
    public bool HasRecognizedSeekerWin { get; set; }
    
    /// <summary>
    /// Indicates whether hiders' winning has been recognized.
    /// </summary>
    /// <remarks>
    /// Meaningless if not the host. Also, there
    /// is no custom handling for host transfers.
    /// </remarks>
    public bool HasRecognizedHiderWin { get; set; }
    
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
    /// <exception cref="InvalidOperationException">
    /// Thrown if the online game mode is <see cref="ArenaOnlineGameMode"/>
    /// and <see cref="HideAndSeekMode"/> is not registered.
    /// </exception>
    public static bool IsHideAndSeekMode([NotNullWhen(true)] out HideAndSeekMode? hideAndSeek)
    {
        hideAndSeek = null;
        
        if (!RainMeadow.RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
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
    
    public void ResetCustomHostSessionData()
    {
        Logger.Mark();
        HasRecognizedSeekerWin = false;
        HasRecognizedHiderWin = false;
    }
    
    /// <summary>
    /// If <paramref name="seekersWon"/> is <see langword="true"/>:<br/>
    /// - (3) initial seekers<br/>
    /// - (2) initial hiders<br/>
    /// - (1) spectators<br/>
    /// - (0) <see langword="null"/> players<br/><br/>
    /// Otherwise, if <paramref name="seekersWon"/> is <see langword="false"/>:<br/>
    /// - (4) current hiders<br/>
    /// - (3) initial hiders<br/>
    /// - (2) current seekers<br/>
    /// - (1) spectators<br/>
    /// - (0) <see langword="null"/> players<br/>
    /// </summary>
    public int GetSessionRanking(OnlinePlayer? oPlayer, bool seekersWon)
    {
        if (oPlayer is null)
        {
            Logger.Debug("Player is null (0)");
            return 0;
        }
        
        ArenaClientSettings? arenaClientData = ArenaHelpers.GetDataSettings<ArenaClientSettings>(oPlayer);
        Assert(arenaClientData is not null);
        
        if (arenaClientData.playingAs == RainMeadow.RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
        {
            Logger.Debug($"{oPlayer} is a spectator (1)");
            return 1;
        }
        
        if (seekersWon)
        {
            if (!oPlayer.IsAnInitialSeeker)
            {
                Logger.Debug($"{oPlayer} is not an initial seeker (2)");
                return 2;
            }
            Logger.Debug($"{oPlayer} is an initial seeker (3)");
            return 3;
        }
        else
        {
            if (oPlayer.IsAnInitialSeeker)
            {
                Logger.Debug($"{oPlayer} is an initial seeker (2)");
                return 2;
            }
            if (oPlayer.IsSeeker)
            {
                Logger.Debug($"{oPlayer} is a seeker (3)");
                return 3;
            }
            
            Logger.Debug($"{oPlayer} is a hider (4)");
            return 4;
        }
    }
    
    public override bool PlayerSessionResultSort(
        ArenaOnlineGameMode __,
        On.ArenaSitting.orig_PlayerSessionResultSort? orig,
        ArenaSitting self,
        ArenaSitting.ArenaPlayer a,
        ArenaSitting.ArenaPlayer b)
    {
        bool seekersWon = ArenaOnlineHelper.GetPlayingOPlayers()
            .All(oPlayer => oPlayer.IsSeeker);
        
        Logger.Info($"seekers won: {seekersWon}");
        
        OnlinePlayer? oPlayerA = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(ArenaOnline, a.playerNumber);
        OnlinePlayer? oPlayerB = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(ArenaOnline, b.playerNumber);
        
        Logger.Debug($"{oPlayerA} score: {a.score}");
        Logger.Debug($"{oPlayerB} score: {b.score}");
        
        int rankingA = GetSessionRanking(oPlayerA, seekersWon);
        int rankingB = GetSessionRanking(oPlayerB, seekersWon);
        
        if (rankingA != rankingB)
        {
            Logger.Info(
                rankingA > rankingB
                    ? $"{oPlayerA} is better than {oPlayerB}"
                    : $"{oPlayerA} is not better than {oPlayerB}"
            );
            return rankingA > rankingB;
        }
        
        Logger.Info(
            a.score > b.score
                ? $"{oPlayerA} is better than {oPlayerB}"
                : $"{oPlayerA} is not better than {oPlayerB}"
        );
        
        return a.score > b.score;
    }
    
    public override void ArenaSessionCtor(
        ArenaOnlineGameMode __,
        On.ArenaGameSession.orig_ctor orig,
        ArenaGameSession self,
        RainWorldGame game)
    {
        ResetCustomHostSessionData();
        base.ArenaSessionCtor(ArenaOnline, orig, self, game);
    }
    
    public override void ArenaSessionUpdate(
        On.ArenaGameSession.orig_Update orig,
        ArenaGameSession self,
        ArenaOnlineGameMode __)
    {
        if (OnlineManager.lobby!.isOwner)
        {
            bool seekersWon = ArenaOnlineHelper.GetPlayingOPlayers()
                .All(oPlayer => oPlayer.IsSeeker);
            
            if (seekersWon && !HasRecognizedSeekerWin)
            {
                ChatLogRpcs.SystemLogSeekerWinRpc(null);
                foreach (OnlinePlayer oPlayer in OnlineManager.players.Where(p => !p.isMe))
                    oPlayer.InvokeRPC(ChatLogRpcs.SystemLogSeekerWinRpc);
            }
            
            if (IsSeekingTimeOver && !HasRecognizedHiderWin)
            {
                ChatLogRpcs.SystemLogHiderWinRpc(null);
                foreach (OnlinePlayer oPlayer in OnlineManager.players.Where(p => !p.isMe))
                    oPlayer.InvokeRPC(ChatLogRpcs.SystemLogHiderWinRpc);
            }
        }
        
        base.ArenaSessionUpdate(orig, self, ArenaOnline);
    }
    
    /// <summary>
    /// Calculates the scores for all players in the <paramref name="arenaSitting"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if not in a game.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    /// <remarks>Handles host and client logic automatically.</remarks>
    public void CalculateSessionFinalStats(ArenaSitting arenaSitting, ArenaGameSession arenaSession)
    {
        bool seekersWon = ArenaOnlineHelper.GetPlayingOPlayers()
            .All(oPlayer => oPlayer.IsSeeker);
        
        foreach (ArenaSitting.ArenaPlayer arenaPlayer in arenaSitting.players)
        {
            OnlinePlayer? oPlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(ArenaOnline, arenaPlayer.playerNumber);
            if (oPlayer is null) continue;
            
            if (arenaPlayer.playerClass == RainMeadow.RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
            {
                ArenaOnline.ResetPlayerStats(arenaPlayer);
                
                if (OnlineManager.lobby.isOwner)
                    ArenaOnlineHelper.CopyStatsToLobbyData(arenaPlayer, oPlayer);
                
                continue;
            }
            
            ArenaOnlineHelper.CopyStatsFromLobbyData(arenaPlayer, oPlayer);
            
            arenaPlayer.winner = false;
            arenaPlayer.allKills.AddRange(arenaPlayer.roundKills);
            arenaPlayer.alive = arenaSession.EndOfSessionLogPlayerAsAlive(arenaPlayer.playerNumber);
            
            if (seekersWon && oPlayer.IsAnInitialSeeker)
            {
                arenaPlayer.score += LobbyData.SeekerWinScore;
                arenaPlayer.winner = true;
            }
            else if (!seekersWon && !oPlayer.IsSeeker)
            {
                arenaPlayer.score += LobbyData.HiderWinScore;
                arenaPlayer.winner = true;
            }
            
            if (arenaPlayer.winner)
                arenaPlayer.wins++;
            
            arenaPlayer.totScore += arenaPlayer.score;
            
            if (OnlineManager.lobby.isOwner)
                ArenaOnlineHelper.CopyStatsToLobbyData(arenaPlayer, oPlayer);
        }
        
        List<ArenaSitting.ArenaPlayer> sortedPlayers = [];
        foreach (ArenaSitting.ArenaPlayer player in arenaSitting.players)
        {
            bool isInserted = false;
            for (int i = 0; i < sortedPlayers.Count; ++i)
            {
                if (arenaSitting.PlayerSessionResultSort(player, sortedPlayers[i]))
                {
                    sortedPlayers.Insert(i, player);
                    isInserted = true;
                    break;
                }
            }
            if (!isInserted)
                sortedPlayers.Add(player);
        }
        
        arenaSession.game.arenaOverlay = new ArenaOverlay(
            arenaSession.game.manager,
            arenaSitting,
            sortedPlayers
        );
        arenaSession.game.manager.sideProcesses.Add(
            arenaSession.game.arenaOverlay
        );
    }
    
    public override void ArenaSessionEnded(
        ArenaOnlineGameMode __,
        On.ArenaSitting.orig_SessionEnded orig,
        ArenaSitting self,
        ArenaGameSession arenaSession)
    {
        CalculateSessionFinalStats(self, arenaSession);
    }
    
    /// <inheritdoc cref="CanStartNewGame(out string)"/>
    public bool CanStartNewGame()
    {
        return CanStartNewGame(out _);
    }
    
    /// <summary>
    /// Determines if the host is able to start a new game
    /// </summary>
    public bool CanStartNewGame([NotNullWhen(false)] out string? failureReason)
    {
        failureReason = null;
        
        List<string> failureReasons = [];
        
        if (LobbyData.EnabledSeekerSelection == SeekerSelection.Random)
        {
            
            // Logic is coupled to SelectRandomSeekers.
            int readyPlayerCount = OnlineManager.players
                .Count(IsReady);
            int selectablePlayerCount = OnlineManager.players
                .Count(player => IsReady(player) && IsWillingToSeek(player));
            
            if (selectablePlayerCount < LobbyData.SeekerCount)
                failureReasons.Add($"There are not enough selectable players ({selectablePlayerCount}).");
            if (readyPlayerCount <= LobbyData.SeekerCount)
                failureReasons.Add($"There are not enough ready players ({readyPlayerCount}) to have at least 1 hider.");
        }
        else
        {
            if (LobbyData.Seekers.Count == 0)
                failureReasons.Add("There are no seekers selected.");
        }
        
        if (failureReasons.Count > 0)
        {
            failureReason = string.Join(" ", failureReasons);
            return false;
        }
        
        return true;
        
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
        bool areAllPlayersSeekers = ArenaOnlineHelper.GetPlayingOPlayers()
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
        if (!GameHelper.IsInGame)
        {
            Logger.Debug($"Not currently in game. {fromText}");
            return;
        }
        if (!hideAndSeek.CanTagPlayer(tagger, target, out string? failureReason))
        {
            Logger.Debug($"{tagger} is trying to tag {target} when they cannot. Reason: {failureReason} {fromText}");
            return;
        }
        
        // Me when half the code is just to get the values to perform logic on
        ArenaOnlineGameMode arenaOnline  = hideAndSeek.ArenaOnline;
        ArenaGameSession    arenaSession = hideAndSeek.ArenaOnline.session;
        ArenaSitting        arenaSitting = hideAndSeek.ArenaOnline.session.arenaSitting;
        
        int taggerIndex = ArenaHelpers.FindOnlinePlayerNumber(arenaOnline, tagger);
        int targetIndex = ArenaHelpers.FindOnlinePlayerNumber(arenaOnline, target);
        
        bool isTargetLastHider = !ArenaOnlineHelper.GetPlayingOPlayers()
            .Any(oPlayer => !oPlayer.IsSeeker && oPlayer != target);
        
        ArenaSitting.ArenaPlayer taggerArenaPlayer = arenaSitting.players[taggerIndex];
        ArenaSitting.ArenaPlayer targetArenaPlayer = arenaSitting.players[targetIndex];
        IconSymbol.IconSymbolData trophy = CreatureSymbol.SymbolDataFromCreature(arenaSession.Players[targetIndex]);
        
        Logger.Mark(hideAndSeek.HasRecognizedSeekerWin);
        
        if (hideAndSeek.HasRecognizedSeekerWin &&
            !hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled)
        {
            Logger.Error(
                $"Seeker win has already been recognized. " +
                $"Continuing in the hopes that this is a minor bug. {fromText}"
            );
        }
        
        // Actual logic here
        Logger.Debug($"Making {target} a seeker. {fromText}");
        hideAndSeek.LobbyData.Seekers.Add(target);
        
        ArenaOnlineHelper.CopyStatsFromLobbyData(taggerArenaPlayer, tagger);
        ArenaOnlineHelper.CopyStatsFromLobbyData(targetArenaPlayer, target);
        
        taggerArenaPlayer.score += hideAndSeek.LobbyData.SeekerTagScore;
        taggerArenaPlayer.roundKills.Add(trophy);
        targetArenaPlayer.deaths++;
        
        ArenaOnlineHelper.CopyStatsToLobbyData(taggerArenaPlayer, tagger);
        ArenaOnlineHelper.CopyStatsToLobbyData(targetArenaPlayer, target);
        
        if (isTargetLastHider)
        {
            ChatLogRpcs.SystemLogLastPlayerTaggedRpc(null, tagger, target);
            foreach (OnlinePlayer oPlayer in OnlineManager.players.Where(p => !p.isMe))
                oPlayer.InvokeRPC(ChatLogRpcs.SystemLogLastPlayerTaggedRpc, tagger, target);
        }
        else
        {
            ChatLogRpcs.SystemLogPlayerTaggedRpc(null, tagger, target);
            foreach (OnlinePlayer oPlayer in OnlineManager.players.Where(p => !p.isMe))
                oPlayer.InvokeRPC(ChatLogRpcs.SystemLogPlayerTaggedRpc, tagger, target);
        }
    }
    
    /// <summary>
    /// Selects n valid players to be seekers. Where
    /// n is <see cref="HideAndSeekLobbyData.SeekerCount"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:<br/>
    /// - Me player is not the host.<br/>
    /// - <see cref="SeekerSelection"/> is not <see cref="SeekerSelection.Random"/>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    /// <remarks>
    /// This method does not throw exceptions for some invalid
    /// states.<br/> For instance: this is fine with not selecting
    /// enough seekers if there are not enough selectable
    /// players. This is also fine with selecting ALL players.
    /// </remarks>
    public void SelectRandomSeekers()
    {
        if (!OnlineManager.lobby!.isOwner)
            throw new InvalidOperationException("You must be the host to select random seekers.");
        if (LobbyData.EnabledSeekerSelection != SeekerSelection.Random)
            throw new InvalidOperationException($"Cannot select random seekers when selection mode is '{LobbyData.EnabledSeekerSelection}'.");
        
        // Logic is coupled to CanStartNewGame.
        List<OnlinePlayer> selectablePlayers = OnlineManager.players
            .Where(oPlayer => IsReady(oPlayer) && IsWillingToSeek(oPlayer)) 
            .ToList();
        
        selectablePlayers.Shuffle();
        
        List<OnlinePlayer> selectedSeekers = selectablePlayers
            .Take(LobbyData.SeekerCount)
            .ToList();
        
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