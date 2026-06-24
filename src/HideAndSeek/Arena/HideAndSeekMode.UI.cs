using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;
using RainMeadow.UI;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed partial class HideAndSeekMode // HideAndSeekMode.UI
{
    public const string
        SeekerAtlasElementName = "HunterA",
        HiderAtlasElementName = "SaintA";
    public static Color SeekerUIColor { get; } = new(0.80f, 0.10f, 0.10f);
    public static Color HiderUIColor { get; }  = new(0.10f, 0.60f, 0.10f);
    
    public HideAndSeekSettingsTab? SettingsTab { get; private set; }
    
    public override string AddIcon(
        ArenaOnlineGameMode __,
        OnlinePlayerDisplay display,
        PlayerSpecificOnlineHud onlineHud,
        SlugcatCustomization customization,
        OnlinePlayer oPlayer)
    {
        if (customization.globalMute)
            return base.AddIcon(ArenaOnline, display, onlineHud, customization, oPlayer);
        
        return oPlayer.IsSeeker
            ? SeekerAtlasElementName
            : HiderAtlasElementName;
    }
    
    public override Color IconColor(
        ArenaOnlineGameMode __,
        OnlinePlayerDisplay display,
        PlayerSpecificOnlineHud onlineHud,
        SlugcatCustomization customization,
        OnlinePlayer oPlayer)
    {
        Color color = oPlayer.IsSeeker
            ? SeekerUIColor
            : HiderUIColor;
        
        // Note: On the first IconColor call, the UI elements are not initialized.
        display.arrowSprite?.color = color;
        display.username?.color = color;
        
        return color;
    }
    
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
        
        if (SettingsTab is null) return;
        
        SettingsTab.RemoveSprites();
        menu.arenaMainLobbyPage.tabContainer.RemoveTab(SettingsTab);
        
        SettingsTab = null;
        base.OnUIDisabled(menu);
    }
    
    public override void OnUIShutDown(ArenaOnlineLobbyMenu menu) => OnUIDisabled(menu);
}