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
    
    public Color ErrorColor => Color.gray;
    public Color InitialSeekerUIColor => LobbyData.SeekerBodyColor;
    public Color InfectedSeekerUIColor => InitialSeekerUIColor.ToHSL().lightness < 0.7f
                                              ? Color.Lerp(InitialSeekerUIColor, Color.white, 0.32f)
                                              : Color.Lerp(InitialSeekerUIColor, Color.black, 0.42f);
    public Color HiderUIColor { get; } = new(0.10f, 0.60f, 0.10f);
    
    /// <summary>
    /// Used to determine if the key bind for toggling seekers via the
    /// <see cref="SlugcatColorableButton"/>s in the arena menu is pressed.
    /// </summary>
    private bool _isToggleSeekerPressed { get; set; }
    /// <summary>
    /// Used to remember the value of
    /// <see cref="_isToggleSeekerPressed"/> last UI Update.
    /// </summary>
    private bool _previousIsToggleSeekerPressed { get; set; }
    
    public HideAndSeekSettingsTab? SettingsTab { get; private set; }
    
    /// <summary>
    /// Gets the <see cref="Color"/> the <see cref="OnlinePlayer"/> is associated with in UI.
    /// </summary>
    /// <remarks>
    /// This does not handle spectators. Ensure a separate check
    /// is used if spectators need to be handled differently.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered.</exception>
    public Color GetPlayerUIColor(OnlinePlayer oPlayer)
    {
        if (oPlayer.IsAnInitialSeeker && !oPlayer.IsSeeker)
        {
            Logger.Error($"Player ({oPlayer}) cannot logically be an initial seeker but not a seeker.");
            return ErrorColor;
        }
        
        if (oPlayer.IsAnInitialSeeker)
            return InitialSeekerUIColor;
        else if (oPlayer.IsAnInfectedSeeker)
            return InfectedSeekerUIColor;
        else
            return HiderUIColor;
    }
    
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
        Color color = GetPlayerUIColor(oPlayer);
        
        // Note: On the first IconColor call, the UI elements are not initialized.
        display.arrowSprite?.color = color;
        display.username?.color = color;
        
        return color;
    }
    
    public override Color GetPortraitColor(ArenaOnlineGameMode __, OnlinePlayer? oPlayer, Color originalColor)
    {
        if (oPlayer is null)
            return base.GetPortraitColor(ArenaOnline, oPlayer, originalColor);
        
        ArenaClientSettings? clientData = ArenaHelpers.GetDataSettings<ArenaClientSettings>(oPlayer);
        if (clientData is null) // Null when first joining the lobby.
            return base.GetPortraitColor(ArenaOnline, oPlayer, originalColor);
        
        
        if (clientData.playingAs == RainMeadow.RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator)
            return originalColor;
        
        return GetPlayerUIColor(oPlayer);
    }
    
    public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
    {
        Logger.Mark();
        Assert(SettingsTab is null);
        AssertIs(OnlineManager.lobby!.gameMode, out ArenaOnlineGameMode arenaOnline);
        
        if (OnlineManager.lobby!.isOwner)
        {
            LobbyData.Seekers = [];
            LobbyData.InitialSeekers = [];
        }
        
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
        _previousIsToggleSeekerPressed = _isToggleSeekerPressed;
        _isToggleSeekerPressed = RWInput.PlayerInput(0).pckp;
        
        if (_isToggleSeekerPressed && !_previousIsToggleSeekerPressed && !ArenaOnline.initiateLobbyCountdown)
        {
            List<ArenaPlayerBox> arenaPlayerBoxes = menu.arenaMainLobbyPage.playerDisplayer.buttons
                                                        .Cast<ArenaPlayerBox>()
                                                        .ToList();
            
            if (menu.selectedObject is SlugcatColorableButton slugcatButton)
            {
                ArenaPlayerBox? playerBox = arenaPlayerBoxes.FirstOrDefault(box => box.slugcatButton == slugcatButton);
                
                if (playerBox is null)
                {
                    Logger.Warning(
                        $"""
                         Unable to find the arena player box that owns the slugcat button.
                         - selected object: {menu.selectedObject}
                         - player boxes: [ {string.Join(", ", arenaPlayerBoxes)} ]
                         """
                    );
                    base.OnUIUpdate(menu);
                    return;
                }
                
                OnlinePlayer target = playerBox.profileIdentifier;
                
                if (CanSelectSeeker(LobbyData.EnabledSeekerSelection, OnlineManager.mePlayer, target))
                    ToggleSelectSeeker(target);
            }
        }
        
        base.OnUIUpdate(menu);
    }
    
    public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
    {
        Logger.Mark();
        
        if (SettingsTab is null) return;
        
        _previousIsToggleSeekerPressed = false;
        _isToggleSeekerPressed = false;
        
        SettingsTab.RemoveSprites();
        menu.arenaMainLobbyPage.tabContainer.RemoveTab(SettingsTab);
        
        SettingsTab = null;
        base.OnUIDisabled(menu);
    }
    
    public override void OnUIShutDown(ArenaOnlineLobbyMenu menu) => OnUIDisabled(menu);
}