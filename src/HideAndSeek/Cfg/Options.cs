using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using UnityEngine.Assertions;
using EnumHelper = OneLetterShor.HideAndSeek.Utils.EnumHelper;

namespace OneLetterShor.HideAndSeek.Cfg;

public sealed class Options : OptionInterface
{
    public static Options Instance { get; internal set; } = null!;
    
    /// <remarks>Primarily for assertions.</remarks>
    public static bool IsInitialized { get; private set; } = false;
    
    
    
    // --- UI Data --- //
    
    /// <remarks>
    /// Useful for looping over all supported <see cref="LogLevel"/> flags.
    /// Configurables are initialized in the constructor.
    /// </remarks>
    // TODO: Consider making this a dictionary. Matches DataByTaggingTypeFlag.
    public (LogLevel, Color, Configurable<bool>)[] LogLevelFlagData { get; } =
    [
        (LogLevel.Debug,   new Color(0.14f, 0.48f, 0.70f), null!),
        (LogLevel.Info,    new Color(0.14f, 0.66f, 0.37f), null!),
        (LogLevel.Message, new Color(0.76f, 0.76f, 0.76f), null!),
        (LogLevel.Warning, new Color(0.74f, 0.61f, 0.00f), null!),
        (LogLevel.Error,   new Color(0.70f, 0.10f, 0.10f), null!),
        (LogLevel.Fatal,   new Color(0.41f, 0.14f, 0.63f), null!)
    ];
    
    /// <summary>Represents the data and UI positions of all dev toggles.</summary>
    /// <remarks>See <see cref="CreateLogLevelUI"/> for more information on grid positioning.</remarks>
    private readonly (Configurable<bool>, string text)?[,] _devToggleUIData = new (Configurable<bool>, string text)?[2,13];
    
    
    
    // --- UI Elements --- //
    public OpTab? GeneralTab { get; private set; }
    public OpTab? DevTab { get; private set; }

    
    /// <remarks>Use this over <see cref="LogLevelFlagByCheckBox"/> for looping over all check boxes.</remarks>
    public Dictionary<LogLevel, OpCheckBox> CheckBoxByLogLevelFlag { get; private set; } = new();
    /// <remarks>Use <see cref="CheckBoxByLogLevelFlag"/> over this for looping over all check boxes.</remarks>
    public Dictionary<OpCheckBox, LogLevel> LogLevelFlagByCheckBox { get; private set; } = new();
    
    public OpLabel? DevTabTitleLabel { get; private set; }
    public OpLabelLong? DevTabInfoLabel { get; private set; }
    public List<(OpLabelLong, OpCheckBox)> DevToggleElements { get; private set; } = [];
    
    
    
    // --- Configurable Accessors --- //
    /// <inheritdoc cref="CfgEnabledLogLevels"/>
    public static LogLevel EnabledLogLevels
    {
        get => Instance.CfgEnabledLogLevels.Value;
        set => Instance.CfgEnabledLogLevels.Value = value;
    }
    
    /// <inheritdoc cref="CfgIsWillingToSeek"/>
    public static bool IsWillingToSeek
    {
        get => Instance.CfgIsWillingToSeek.Value;
        set => Instance.CfgIsWillingToSeek.Value = value;
    }
    
