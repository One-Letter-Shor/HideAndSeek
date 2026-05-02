using RainMeadow;
using RainMeadow.UI;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed partial class HideAndSeekMode // HideAndSeekMode.UI
{
    public HideAndSeekSettingsTab? SettingsTab { get; private set; }
    
    /// <summary>Fully initializes all UI elements.</summary>
    public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
    {
        Logger.Mark();
        Assert(SettingsTab is null);
        AssertIs(OnlineManager.lobby?.gameMode, out ArenaOnlineGameMode arenaOnline);
        
        base.OnUIEnabled(menu);
        SettingsTab = new HideAndSeekSettingsTab(
            menu,
            arenaOnline,
            this
        );
        
        menu.arenaMainLobbyPage.tabContainer.AddTab(SettingsTab, Plugin.Name);
    }
    
    /// <summary>Fully removes all UI.</summary>
    public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
    {
        Logger.Mark();
        Assert(OnlineManager.lobby?.gameMode is ArenaOnlineGameMode);
        
        if (SettingsTab is null) return;
        
        SettingsTab.RemoveSprites();
        menu.arenaMainLobbyPage.tabContainer.RemoveTab(SettingsTab);
        
        SettingsTab = null;
        base.OnUIDisabled(menu);
    }
    
    /// <remarks>
    /// Calls <see cref="OnUIDisabled"/> because it fully handles UI removal.
    /// <see cref="ExternalArenaGameMode.OnUIShutDown"/> only performs a partial removal.
    /// </remarks>
    public override void OnUIShutDown(ArenaOnlineLobbyMenu menu) => OnUIDisabled(menu);
}