using Menu;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using RainMeadow.UI.Components.Patched;
using EnumHelper = OneLetterShor.HideAndSeek.Utils.EnumHelper;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed class HideAndSeekSettingsTab : TabContainer.Tab
{
    private ArenaOnlineGameMode ArenaOnline { get; }
    private HideAndSeekMode HideAndSeekMode { get; }
    public ArenaOnlineLobbyMenu ArenaOnlineMenu { get; }
    
    public ProperlyAlignedMenuLabel WillingToSeekLabel { get; }
    public OpCheckBox               WillingToSeekCheckBox { get; }
    public ProperlyAlignedMenuLabel SeekerCountLabel { get; }
    public OpUpdown                 SeekerCountUpdown { get; }
    public ProperlyAlignedMenuLabel SeekerSelectionLabel { get; }
    public OpResourceSelector       SeekerSelectionSelector { get; }
    public ProperlyAlignedMenuLabel TaggingMethodsLabel { get; }
    public Dictionary<TaggingMethods, (ProperlyAlignedMenuLabel, OpCheckBox)> UIByTaggingMethod { get; } = [];
    public ProperlyAlignedMenuLabel TagResultLabel { get; }
    public OpResourceSelector       TagResultSelector { get; }
    
    public UIfocusable[] AllUIFocusables { get; }
    public UIfocusable[] LobbyUIFocusables { get; }
    public UIfocusable[] ClientUIFocusables { get; }
    public UIfocusable[] DisabledUIFocusables { get; }
    
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
        HideAndSeekMode = hideAndSeek;
        ArenaOnlineMenu = menu;
        
        // TODO: Improve UI logic.
        Vector2 rowRootPos = new(42f, 417f);      // Starting position for most settings.
        const float rowOffsetY = 40f;             // Offset between most settings. 
        const float resourceSelectorSizeX = 130f; // Used by every resource selector.
        const float labelAlignmentYFix = -5f;     // ProperlyAlignedMenuLabel isn't properly aligned. (Text is always put to the bottom left)
        Vector2 labelSize = new(122f, 25f);       // Used by most settings labels.
        
        
        WillingToSeekLabel = new ProperlyAlignedMenuLabel(
            ArenaOnlineMenu,
            this,
            "Willing to Seek:",
            new Vector2(288f, 35f),
            new Vector2(80f, labelSize.y),
            false
        );
        
        WillingToSeekCheckBox = new OpCheckBox(
            ConfigurableHelper.Clone(Plugin.Options.CfgIsWillingToSeek),
            new Vector2(WillingToSeekLabel.pos.x + labelSize.x, WillingToSeekLabel.pos.y + labelAlignmentYFix)
        );
        
        WillingToSeekCheckBox.OnValueChanged += (_, _, _) =>
        {
            bool value = WillingToSeekCheckBox.GetValueBool();
            Plugin.Options.IsWillingToSeek = value;
            HideAndSeekClientData.GetMyData().IsWillingToSeek = value;
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
        ) { description = "How many seekers can be selected. (Ignored if the selection type is set to host choice)" };
        
        SeekerCountUpdown.OnValueChanged += (_, _, _) =>
        {
            Assert(SeekerCountUpdown.accept == OpTextBox.Accept.Int);
            HideAndSeekMode.SeekerCount = SeekerCountUpdown.GetValueInt();
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
            Assert(Enum.TryParse(valueAsString, out SeekerSelection value));
            HideAndSeekMode.EnabledSeekerSelection = value;
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
                    bool value = checkBox.GetValueBool();
                    
                    TaggingMethods currentValue = HideAndSeekMode.EnabledTaggingMethods;
                    TaggingMethods newValue = value
                        ? currentValue | taggingMethod 
                        : currentValue & ~taggingMethod;
                    
                    if (newValue == currentValue) return;
                    if (newValue == TaggingMethods.None)
                    {
                        checkBox.SetValueBool(true);
                        return;
                    }
                    
                    HideAndSeekMode.EnabledTaggingMethods = newValue;
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
            Assert(Enum.TryParse(valueAsString, out TagResult value));
            HideAndSeekMode.EnabledTagResult = value;
        };
        
        
        MenuObject[] taggingMethodMenuObjects = UIByTaggingMethod.Select(tuple => tuple.Value.Item1).ToArray<MenuObject>();
        UIfocusable[] taggingMethodUIFocusables = UIByTaggingMethod.Select(tuple => tuple.Value.Item2).ToArray<UIfocusable>();
        
        // Add new UIFocusables here
        // LobbyUIFocusables = [ SeekerCountUpdown, SeekerSelectionSelector, ..taggingMethodUIFocusables, TagResultSelector ];
        // ClientUIFocusables = [ WillingToSeekCheckBox ];
        // DisabledUIFocusables = [];
        
        LobbyUIFocusables = [];
        ClientUIFocusables = [ WillingToSeekCheckBox ];
        DisabledUIFocusables = [ SeekerCountUpdown, SeekerSelectionSelector, ..taggingMethodUIFocusables, TagResultSelector ];
        
        AllUIFocusables =
        [
            ..LobbyUIFocusables,
            ..ClientUIFocusables,
            ..DisabledUIFocusables
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
        this.SafeAddSubobjects([
            myTabWrapper,
            WillingToSeekLabel,
            SeekerCountLabel,
            SeekerSelectionLabel,
            ..taggingMethodMenuObjects,
            TaggingMethodsLabel,
            TagResultLabel
        ]);
        
        return;
        
        void AddUIElements(params UIelement[] uiElements)
        {
            foreach (UIelement uiElement in uiElements)
            {
                if (uiElement is UIfocusable uiFocusable)
                {
                    UIfocusable[][] uiFocusableArrays = [ LobbyUIFocusables, ClientUIFocusables, DisabledUIFocusables ];
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
        
        foreach (UIfocusable uiFocusable in DisabledUIFocusables)
            uiFocusable.greyedOut = true;
        
        base.Update();
        
        // The value is not updated when held.
        // If the update loop always assigned the value, then no user input would go through.(unless they clicked for exactly one tick)
        if (!SeekerCountUpdown.held) 
            SeekerCountUpdown.SetValueInt(HideAndSeekMode.SeekerCount);
        SeekerSelectionSelector.value = HideAndSeekMode.EnabledSeekerSelection.ToString();
        TagResultSelector.value = HideAndSeekMode.EnabledTagResult.ToString();
        
        foreach (var kvp in UIByTaggingMethod)
        {
            TaggingMethods taggingMethod = kvp.Key;
            (_, OpCheckBox? opCheckBox) = kvp.Value;
            
            opCheckBox.SetValueBool(HideAndSeekMode.EnabledTaggingMethods.HasFlag(taggingMethod));
        }
    }
    
    public override void RemoveSprites()
    {
        Assert(OnlineManager.lobby?.gameMode is ArenaOnlineGameMode);
        base.RemoveSprites();
    }
}