    /// <inheritdoc cref="CfgHideDurationSeconds"/>
    public static int HideDurationSeconds
    {
        get => Instance.CfgHideDurationSeconds.Value;
        set => Instance.CfgHideDurationSeconds.Value = value;
    }
    /// <inheritdoc cref="CfgRoundDurationSeconds"/>
    public static int RoundDurationSeconds
    {
        get => Instance.CfgRoundDurationSeconds.Value;
        set => Instance.CfgRoundDurationSeconds.Value = value;
    }
    /// <inheritdoc cref="CfgSeekerCount"/>
    public static int SeekerCount
    {
        get => Instance.CfgSeekerCount.Value;
        set => Instance.CfgSeekerCount.Value = value;
    }
    /// <inheritdoc cref="CfgEnabledSeekerSelectionType"/>
    public static SeekerSelectionType EnabledSeekerSelectionType
    {
        get => Instance.CfgEnabledSeekerSelectionType.Value;
        set => Instance.CfgEnabledSeekerSelectionType.Value = value;
    }
    /// <inheritdoc cref="CfgEnabledTaggingTypes"/>
    public static TaggingTypes EnabledTaggingTypes
    {
        get => Instance.CfgEnabledTaggingTypes.Value;
        set => Instance.CfgEnabledTaggingTypes.Value = value;
    }
    /// <inheritdoc cref="CfgEnabledTagResultType"/>
    public static TagResultType EnabledTagResultType
    {
        get => Instance.CfgEnabledTagResultType.Value;
        set => Instance.CfgEnabledTagResultType.Value = value;
    }
    
    
    // --- Configurables --- //
    /// <summary>For <see cref="Logging.Logger"/> to determine if a message should be logged.</summary>
    public Configurable<LogLevel> CfgEnabledLogLevels { get; }
    
    
    // TODO: Add documentation.
    public Configurable<bool> CfgIsWillingToSeek { get; }
    
    
    // TODO: Add documentation.
    public Configurable<int> CfgHideDurationSeconds { get; }
    
    // TODO: Add documentation.
    public Configurable<int> CfgRoundDurationSeconds { get; }
    
    // TODO: Add documentation.
    public Configurable<int> CfgSeekerCount { get; }
    
    // TODO: Add documentation.
    public Configurable<SeekerSelectionType> CfgEnabledSeekerSelectionType { get; }
    
    // TODO: Add documentation.
    public Configurable<TaggingTypes> CfgEnabledTaggingTypes { get; }
    
    // TODO: Add documentation.
    public Configurable<TagResultType> CfgEnabledTagResultType { get; }
    
    
    
    // --- Initialization --- //
    
    internal Options()
    {
        Assert(!IsInitialized);
        Instance = this;
        
        OnConfigChanged += OnConfigChanged_;
        OnUnload += OnUnload_;
        
        for (int i = 0; i < LogLevelFlagData.Length; i++)
        {
            (LogLevel logLevelFlag, _, _) = LogLevelFlagData[i];
            Assert(EnumHelper.HasExactlyOneFlag(logLevelFlag));
            Configurable<bool> configurable = BindCfg(logLevelFlag.ToString(), true);
            
            LogLevelFlagData[i].Item3 = configurable;
        }
        
        CfgEnabledLogLevels = BindCfg(nameof(EnabledLogLevels), LogLevel.All);
        CfgEnabledLogLevels.OnChange += () =>
        {
            Logger.ForceLog(LogLevel.Message, $"Enabled log levels: {EnabledLogLevels}");
            UpdateLogLevelCheckBoxes();
        };
        
        
        CfgIsWillingToSeek = BindCfg(nameof(IsWillingToSeek), true);
        
        CfgHideDurationSeconds = BindCfg(nameof(HideDurationSeconds), 60);
        CfgRoundDurationSeconds = BindCfg(nameof(RoundDurationSeconds), 60 * 8);
        CfgSeekerCount = BindCfg(nameof(SeekerCount), 1);
        CfgEnabledSeekerSelectionType = BindCfg(nameof(EnabledSeekerSelectionType), SeekerSelectionType.Random);
        CfgEnabledTaggingTypes = BindCfg(nameof(EnabledTaggingTypes), TaggingTypes.Ascension, new ConfigAcceptableRange<TaggingTypes>(TaggingTypes.None + 1, TaggingTypes.All));
        CfgEnabledTagResultType = BindCfg(nameof(EnabledTagResultType), TagResultType.Ghost);
        
        IsInitialized = true;
    }
    
    public override void Initialize()
    {
        GeneralTab = new OpTab(this, "General");
        DevTab = new OpTab(this, "Developer");
        
        Tabs = [ GeneralTab, DevTab ];
        
        CreateLogLevelUI();
        CreateDevToggleUI();
    }
    
    
    
    // --- Logic --- //
    
