using OneLetterShor.HideAndSeek.Arena;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Utils;

public static class OnlineCreatureExtensions
{
    extension(OnlineCreature self)
    {
        /// <summary>
        /// Determines whether this <see cref="OnlineCreature"/>
        /// can tag the specified <see cref="OnlineCreature"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the following are all true:<br/>
        /// - There is still time for seekers to seek. (It is not about to rain)<br/>
        /// - Both are avatars.<br/>
        /// - This is a seeker.<br/>
        /// - Other is not a seeker.<br/>
        /// Otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when:<br/>
        /// - The online game mode is not <see cref="ArenaOnlineGameMode"/>.<br/>
        /// - The external game mode is not <see cref="HideAndSeekMode"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
        public bool CanTag(OnlineCreature target)
        {
            if (OnlineManager.lobby!.gameMode is not ArenaOnlineGameMode arenaOnline)
                throw new InvalidOperationException($"The online game mode ({OnlineManager.lobby.gameMode}) is not arena.");
            
            if (!HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
                throw new InvalidOperationException($"The external game mode ({arenaOnline.currentGameMode}) is not Hide and Seek.");
            
            return !hideAndSeek.IsSeekingTimeOver &&
                   self   is { isAvatar: true, owner.IsSeeker: true  } &&
                   target is { isAvatar: true, owner.IsSeeker: false };
        }
    }
}