using Menu;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using RainMeadow.UI.Components.Patched;
using RWCustom;
using EnumHelper = OneLetterShor.HideAndSeek.Utils.EnumHelper;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed class HideAndSeekSettingsTab : TabContainer.Tab
{
    public ArenaOnlineGameMode ArenaOnline { get; }
    public ArenaOnlineLobbyMenu ArenaOnlineMenu { get; }
    public HideAndSeekMode HideAndSeek { get; }
    
    public ProperlyAlignedMenuLabel IsWillingToSeekLabel { get; }
    public OpCheckBox               IsWillingToSeekCheckBox { get; }
    
    public ProperlyAlignedMenuLabel SeekerCountLabel { get; }
    public OpUpdown                 SeekerCountUpdown { get; }
    public ProperlyAlignedMenuLabel SeekerSelectionLabel { get; }
    public OpResourceSelector       SeekerSelectionSelector { get; }
    public ProperlyAlignedMenuLabel TaggingMethodsLabel { get; }
    public Dictionary<TaggingMethods, (ProperlyAlignedMenuLabel label, OpCheckBox checkBox)> UIByTaggingMethod { get; } = [];
    public ProperlyAlignedMenuLabel TagResultLabel { get; }
    public OpResourceSelector       TagResultSelector { get; }
    
    public MenuLabel SeekerWinScoreLabel { get; }
    public OpUpdown  SeekerWinScoreUpdown { get; }
    public MenuLabel SeekerTagScoreLabel { get; }
    public OpUpdown  SeekerTagScoreUpdown { get; }
    public MenuLabel HiderWinScoreLabel { get; }
    public OpUpdown  HiderWinScoreUpdown { get; }
    
    public MenuLabel     SeekerBodyColorLabel { get; }
    public OpColorPicker SeekerBodyColorPicker { get; }
    public MenuLabel     SeekerEyeColorLabel { get; }
    public OpColorPicker SeekerEyeColorPicker { get; }
    public MenuLabel     SeekerTertiaryColorLabel { get; }
    public OpColorPicker SeekerTertiaryColorPicker { get; }
    
    public UIfocusable[] AllUIFocusables { get; }
    public UIfocusable[] LobbyUIFocusables { get; }
    public UIfocusable[] ClientUIFocusables { get; }
    
    public Dictionary<TaggingMethods, (string friendlyName, string description)> DataByTaggingMethod = new()
    {
        { TaggingMethods.Rock,      ("Rock hit",  "Thrown rubbish will tag hiders."          ) },
        { TaggingMethods.Contact,   ("Contact",   "Body contact will tag hiders."            ) },
        { TaggingMethods.Ascension, ("Ascension", "Saint's ascension power will tag hiders." ) }
    };
    
    
    public HideAndSeekSettingsTab(
        ArenaOnlineLobbyMenu menu,
        ArenaOnlineGameMode arenaOnline,
        HideAndSeekMode hideAndSeek) : base(menu, menu.arenaMainLobbyPage.tabContainer)
    {
        ArenaOnline = arenaOnline;
        HideAndSeek = hideAndSeek;
        ArenaOnlineMenu = menu;
        
        // TODO: Improve UI logic.
        float tabSizeX = 450f;
        
        Vector2 rowRootPos = new(42f, 417f);   // Starting position for most settings.
        float rowOffsetY = 40f;                // Offset between most settings. 
        float resourceSelectorSizeX = 100f;    // Used by every resource selector.
        float labelAlignmentYFix = -5f;        // ProperlyAlignedMenuLabel isn't properly aligned. (Text is always put to the bottom left)
        Vector2 labelSize = new(122f, 25f);    // Used by most settings labels.
        
        
        IsWillingToSeekLabel = new ProperlyAlignedMenuLabel(
            ArenaOnlineMenu,
            this,
            "Willing to Seek:",
            new Vector2(288f, 417f),
            new Vector2(100f, labelSize.y),
            false
        );
        
        IsWillingToSeekCheckBox = new OpCheckBox(
            new Configurable<bool>(HideAndSeek.MyClientData.IsWillingToSeek),
            new Vector2(IsWillingToSeekLabel.pos.x + IsWillingToSeekLabel.size.x, IsWillingToSeekLabel.pos.y + labelAlignmentYFix)
        );
        
        IsWillingToSeekCheckBox.OnValueChanged += (_, _, _) =>
        {
            HideAndSeek.MyClientData.IsWillingToSeek = IsWillingToSeekCheckBox.GetValueBool();
        };
        
        
        SeekerCountLabel = new ProperlyAlignedMenuLabel(
            ArenaOnlineMenu,
            this,
            "Number of Seekers:",
            rowRootPos,
            labelSize,
            false
        );
        
        SeekerCountUpdown = new OpUpdown(
            ConfigurableHelper.Clone(Plugin.Options.CfgSeekerCount),
            new Vector2(SeekerCountLabel.pos.x + labelSize.x, SeekerCountLabel.pos.y + labelAlignmentYFix - 4f), // OpUpdown is taller than most UI elements. Offset y further to center it with the label.
            50f
        ) { description = $"How many seekers can be selected. (Ignored if the {nameof(SeekerSelection)} is {nameof(SeekerSelection.Host)})" };
        
        SeekerCountUpdown.OnValueChanged += (_, _, _) =>
        {
            if (!OnlineManager.lobby!.isOwner) return;
            
            Assert(SeekerCountUpdown.accept == OpTextBox.Accept.Int);
            HideAndSeek.LobbyData.SeekerCount = SeekerCountUpdown.GetValueInt();
        };
        
        
        SeekerSelectionLabel = new ProperlyAlignedMenuLabel(
            ArenaOnlineMenu,
            this,
            "Seeker Selection:",
            rowRootPos - new Vector2(0, rowOffsetY * 1),
            labelSize,
            false
        );
        
        SeekerSelectionSelector = new OpResourceSelector(
            ConfigurableHelper.Clone(Plugin.Options.CfgEnabledSeekerSelection),
            new Vector2(SeekerSelectionLabel.pos.x + labelSize.x, SeekerSelectionLabel.pos.y + labelAlignmentYFix),
            resourceSelectorSizeX
        ) { description = "How seekers are selected" };
        
        SeekerSelectionSelector.OnValueChanged += (_, valueAsString, _) =>
        {
            if (!OnlineManager.lobby!.isOwner) return;
            
            Assert(Enum.TryParse(valueAsString, out SeekerSelection value));
            HideAndSeek.LobbyData.EnabledSeekerSelection = value;
        };
        
        
        TaggingMethodsLabel = new ProperlyAlignedMenuLabel(
            ArenaOnlineMenu,
            this,
            "Tagging Methods:",
            rowRootPos - new Vector2(0, rowOffsetY * 2),
            labelSize,
            false
        );
        
        {
            Vector2 checkBoxRootPos = new(TaggingMethodsLabel.pos.x + labelSize.x, TaggingMethodsLabel.pos.y + labelAlignmentYFix);
            
            int i = 0;
            foreach (var data in DataByTaggingMethod)
            {
                TaggingMethods taggingMethod = data.Key;
                (string friendlyName, string description) = data.Value;
                Assert(EnumHelper.HasExactlyOneFlag(taggingMethod));
                
                OpCheckBox checkBox = new(
                    new Configurable<bool>(
                        Plugin.Options.EnabledTaggingMethods.HasFlag(taggingMethod)
                    ),
                    checkBoxRootPos + new Vector2(85f * i, 0f)
                ) { description = description };
                
                checkBox.OnValueChanged += (_, _, _) =>
                {
                    if (!OnlineManager.lobby!.isOwner) return;
                    
                    bool value = checkBox.GetValueBool();
                    
                    TaggingMethods currentValue = HideAndSeek.LobbyData.EnabledTaggingMethods;
                    TaggingMethods newValue = value
                        ? currentValue | taggingMethod
                        : currentValue & ~taggingMethod;
                    
                    if (newValue == currentValue) return;
                    if (newValue == TaggingMethods.None)
                    {
                        checkBox.SetValueBool(true);
                        return;
                    }
                    
                    HideAndSeek.LobbyData.EnabledTaggingMethods = newValue;
                };
                
                ProperlyAlignedMenuLabel label = new(
                    ArenaOnlineMenu,
                    this,
                    friendlyName,
                    checkBox.pos + new Vector2(checkBox.size.x + 3f, -labelAlignmentYFix),
                    labelSize,
                    false
                );
                
                
                UIByTaggingMethod[taggingMethod] = (label, checkBox);
                i++;
            }
        }
        
        TagResultLabel = new ProperlyAlignedMenuLabel(
            ArenaOnlineMenu,
            this,
            "Tag Result:",
            rowRootPos - new Vector2(0, rowOffsetY * 3),
            labelSize,
            false
        );
        
        TagResultSelector = new OpResourceSelector(
            ConfigurableHelper.Clone(Plugin.Options.CfgEnabledTagResult),
            new Vector2(TagResultLabel.pos.x + labelSize.x, TagResultLabel.pos.y + labelAlignmentYFix),
            resourceSelectorSizeX
        ) { description = "What happens after hiders are tagged" };
        
        TagResultSelector.OnValueChanged += (_, valueAsString, _) =>
        {
            if (!OnlineManager.lobby!.isOwner) return;
            
            Assert(Enum.TryParse(valueAsString, out TagResult value));
            HideAndSeek.LobbyData.EnabledTagResult = value;
        };
        
        
        float scoreUpdownSizeX         = 50f;
        float scoreColumnOffsetX       = 120f;                                                     // How far from one column to another.
        float scoreColumnInitialOffset = (tabSizeX - scoreColumnOffsetX * 2)/2 - scoreUpdownSizeX; // How far from the left most edge.
        
        SeekerWinScoreUpdown = new OpUpdown(
            ConfigurableHelper.Clone(Plugin.Options.CfgSeekerWinScore),
            new Vector2(scoreUpdownSizeX/2 + scoreColumnInitialOffset, 220f),
            scoreUpdownSizeX
        );
        SeekerWinScoreUpdown.OnValueChanged += (_, _, _) =>
        {
            if (!OnlineManager.lobby!.isOwner) return;
            
            Assert(SeekerWinScoreUpdown.accept == OpTextBox.Accept.Int);
            HideAndSeek.LobbyData.SeekerWinScore = SeekerWinScoreUpdown.GetValueInt();
        };
        
        SeekerWinScoreLabel = new MenuLabel(
            ArenaOnlineMenu,
            this,
            "Seeker Win Score",
            SeekerWinScoreUpdown.pos + new Vector2(-(scoreColumnOffsetX - scoreUpdownSizeX)/2, 30f),
            new Vector2(scoreColumnOffsetX, labelSize.y),
            false
        );
        
        SeekerTagScoreUpdown = new OpUpdown(
            ConfigurableHelper.Clone(Plugin.Options.CfgSeekerTagScore),
            SeekerWinScoreUpdown.pos + new Vector2(scoreColumnOffsetX, 0f),
            scoreUpdownSizeX
        );
        SeekerTagScoreUpdown.OnValueChanged += (_, _, _) =>
        {
            if (!OnlineManager.lobby!.isOwner) return;
            
            Assert(SeekerTagScoreUpdown.accept == OpTextBox.Accept.Int);
            HideAndSeek.LobbyData.SeekerTagScore = SeekerTagScoreUpdown.GetValueInt();
        };
        
        SeekerTagScoreLabel = new MenuLabel(
            ArenaOnlineMenu,
            this,
            "Seeker Tag Score",
            SeekerTagScoreUpdown.pos + new Vector2(-(scoreColumnOffsetX - scoreUpdownSizeX)/2, 30f),
            new Vector2(120f, labelSize.y),
            false
        );
        
        HiderWinScoreUpdown = new OpUpdown(
            ConfigurableHelper.Clone(Plugin.Options.CfgHiderWinScore),
            SeekerTagScoreUpdown.pos + new Vector2(scoreColumnOffsetX, 0f),
            scoreUpdownSizeX
        );
        HiderWinScoreUpdown.OnValueChanged += (_, _, _) =>
        {
            if (!OnlineManager.lobby!.isOwner) return;
            
            Assert(HiderWinScoreUpdown.accept == OpTextBox.Accept.Int);
            HideAndSeek.LobbyData.HiderWinScore = HiderWinScoreUpdown.GetValueInt();
        };
        
        HiderWinScoreLabel = new MenuLabel(
            ArenaOnlineMenu,
            this,
            "Hider Win Score",
            HiderWinScoreUpdown.pos + new Vector2(-(scoreColumnOffsetX - scoreUpdownSizeX)/2, 30f),
            new Vector2(scoreColumnOffsetX, labelSize.y),
            false
        );
        
        
        SeekerBodyColorPicker = new OpColorPicker(
            ConfigurableHelper.Clone(Plugin.Options.CfgSeekerBodyColor),
            new Vector2(0f, 12f)
        );
        SeekerBodyColorPicker.OnValueChanged += (_, valueAsString, _) =>
        {
            if (!OnlineManager.lobby!.isOwner) return;
            
            HideAndSeek.LobbyData.SeekerBodyColor = Custom.hexToColor(valueAsString);
        };
        
        SeekerBodyColorLabel = new MenuLabel(
            ArenaOnlineMenu,
            this,
            "Seeker Body Color",
            SeekerBodyColorPicker.pos + new Vector2(0f, 150f),
            new Vector2(150f, labelSize.y),
            false
        );
        
        
        SeekerEyeColorPicker = new OpColorPicker(
            ConfigurableHelper.Clone(Plugin.Options.CfgSeekerEyeColor),
            SeekerBodyColorPicker.pos + new Vector2(SeekerBodyColorPicker.size.x, 0)
        );
        SeekerEyeColorPicker.OnValueChanged += (_, valueAsString, _) =>
        {
            if (!OnlineManager.lobby!.isOwner) return;
            
            HideAndSeek.LobbyData.SeekerEyeColor = Custom.hexToColor(valueAsString);
        };
        
        SeekerEyeColorLabel = new MenuLabel(
            ArenaOnlineMenu,
            this,
            "Seeker Eye Color",
            SeekerEyeColorPicker.pos + new Vector2(0f, 150f),
            new Vector2(150f, labelSize.y),
            false
        );
        
        
        SeekerTertiaryColorPicker = new OpColorPicker(
            ConfigurableHelper.Clone(Plugin.Options.CfgSeekerTertiaryColor),
            SeekerEyeColorPicker.pos + new Vector2(SeekerEyeColorPicker.size.x, 0)
        );
        SeekerTertiaryColorPicker.OnValueChanged += (_, valueAsString, _) =>
        {
            if (!OnlineManager.lobby!.isOwner) return;
            
            HideAndSeek.LobbyData.SeekerTertiaryColor = Custom.hexToColor(valueAsString);
        };
        
        SeekerTertiaryColorLabel = new MenuLabel(
            ArenaOnlineMenu,
            this,
            "Seeker Tertiary Color",
            SeekerTertiaryColorPicker.pos + new Vector2(0f, 150f),
            new Vector2(150f, labelSize.y),
            false
        );
        
        
        
        MenuObject[] taggingMethodMenuObjects = UIByTaggingMethod.Select(tuple => tuple.Value.label).ToArray<MenuObject>();
        UIfocusable[] taggingMethodUIFocusables = UIByTaggingMethod.Select(tuple => tuple.Value.checkBox).ToArray<UIfocusable>();
        
        // Add new UIFocusables here
        LobbyUIFocusables =
        [
            SeekerCountUpdown,
            SeekerSelectionSelector,
            ..taggingMethodUIFocusables,
            TagResultSelector,
            SeekerWinScoreUpdown,
            SeekerTagScoreUpdown,
            HiderWinScoreUpdown,
            SeekerBodyColorPicker,
            SeekerEyeColorPicker,
            SeekerTertiaryColorPicker
        ];
        ClientUIFocusables = [ IsWillingToSeekCheckBox ];
        
        AllUIFocusables =
        [
            ..LobbyUIFocusables,
            ..ClientUIFocusables
        ];
        
        Assert(
            AllUIFocusables.Length == AllUIFocusables.Distinct().Count(),
            $"""
            No UI focusable may be added multiple times.
                initial array:  [ {string.Join(", ", AllUIFocusables.AsEnumerable())} ]
                distinct array: [ {string.Join(", ", AllUIFocusables.Distinct())} ]
            """
        );
        
        AddUIElements(AllUIFocusables.ToArray<UIelement>());
        
        // Add new MenuObjects here
        AddObjects([
            myTabWrapper,
            IsWillingToSeekLabel,
            SeekerCountLabel,
            SeekerSelectionLabel,
            ..taggingMethodMenuObjects,
            TaggingMethodsLabel,
            TagResultLabel,
            SeekerWinScoreLabel,
            SeekerTagScoreLabel,
            HiderWinScoreLabel,
            SeekerBodyColorLabel,
            SeekerEyeColorLabel,
            SeekerTertiaryColorLabel
        ]);
        
        return;
        
        void AddUIElements(params UIelement[] uiElements)
        {
            foreach (UIelement uiElement in uiElements)
            {
                if (uiElement is UIfocusable uiFocusable)
                {
                    UIfocusable[][] uiFocusableArrays = [ LobbyUIFocusables, ClientUIFocusables ];
                    Assert(uiFocusableArrays.Count(uiFocusables => uiFocusables.Contains(uiFocusable)) == 1, $"Only one array should contain {uiFocusable}.");
                }
                
                _ = new PatchedUIelementWrapper(
                    myTabWrapper,
                    uiElement
                );
            }
        }
    }
    
    public override void Update()
    {
        foreach (UIfocusable uiFocusable in LobbyUIFocusables)
            uiFocusable.greyedOut = ArenaOnlineMenu.SettingsDisabled;
        
        foreach (UIfocusable uiFocusable in ClientUIFocusables)
            uiFocusable.greyedOut = ArenaOnline.initiateLobbyCountdown;
        
        
        if (HideAndSeek.LobbyData.EnabledSeekerSelection != SeekerSelection.Random)
        {
            IsWillingToSeekCheckBox.greyedOut = true;
            SeekerCountUpdown.greyedOut = true;
        }
        
        TagResultSelector.greyedOut = true; // Not implemented yet.
        
        base.Update();
        
        if (!SeekerCountUpdown.held)
            SeekerCountUpdown.SetValueInt(HideAndSeek.LobbyData.SeekerCount);
        SeekerSelectionSelector.value = HideAndSeek.LobbyData.EnabledSeekerSelection.ToString();
        TagResultSelector.value = HideAndSeek.LobbyData.EnabledTagResult.ToString();
        
        if (!SeekerWinScoreUpdown.held)
            SeekerWinScoreUpdown.SetValueInt(HideAndSeek.LobbyData.SeekerWinScore);
        if (!SeekerTagScoreUpdown.held)
            SeekerTagScoreUpdown.SetValueInt(HideAndSeek.LobbyData.SeekerTagScore);
        if (!HiderWinScoreUpdown.held)
            HiderWinScoreUpdown.SetValueInt(HideAndSeek.LobbyData.HiderWinScore);
        
        foreach (var kvp in UIByTaggingMethod)
        {
            TaggingMethods taggingMethod = kvp.Key;
            (_, OpCheckBox? opCheckBox) = kvp.Value;
            
            opCheckBox.SetValueBool(HideAndSeek.LobbyData.EnabledTaggingMethods.HasFlag(taggingMethod));
        }
    }
}