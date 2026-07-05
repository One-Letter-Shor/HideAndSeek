using RWCustom;

namespace OneLetterShor.HideAndSeek.Utils;

internal static class GameHelper
{
    /// <summary>
    /// Checks if the active main process is <see cref="RainWorldGame"/>
    /// </summary>
    internal static bool IsInGame => Custom.rainWorld.processManager.currentMainLoop is RainWorldGame;
}