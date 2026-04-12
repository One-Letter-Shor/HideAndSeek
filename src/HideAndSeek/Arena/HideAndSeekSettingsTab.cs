using Menu;
using Menu.Remix.MixedUI;
using RainMeadow;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using RainMeadow.UI.Components.Patched;
using EnumHelper = OneLetterShor.HideAndSeek.Utils.EnumHelper;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed class HideAndSeekSettingsTab : TabContainer.Tab
{
    private ArenaOnlineGameMode OnlineArena { get; }
    private HideAndSeekMode HideAndSeekMode { get; }
    public ArenaOnlineLobbyMenu ArenaOnlineMenu { get; }
    
    public ProperlyAlignedMenuLabel WillingToSeekLabel { get; }
    public OpCheckBox               WillingToSeekCheckBox { get; }
    public ProperlyAlignedMenuLabel SeekerCountLabel { get; }
    public OpUpdown                 SeekerCountUpdown { get; }
    public ProperlyAlignedMenuLabel SeekerSelectionTypeLabel { get; }
    public OpResourceSelector       SeekerSelectionTypeSelector { get; }
    public ProperlyAlignedMenuLabel TaggingTypesLabel { get; }
    public Dictionary<TaggingTypes, (ProperlyAlignedMenuLabel, OpCheckBox)> UIByTaggingTypeFlag { get; } = new();
    public ProperlyAlignedMenuLabel TagResultTypeLabel { get; }
    public OpResourceSelector       TagResultTypeSelector { get; }
    
    public List<UIfocusable> UIFocusables = [];
    
    public Dictionary<TaggingTypes, (string friendlyName, string description)> DataByTaggingTypeFlag = new()
    {
        { TaggingTypes.Rock,      ("Rock hit",  "Rubbish that can be thrown will tag hiders who are hit.") },
        { TaggingTypes.Contact,   ("Contact",   "Body contact will tag hiders."                          ) },
        { TaggingTypes.Ascension, ("Ascension", "Saint's ascension powers will tag hiders who are hit."  ) }
    };
    
    
    public HideAndSeekSettingsTab(
        ArenaOnlineLobbyMenu arenaOnlineMenu,
        HideAndSeekMode hideAndSeek,
        ArenaOnlineGameMode onlineArena) : base(arenaOnlineMenu, arenaOnlineMenu.arenaMainLobbyPage.tabContainer)
    {
        OnlineArena = onlineArena;
        HideAndSeekMode = hideAndSeek;
        ArenaOnlineMenu = arenaOnlineMenu;
        
        // TODO: Improve UI logic.
        Vector2 rowRootPos = new(42f, 417f);      // Starting position for most settings.
        const float rowOffsetY = 40f;             // Offset between most settings. 
        const float resourceSelectorSizeX = 130f; // Used by every resource selector.
        const float labelAlignmentYFix = -5f;     // ProperlyAlignedMenuLabel isn't properly aligned. (Text is put to the bottom left)
        Vector2 labelSize = new(122f, 25f);       // Used by most settings labels.
        
        
        WillingToSeekLabel = new ProperlyAlignedMenuLabel(
            ArenaOnlineMenu,
            this,
            "Willing to Seek:",
            new Vector2(288f, 35f),
            new Vector2(97f, labelSize.y),
            false
        );
        
        WillingToSeekCheckBox = new OpCheckBox(
            new Configurable<bool>(
                Cfg.Options.IsWillingToSeek
            ),
            new Vector2(WillingToSeekLabel.pos.x + labelSize.x, WillingToSeekLabel.pos.y + labelAlignmentYFix)
        );
        
        WillingToSeekCheckBox.OnValueChanged += (_, valueAsString, _) =>
        {
            Assert(OnlineManager.lobby is not null);
            
            HideAndSeekClientData.MyClientData.IsWillingToSeek = bool.Parse(valueAsString);
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
            new Configurable<int>(
                Cfg.Options.SeekerCount,
                new ConfigAcceptableRange<int>(1, 99)
            ),
            new Vector2(SeekerCountLabel.pos.x + labelSize.x, SeekerCountLabel.pos.y + labelAlignmentYFix - 4f), // OpUpdown is taller than most UI elements. Offset y further to center it with the label.
            50f
        ) { description = "How many seekers can be selected. Ignored if the selection type is not set to randomized." };
        
        SeekerCountUpdown.OnValueChanged += (_, valueAsString, _) =>
        {
            HideAndSeekMode.SeekerCount = int.Parse(valueAsString);
        };
        
        
        SeekerSelectionTypeLabel = new ProperlyAlignedMenuLabel(
            ArenaOnlineMenu,
            this,
            "Seeker Selection:",
            rowRootPos - new Vector2(0, rowOffsetY * 1),
            labelSize,
            false
        );
        
        SeekerSelectionTypeSelector = new OpResourceSelector(
            new Configurable<SeekerSelectionType>(
                Cfg.Options.EnabledSeekerSelectionType
            ),
            new Vector2(SeekerSelectionTypeLabel.pos.x + labelSize.x, SeekerSelectionTypeLabel.pos.y + labelAlignmentYFix),
            resourceSelectorSizeX
        ) { description = "How seekers are selected" };
        
        SeekerSelectionTypeSelector.OnValueChanged += (_, valueAsString, _) =>
        {
            Assert(Enum.TryParse(valueAsString, out SeekerSelectionType value));
            HideAndSeekMode.EnabledSeekerSelectionType = value;
        };
        
        
        TaggingTypesLabel = new ProperlyAlignedMenuLabel(
            ArenaOnlineMenu,
            this,
            "Tagging Methods:",
            rowRootPos - new Vector2(0, rowOffsetY * 2),
            labelSize,
            false
        );
        
        {
            Vector2 checkBoxRootPos = new(TaggingTypesLabel.pos.x + labelSize.x, TaggingTypesLabel.pos.y + labelAlignmentYFix);
            
            int i = 0;
            foreach (var data in DataByTaggingTypeFlag)
            {
                TaggingTypes taggingTypeFlag = data.Key;
                (string friendlyName, string description) = data.Value;
                Assert(EnumHelper.HasExactlyOneFlag(taggingTypeFlag));
                
                OpCheckBox checkBox = new(
                    new Configurable<bool>(
                        Cfg.Options.EnabledTaggingTypes.HasFlag(taggingTypeFlag)
                    ),
                    checkBoxRootPos + new Vector2(85f * i, 0f)
                ) { description = description };
                
                checkBox.OnValueChanged += (_, valueAsString, _) =>
                {
                    bool value = bool.Parse(valueAsString);
                    
                    TaggingTypes currentValue = HideAndSeekMode.EnabledTaggingTypes;
                    TaggingTypes newValue = value
                        ? currentValue | taggingTypeFlag 
                        : currentValue & ~taggingTypeFlag;
                    
                    if (newValue == currentValue) return;
                    if (newValue == TaggingTypes.None)
                    {
                        checkBox.value = true.ToString().ToLower();
                        return;
                    }
                    
                    HideAndSeekMode.EnabledTaggingTypes = newValue;
                };
                
                ProperlyAlignedMenuLabel label = new(
                    ArenaOnlineMenu,
                    this,
                    friendlyName,
                    checkBox.pos + new Vector2(checkBox.size.x + 3f, -labelAlignmentYFix),
                    labelSize,
                    false
                );
                
                
                UIByTaggingTypeFlag[taggingTypeFlag] = (label, checkBox);
                
                this.SafeAddSubobjects(label);
                AddUIElements(checkBox);
                i++;
            }
        }
        
        TagResultTypeLabel = new ProperlyAlignedMenuLabel(
            ArenaOnlineMenu,
            this,
            "Tag Result:",
            rowRootPos - new Vector2(0, rowOffsetY * 3),
            labelSize,
            false
        );
        
        TagResultTypeSelector = new OpResourceSelector(
            new Configurable<TagResultType>(
                Cfg.Options.EnabledTagResultType
            ),
            new Vector2(TagResultTypeLabel.pos.x + labelSize.x, TagResultTypeLabel.pos.y + labelAlignmentYFix),
            resourceSelectorSizeX
        ) { description = "What happens after hiders are tagged" };
        
        TagResultTypeSelector.OnValueChanged += (_, valueAsString, _) =>
        {
            Assert(Enum.TryParse(valueAsString, out TagResultType value));
            HideAndSeekMode.EnabledTagResultType = value;
        };
        
        
        AddUIElements(
            WillingToSeekCheckBox,
            SeekerCountUpdown,
            SeekerSelectionTypeSelector,
            TagResultTypeSelector
        );
        
        this.SafeAddSubobjects(
            myTabWrapper,
            WillingToSeekLabel,
            SeekerCountLabel,
            SeekerSelectionTypeLabel,
            TaggingTypesLabel,
            TagResultTypeLabel
        );
        
        return;
        
        void AddUIElements(params UIelement[] uiElements)
        {
            foreach (UIelement uiElement in uiElements)
            {
                if (uiElement is UIfocusable uiFocusable)
                    UIFocusables.Add(uiFocusable);
                
                _ = new PatchedUIelementWrapper(
                    myTabWrapper,
                    uiElement
                );
            }
        }
    }
    
    public override void Update()
    {
        foreach (UIfocusable uiFocusable in UIFocusables)
        {
            uiFocusable.greyedOut = ArenaOnlineMenu.SettingsDisabled;
        }
        base.Update();
    }
    
    public override void RemoveSprites()
    {
        Assert(OnlineManager.lobby?.gameMode is ArenaOnlineGameMode);
        base.RemoveSprites();
    }
}