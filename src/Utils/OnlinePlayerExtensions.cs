using OneLetterShor.HideAndSeek.Arena;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Utils;

public static class OnlinePlayerExtensions
{
    extension(OnlinePlayer self)
    {
        /// <summary>
        /// Checks if the <see cref="OnlinePlayer"/> is a seeker or not.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when:<br/>
        /// - The online game mode is not <see cref="ArenaOnlineGameMode"/>.<br/>
        /// - The external game mode is not <see cref="HideAndSeekMode"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
        public bool IsSeeker
        {
            get
            {
                if (!RainMeadow.RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
                    throw new InvalidOperationException($"The online game mode ({OnlineManager.lobby.gameMode}) is not arena.");
                
                if (!HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
                    throw new InvalidOperationException($"The external game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
                
                return hideAndSeek.LobbyData.Seekers.Contains(self);
            }
        }
        
        /// <summary>
        /// Checks if the <see cref="OnlinePlayer"/> was an initial seeker.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when:<br/>
        /// - The online game mode is not <see cref="ArenaOnlineGameMode"/>.<br/>
        /// - The external game mode is not <see cref="HideAndSeekMode"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
        public bool IsAnInitialSeeker
        {
            get
            {
                if (!RainMeadow.RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
                    throw new InvalidOperationException($"The online game mode ({OnlineManager.lobby.gameMode}) is not arena.");
                
                if (!HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
                    throw new InvalidOperationException($"The external game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
                
                return hideAndSeek.LobbyData.InitialSeekers.Contains(self);
            }
        }
        
        /// <summary>
        /// Checks if the <see cref="OnlinePlayer"/> was an infected seeker.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when:<br/>
        /// - The online game mode is not <see cref="ArenaOnlineGameMode"/>.<br/>
        /// - The external game mode is not <see cref="HideAndSeekMode"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
        /// <remarks>Equivalent to <c>IsSeeker &amp;&amp; !IsAnInfectedSeeker</c>.</remarks>
        public bool IsAnInfectedSeeker => self.IsSeeker && !self.IsAnInitialSeeker;
    }
}