    /// <summary>
    /// Updates all <see cref="OpCheckBox"/>es in <see cref="CheckBoxByLogLevelFlag"/>
    /// that have different values than <see cref="CfgEnabledLogLevels"/>
    /// </summary>
    private void UpdateLogLevelCheckBoxes()
    {
        foreach (var kvp in CheckBoxByLogLevelFlag)
        {
            LogLevel logLevelFlag = kvp.Key;
            OpCheckBox checkBox = kvp.Value;
            bool hasFlag = CfgEnabledLogLevels.Value.HasFlag(logLevelFlag);
            
            if (checkBox.GetValueBool() != hasFlag)
                checkBox.value = hasFlag.ToString().ToLower();
        }
    }
    
    private void UpdateEnabledLogLevels()
    {
        LogLevel newLogLevels = LogLevel.None;
        
        foreach ((LogLevel logLevelFlag, _, Configurable<bool> configurable) in LogLevelFlagData)
        {
            if (configurable.Value)
                newLogLevels |= logLevelFlag;
        }
        
        // Only update the value if it changed.
        if (newLogLevels != CfgEnabledLogLevels.Value)
            CfgEnabledLogLevels.Value = newLogLevels;
    }
    
    
    // --- UI Creation --- //
    
    /// <summary>
    /// Creates the <see cref="OpLabel"/>s and <see cref="OpCheckBox"/>es
    /// for dev toggle <see cref="Configurable{T}"/>s.
    /// </summary>
    /// <remarks>Places UI elements in a grid based on <see cref="_devToggleUIData"/>.</remarks>
    /// <exception cref="AssertionException">
    /// Thrown if <see cref="DevTab"/> is <see langword="null"/> or if the column count
    /// is an unexpected value (<see cref="_devToggleUIData"/>'s first dimension length).
    /// </exception>
    [MemberNotNull(nameof(DevTabTitleLabel), nameof(DevTabInfoLabel))]
    private void CreateDevToggleUI()
    {
        Vector2Int gridCellCounts = new(_devToggleUIData.GetLength(0), _devToggleUIData.GetLength(1));
        float[] columnPosXs = [ 30f, 310f ];
        Assert(DevTab is not null);
        Assert(columnPosXs.Length == gridCellCounts.x);
        
        const float cellSpacingY = 10f;
        Vector2 gridPosOffset = new(0f, 0f); // Used to offset the entire grid based on the lengths of dev toggle labels (to look centered).
        
        DevTabTitleLabel = new OpLabel(new Vector2(75f, 540f), new Vector2(450f, 30f), "Developer Settings", bigText: true);
        DevTabInfoLabel = new OpLabelLong(new Vector2(75f, 507f), new Vector2(450f, 30f), "It is recommended to not change settings unless you know what you are doing.");
        
        for (int i = 0; i < gridCellCounts.x; i++)
        {
            for (int j = 0; j < gridCellCounts.y; j++)
            {
                if (_devToggleUIData[i, j] is not (Configurable<bool> configurable, string text)) continue;
                
                Vector2Int gridCoord = new(i, j);
                
                float cellSizeY = 25f + cellSpacingY;
                Vector2 anchorPos = new Vector2(columnPosXs[gridCoord.x], cellSizeY * gridCoord.y + 40f) + gridPosOffset;
                
                OpLabelLong label = new(anchorPos - new Vector2(0f, cellSpacingY / 2f), new Vector2(210f, cellSizeY), text, alignment: FLabelAlignment.Right)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                };
                OpCheckBox checkBox = new(configurable, anchorPos + new Vector2(label.size.x + 7f, 0f));
                
                DevToggleElements.Add((label, checkBox));
                
                DevTab.AddItems(
                    label, 
                    checkBox
                );
            }
        }
        
