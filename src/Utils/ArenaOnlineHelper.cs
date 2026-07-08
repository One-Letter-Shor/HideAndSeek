using OneLetterShor.HideAndSeek.Arena;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Utils;

internal static class ArenaOnlineHelper
{
    /// <summary>
    /// Finds all the <see cref="OnlinePlayer"/>s who
    /// are in the actual game and not a spectator.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:<br/>
    /// - The online game mode is not <see cref="ArenaOnlineGameMode"/>.<br/>
    /// - The external game mode is not <see cref="HideAndSeekMode"/>.
    /// - The me player is not in-game.
    /// </exception>
    internal static List<OnlinePlayer> GetPlayingOPlayers()
    {
        if (!RainMeadow.RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
            throw new InvalidOperationException($"The online game mode ({OnlineManager.lobby.gameMode}) is not arena.");
        if (!HideAndSeekMode.IsHideAndSeekMode(out _))
            throw new InvalidOperationException($"The external game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
        if (!GameHelper.IsInGame)
            throw new InvalidOperationException("Not in a game.");
        
        List<OnlinePlayer> playingOPlayers = [];
        
        ArenaSitting arenaSitting = arenaOnline.session!.arenaSitting;
        
        foreach (ArenaSitting.ArenaPlayer arenaPlayer in arenaSitting.players)
        {
            // TODO: Understand why oPlayer is null when they exit to lobby. (that or the oPlayer WHO exits to the lobby sees everyone else as null)
            OnlinePlayer? oPlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, arenaPlayer.playerNumber);
            if (oPlayer is null)
                continue;
            
            ArenaClientSettings? clientData = ArenaHelpers.GetDataSettings<ArenaClientSettings>(oPlayer);
            Assert(clientData is not null);
            
            if (clientData.playingAs != RainMeadow.RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                playingOPlayers.Add(oPlayer);
        }
        
        return playingOPlayers;
    }
    
    /// <exception cref="InvalidOperationException">
    /// Thrown when:<br/>
    /// - The online game mode is not <see cref="ArenaOnlineGameMode"/>.<br/>
    /// - The external game mode is not <see cref="HideAndSeekMode"/>.
    /// - Me player is not the host.
    /// </exception>
    internal static void CopyStatsToLobbyData(
        ArenaSitting.ArenaPlayer arenaPlayer,
        OnlinePlayer oPlayer)
    {
        if (!RainMeadow.RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
            throw new InvalidOperationException($"The online game mode ({OnlineManager.lobby.gameMode}) is not arena.");
        if (!HideAndSeekMode.IsHideAndSeekMode(out _))
            throw new InvalidOperationException($"The external game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
        if (!OnlineManager.lobby!.isOwner)
            throw new InvalidOperationException("Only the host can write to lobby data.");
        
        ushort inLobbyId = oPlayer.inLobbyId;
        
        arenaOnline.playerNumberWithWins[inLobbyId]             = arenaPlayer.wins;
        arenaOnline.playerNumberWithDeaths[inLobbyId]           = arenaPlayer.deaths;
        arenaOnline.playerNumberWithScore[inLobbyId]            = arenaPlayer.score;
        arenaOnline.playerTotScore[inLobbyId]                   = arenaPlayer.totScore;
        arenaOnline.playerNumberWithTrophiesPerRound[inLobbyId] = arenaPlayer.roundKills.Select(trophy => trophy.ToString()).ToList();
        arenaOnline.playerNumberWithTrophies[inLobbyId]         = arenaPlayer.allKills.Select(trophy => trophy.ToString()).ToList();
    }
    
    /// <exception cref="InvalidOperationException">
    /// Thrown when:<br/>
    /// - The online game mode is not <see cref="ArenaOnlineGameMode"/>.<br/>
    /// - The external game mode is not <see cref="HideAndSeekMode"/>.
    /// </exception>
    internal static void CopyStatsFromLobbyData(
        ArenaSitting.ArenaPlayer arenaPlayer,
        OnlinePlayer oPlayer)
    {
        if (!RainMeadow.RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
            throw new InvalidOperationException($"The online game mode ({OnlineManager.lobby.gameMode}) is not arena.");
        if (!HideAndSeekMode.IsHideAndSeekMode(out _))
            throw new InvalidOperationException($"The external game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
        
        ushort inLobbyId = oPlayer.inLobbyId;
        
        if (!arenaOnline.playerNumberWithWins.TryGetValue(inLobbyId, out int wins))
            Logger.Warning($"Unable to find wins for {oPlayer}.");
        if (!arenaOnline.playerNumberWithDeaths.TryGetValue(inLobbyId, out int deaths))
            Logger.Warning($"Unable to find deaths for {oPlayer}.");
        if (!arenaOnline.playerNumberWithScore.TryGetValue(inLobbyId, out int score))
            Logger.Warning($"Unable to find score for {oPlayer}.");
        if (!arenaOnline.playerTotScore.TryGetValue(inLobbyId, out int totalScore))
            Logger.Warning($"Unable to find total score for {oPlayer}.");
        if (!arenaOnline.playerNumberWithTrophiesPerRound.TryGetValue(inLobbyId, out List<string>? roundKills))
            Logger.Warning($"Unable to find round kills for {oPlayer}.");
        if (!arenaOnline.playerNumberWithTrophies.TryGetValue(inLobbyId, out List<string>? allKills))
            Logger.Warning($"Unable to find all kills for {oPlayer}.");
        
        roundKills ??= [];
        allKills   ??= [];
        
        arenaPlayer.wins       = wins;
        arenaPlayer.deaths     = deaths;
        arenaPlayer.score      = score;
        arenaPlayer.totScore   = totalScore;
        arenaPlayer.roundKills = roundKills.Select(IconSymbol.IconSymbolData.IconSymbolDataFromString).ToList();
        arenaPlayer.allKills   = allKills.Select(IconSymbol.IconSymbolData.IconSymbolDataFromString).ToList();
    }
}