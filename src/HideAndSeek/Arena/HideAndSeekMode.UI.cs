using RainMeadow;
using RainMeadow.UI;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed partial class HideAndSeekMode // HideAndSeekMode.UI
{
    public HideAndSeekSettingsTab? SettingsTab { get; private set; }
    
    public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
    {
        Logger.Mark();
        Assert(SettingsTab is null);
        AssertIs(OnlineManager.lobby!.gameMode, out ArenaOnlineGameMode arenaOnline);
        
        base.OnUIEnabled(menu);
        SettingsTab = new HideAndSeekSettingsTab(
            menu,
            arenaOnline,
            this
        );
        
        menu.arenaMainLobbyPage.tabContainer.AddTab(SettingsTab, Plugin.Name);
    }
    
    public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
    {
        Logger.Mark();
        Assert(OnlineManager.lobby!.gameMode is ArenaOnlineGameMode);
        
        if (SettingsTab is null) return;
        
        SettingsTab.RemoveSprites();
        menu.arenaMainLobbyPage.tabContainer.RemoveTab(SettingsTab);
        
        SettingsTab = null;
        base.OnUIDisabled(menu);
    }
    
    public override void OnUIShutDown(ArenaOnlineLobbyMenu menu) => OnUIDisabled(menu);
}