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
            OnlinePlayer? oPlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arenaOnline, arenaPlayer.playerNumber);
            Assert(oPlayer is not null);
            
            ArenaClientSettings? clientData = ArenaHelpers.GetDataSettings<ArenaClientSettings>(oPlayer);
            Assert(clientData is not null);
            
            if (clientData.playingAs != RainMeadow.RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
                playingOPlayers.Add(oPlayer);
        }
        
        return playingOPlayers;
    }
}