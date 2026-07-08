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
}