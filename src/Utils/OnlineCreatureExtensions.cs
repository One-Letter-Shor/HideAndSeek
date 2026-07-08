using OneLetterShor.HideAndSeek.Arena;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Utils;

public static class OnlineCreatureExtensions
{
    extension(OnlineCreature self)
    {
        // TODO: Move summary to remarks and do similar documentation for OnlinePlayerExtensions.
        /// <summary>Wraps <see cref="HideAndSeekMode.CanTagPlayer(OnlinePlayer, OnlinePlayer)"/> for convenience.</summary>
        /// <returns>
        /// <see langword="true"/> when the following are all true:<br/>
        /// - Both <see cref="OnlineCreature"/>s are avatars.<br/>
        /// - <see cref="HideAndSeekMode.CanTagPlayer(OnlinePlayer, OnlinePlayer)"/> returns <see langword="true"/><br/>
        /// Otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when:<br/>s
        /// - The online game mode is not <see cref="ArenaOnlineGameMode"/>.<br/>
        /// - The external game mode is not <see cref="HideAndSeekMode"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
        public bool CanTag(OnlineCreature target)
        {
            if (!RainMeadow.RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
                throw new InvalidOperationException($"The online game mode ({OnlineManager.lobby.gameMode}) is not arena.");
            
            if (!HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
                throw new InvalidOperationException($"The external game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
            
            return self.isAvatar &&
                   target.isAvatar &&
                   hideAndSeek.CanTagPlayer(self.owner, target.owner);
        }
    }
}