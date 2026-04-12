using RainMeadow;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using UnityEngine.Assertions;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed partial class HideAndSeekMode // HideAndSeekMode.UI
{
    public HideAndSeekSettingsTab? SettingsTab { get; private set; }
    
    /// <summary>Fully initializes all UI elements.</summary>
    /// <exception cref="AssertionException">Thrown if <see cref="SettingsTab"/> is not <see langword="null"/></exception>
    public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
    {
        Logger.Mark();
        Assert(SettingsTab is null);
        AssertIs(OnlineManager.lobby?.gameMode, out ArenaOnlineGameMode onlineArena);
        
        base.OnUIEnabled(menu);
        SettingsTab = new HideAndSeekSettingsTab(
            menu,
            this,
            onlineArena
        );
        
        menu.arenaMainLobbyPage.tabContainer.AddTab(SettingsTab, Plugin.Name);
    }
    
    /// <summary>Fully removes all UI elements.</summary>
    /// <exception cref="AssertionException">
    /// Thrown if the <see cref="Lobby"/> is <see langword="null"/>
    /// or if the game mode is not <see cref="ArenaOnlineGameMode"/>
    /// </exception>
    public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
    {
        Logger.Mark();
        Assert(OnlineManager.lobby?.gameMode is ArenaOnlineGameMode);
        
        if (SettingsTab is null) return;
        
        base.OnUIDisabled(menu);
        
        SettingsTab.RemoveSprites();
        menu.arenaMainLobbyPage.tabContainer.RemoveTab(SettingsTab);
        
        SettingsTab = null;
    }
    
    /// <remarks>
    /// Calls <see cref="OnUIDisabled"/> because it fully handles UI removal.
    /// <see cref="ExternalArenaGameMode.OnUIShutDown"/> only performs a partial removal.
    /// </remarks>
    /// <exception cref="AssertionException">Thrown if propagated from <see cref="OnUIDisabled"/>.</exception>
    public override void OnUIShutDown(ArenaOnlineLobbyMenu menu) => OnUIDisabled(menu);
}