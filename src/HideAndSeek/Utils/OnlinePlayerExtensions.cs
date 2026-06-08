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
                if (OnlineManager.lobby!.gameMode is not ArenaOnlineGameMode arenaOnline)
                    throw new InvalidOperationException($"The online game mode ({OnlineManager.lobby.gameMode}) is not arena.");
                
                if (!HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
                    throw new InvalidOperationException($"The external game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
                
                return hideAndSeek.LobbyData.Seekers.Contains(self);
            }
        }
    }
}