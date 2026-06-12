using RainMeadow;

namespace OneLetterShor.HideAndSeek.Utils;

internal static class ArenaOnlineHelper
{
    /// <summary>
    /// Finds all the <see cref="OnlinePlayer"/>s who
    /// are in the actual game and not a spectator.
    /// </summary>
    internal static List<OnlinePlayer> GetPlayingOPlayers(ArenaOnlineGameMode arenaOnline)
    {
        List<OnlinePlayer> playingOPlayers = [];
        
        ArenaSitting arenaSitting = arenaOnline.session.arenaSitting;
        
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