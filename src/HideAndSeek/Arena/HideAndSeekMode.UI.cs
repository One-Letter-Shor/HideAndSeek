using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;
using RainMeadow.UI;
using RainMeadow.UI.Components;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed partial class HideAndSeekMode // HideAndSeekMode.UI
{
    public const string
        SeekerAtlasElementName = "HunterA",
        HiderAtlasElementName = "SaintA";
    public Color SeekerUIColor => LobbyData.SeekerBodyColor;
    public Color HiderUIColor { get; } = new(0.10f, 0.60f, 0.10f);
    
    /// <summary>
    /// Used to determine if the key bind for toggling seekers via the
    /// <see cref="SlugcatColorableButton"/>s in the arena menu is pressed.
    /// </summary>
    private bool IsToggleSeekerPressed { get; set; }
    /// <summary>
    /// Used to remember the value of
    /// <see cref="IsToggleSeekerPressed"/> last UI Update.
    /// </summary>
    private bool PreviousIsToggleSeekerPressed { get; set; }
    
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
    
    public override Color GetPortraitColor(ArenaOnlineGameMode __, OnlinePlayer? oPlayer, Color originalColor)
    {
        if (oPlayer is null)
        {
            Logger.Warning($"Null player. Color: {originalColor}");
            return base.GetPortraitColor(ArenaOnline, oPlayer, originalColor);
        }
        
        ArenaClientSettings? clientData = ArenaHelpers.GetDataSettings<ArenaClientSettings>(oPlayer);
        if (clientData is null) // Null when first joining the lobby.
            return base.GetPortraitColor(ArenaOnline, oPlayer, originalColor);
        
        if (clientData.playingAs == RainMeadow.RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
            return originalColor;
        else
            return oPlayer.IsSeeker
                ? SeekerUIColor
                : HiderUIColor;
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
    
    public override void OnUIUpdate(ArenaOnlineLobbyMenu menu)
    {
        PreviousIsToggleSeekerPressed = IsToggleSeekerPressed;
        IsToggleSeekerPressed = RWInput.PlayerInput(0).pckp;
        
        if (IsToggleSeekerPressed && !PreviousIsToggleSeekerPressed && !ArenaOnline.initiateLobbyCountdown)
        {
            List<ArenaPlayerBox> arenaPlayerBoxes = menu.arenaMainLobbyPage.playerDisplayer.buttons
                                                        .Cast<ArenaPlayerBox>()
                                                        .ToList();
            
            if (menu.selectedObject is SlugcatColorableButton slugcatButton)
            {
                ArenaPlayerBox? playerBox = arenaPlayerBoxes.FirstOrDefault(box => box.slugcatButton == slugcatButton);
                
                if (playerBox is null)
                {
                    Logger.Error(
                        $"""
                         Unable to find the arena player box that owns the slugcat button.
                         - selected object: {menu.selectedObject}
                         - player boxes: [ {string.Join(", ", arenaPlayerBoxes)} ]
                         """
                    );
                    base.OnUIUpdate(menu);
                    return;
                }
                
                OnlinePlayer oPlayer = playerBox.profileIdentifier;
                
                if (LobbyData.EnabledSeekerSelection == SeekerSelection.Host &&
                    OnlineManager.lobby!.isOwner ||
                    LobbyData.EnabledSeekerSelection == SeekerSelection.Self &&
                    oPlayer.isMe)
                {
                    ToggleSeeker(oPlayer);
                }
            }
        }
        
        base.OnUIUpdate(menu);
    }
    
    public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
    {
        Logger.Mark();
        
        if (SettingsTab is null) return;
        
        PreviousIsToggleSeekerPressed = false;
        IsToggleSeekerPressed = false;
        
        SettingsTab.RemoveSprites();
        menu.arenaMainLobbyPage.tabContainer.RemoveTab(SettingsTab);
        
        SettingsTab = null;
        base.OnUIDisabled(menu);
    }
    
    public override void OnUIShutDown(ArenaOnlineLobbyMenu menu) => OnUIDisabled(menu);
}