        DevTab.AddItems(
            DevTabTitleLabel,
            DevTabInfoLabel
        );
    }
    
    /// <summary>
    /// Creates the <see cref="OpLabel"/>s and <see cref="OpCheckBox"/>es
    /// for <see cref="LogLevel"/> toggles.
    /// </summary>
    /// <exception cref="AssertionException">Thrown if <see cref="DevTab"/> is <see langword="null"/>.</exception>
    private void CreateLogLevelUI()
    {
        Assert(DevTab is not null);
        
        Vector2 rootPos = new(480f, 25f);
        Vector2 rootPosOffset = new(0f, 32f);
        
        for (int i = 0; i < LogLevelFlagData.Length; i++)
        {
            (LogLevel logLevelFlag, Color color, Configurable<bool> configurable) = LogLevelFlagData[i];
            
            Vector2 pos = rootPos + (rootPosOffset * i);
            Vector2 labelSize = new(55f, 24f);
            Vector2 checkBoxOffset = new(labelSize.x + 10f, 0f);
            
            OpLabel label = new(pos, labelSize, $"{logLevelFlag}:", FLabelAlignment.Right)
            {
                color = color
            };
            
            /* Configurable MUST not be cosmetic (null key), otherwise OnValueChanged
               will be invoked with the default value when the remix menu closes. */
            OpCheckBox checkBox = new(configurable, pos + checkBoxOffset)
            {
                colorEdge = color,
                colorFill = Color.Lerp(color, Color.black, 0.83f)
            };
            
            CheckBoxByLogLevelFlag[logLevelFlag] = checkBox;
            LogLevelFlagByCheckBox[checkBox] = logLevelFlag;
            
            DevTab.AddItems(label, checkBox);
        }
    }
    
    
    
    // --- UI Events --- //
    
    
    
    // --- OI Events --- //
    
    private void OnConfigChanged_()
    {
        UpdateEnabledLogLevels();
    }
    
    /// <summary>
    /// Handles cleaning up ui & ui data such as unsubscribing
    /// ui elements and setting references to <see langword="null"/>. 
    /// </summary>
    private void OnUnload_()
    {
        GeneralTab = null;
        DevTab = null;
        DevTabTitleLabel = null;
        DevTabInfoLabel = null;
        
        CheckBoxByLogLevelFlag = new();
        LogLevelFlagByCheckBox = new();
        
        DevToggleElements = [];
    }
    
    
    
    // --- Overrides --- //
    
    
    
    // --- Helpers --- //
    
    /// <summary>Wrapper for binding <see cref="Configurable{T}"/>s.</summary>
    private Configurable<T> BindCfg<T>(string? key, T defaultValue, ConfigurableInfo? configInfo = null)
    {
        return config.Bind(key, defaultValue, configInfo);
    }
    
    /// <summary>Wrapper for binding <see cref="Configurable{T}"/>s.</summary>
    private Configurable<T> BindCfg<T>(string? key, T defaultValue, ConfigAcceptableBase accept)
    {
        return BindCfg(key, defaultValue, new ConfigurableInfo("", accept));
    }
    
    /// <summary>
    /// Wrapper for binding <see cref="Configurable{T}"/>s intended for dev toggles.
    /// </summary>
    /// <exception cref="AssertionException">
    /// Thrown if <paramref name="gridCoord"/> is out
    /// of range of <see cref="_devToggleUIData"/>.
    /// </exception>
    private Configurable<bool> BindDevToggleCfg(
        string key,
        bool defaultValue,
        Vector2Int gridCoord,
        string text,
        ConfigurableInfo? configInfo = null)
    {
        Assert(gridCoord is { x: > -1, y: > -1 });
        Assert(gridCoord.x < _devToggleUIData.GetLength(0));
        Assert(gridCoord.y < _devToggleUIData.GetLength(1));
        
        Configurable<bool> configurable = BindCfg(key, defaultValue, configInfo);
        _devToggleUIData[gridCoord.x, gridCoord.y] = (configurable, text);
        
        return configurable;
    }
    
    private void DebugPlaceAt(UIelement uiElement) => DebugPlaceAt(uiElement.pos, uiElement.size);
    
    private void DebugPlaceAt(Vector2 pos, Vector2 size)
    {
        Assert(DevTab is not null);
        
        DevTab.AddItems(
            new OpRect(pos, size, 0.1f)
        );
    }
}