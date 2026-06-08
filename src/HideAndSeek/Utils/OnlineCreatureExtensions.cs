using OneLetterShor.HideAndSeek.Arena;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Utils;

public static class OnlineCreatureExtensions
{
    extension(OnlineCreature self)
    {
        /// <summary>
        /// Determines if this <see cref="OnlineCreature"/> can tag another.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if both <see cref="OnlineCreature"/>s are avatars,
        /// this <see cref="OnlineCreature"/> is a seeker, and the other
        /// <see cref="OnlineCreature"/> is not a seeker. Otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when:<br/>
        /// - The online game mode is not <see cref="ArenaOnlineGameMode"/>.<br/>
        /// - The external game mode is not <see cref="HideAndSeekMode"/>.
        /// </exception>
        /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
        public bool CanTag(OnlineCreature other)
        {
            return self  is { isAvatar: true, owner.IsSeeker: true  } &&
                   other is { isAvatar: true, owner.IsSeeker: false };
        }
    }
}