using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using EnumHelper = OneLetterShor.HideAndSeek.Utils.EnumHelper;

namespace OneLetterShor.HideAndSeek.Cfg;

public sealed class Options : OptionInterface
{
    /// <remarks>Primarily for assertions.</remarks>
    public bool IsInitialized { get; private set; } = false;
    
    
    
    // --- UI Data --- //
    
    /// <remarks>
    /// Useful for looping over all supported <see cref="LogLevel"/> flags.
    /// Configurables are initialized in the constructor.
    /// </remarks>
    // TODO: Consider making this a dictionary. Matches DataByTaggingMethod.
    public (LogLevel, Color, Configurable<bool>)[] LogLevelData { get; } =
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

    
    /// <remarks>Use this over <see cref="LogLevelByCheckBox"/> for looping over all check boxes.</remarks>
    public Dictionary<LogLevel, OpCheckBox> CheckBoxByLogLevelFlag { get; private set; } = [];
    /// <remarks>Use <see cref="CheckBoxByLogLevelFlag"/> over this for looping over all check boxes.</remarks>
    public Dictionary<OpCheckBox, LogLevel> LogLevelByCheckBox { get; private set; } = [];
    
    public OpLabel? DevTabTitleLabel { get; private set; }
    public OpLabelLong? DevTabInfoLabel { get; private set; }
    public List<(OpLabelLong, OpCheckBox)> DevToggleElements { get; private set; } = [];
    
    
    
    // --- Configurable Accessors --- //
    /// <inheritdoc cref="CfgEnabledLogLevels"/>
    public LogLevel EnabledLogLevels
    {
        get => CfgEnabledLogLevels.Value;
        set => CfgEnabledLogLevels.Value = value;
    }
    
    /// <inheritdoc cref="CfgIsWillingToSeek"/>
    public bool IsWillingToSeek
    {
        get => CfgIsWillingToSeek.Value;
        set => CfgIsWillingToSeek.Value = value;
    }
    
    /// <inheritdoc cref="CfgHideDurationSeconds"/>
    public int HideDurationSeconds
    {
        get => CfgHideDurationSeconds.Value;
        set => CfgHideDurationSeconds.Value = value;
    }
    /// <inheritdoc cref="CfgRoundDurationSeconds"/>
    public int RoundDurationSeconds
    {
        get => CfgRoundDurationSeconds.Value;
        set => CfgRoundDurationSeconds.Value = value;
    }
    /// <inheritdoc cref="CfgSeekerCount"/>
    public int SeekerCount
    {
        get => CfgSeekerCount.Value;
        set => CfgSeekerCount.Value = value;
    }
    /// <inheritdoc cref="CfgEnabledSeekerSelection"/>
    public SeekerSelection EnabledSeekerSelection
    {
        get => CfgEnabledSeekerSelection.Value;
        set => CfgEnabledSeekerSelection.Value = value;
    }
    /// <inheritdoc cref="CfgEnabledTaggingMethods"/>
    public TaggingMethods EnabledTaggingMethods
    {
        get => CfgEnabledTaggingMethods.Value;
        set => CfgEnabledTaggingMethods.Value = value;
    }
    /// <inheritdoc cref="CfgEnabledTagResult"/>
    public TagResult EnabledTagResult
    {
        get => CfgEnabledTagResult.Value;
        set => CfgEnabledTagResult.Value = value;
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
    public Configurable<SeekerSelection> CfgEnabledSeekerSelection { get; }
    
    // TODO: Add documentation.
    public Configurable<TaggingMethods> CfgEnabledTaggingMethods { get; }
    
    // TODO: Add documentation.
    public Configurable<TagResult> CfgEnabledTagResult { get; }
    
    
    
    // --- Initialization --- //
    
    internal Options()
    {
        Assert(!IsInitialized);
        
        OnConfigChanged += OnConfigChanged_;
        OnUnload += OnUnload_;
        
        for (int i = 0; i < LogLevelData.Length; i++)
        {
            (LogLevel logLevel, _, _) = LogLevelData[i];
            Assert(EnumHelper.HasExactlyOneFlag(logLevel));
            Configurable<bool> configurable = BindCfg(logLevel.ToString(), true);
            
            LogLevelData[i].Item3 = configurable;
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
        CfgSeekerCount = BindCfg(nameof(SeekerCount), 1, new ConfigAcceptableRange<int>(1, 99));
        CfgEnabledSeekerSelection = BindCfg(nameof(EnabledSeekerSelection), SeekerSelection.Random);
        CfgEnabledTaggingMethods = BindCfg(nameof(EnabledTaggingMethods), TaggingMethods.Ascension, new ConfigAcceptableRange<TaggingMethods>(TaggingMethods.None + 1, TaggingMethods.All));
        CfgEnabledTagResult = BindCfg(nameof(EnabledTagResult), TagResult.Ghost);
        
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
    /// Updates all <see cref="OpCheckBox"/> values in <see cref="CheckBoxByLogLevelFlag"/>.
    /// </summary>
    private void UpdateLogLevelCheckBoxes()
    {
        foreach (var kvp in CheckBoxByLogLevelFlag)
        {
            LogLevel logLevelFlag = kvp.Key;
            OpCheckBox checkBox = kvp.Value;
            
            checkBox.SetValueBool(CfgEnabledLogLevels.Value.HasFlag(logLevelFlag));
        }
    }
    
    private void UpdateEnabledLogLevels()
    {
        LogLevel newLogLevels = LogLevel.None;
        
        foreach ((LogLevel logLevelFlag, _, Configurable<bool> configurable) in LogLevelData)
        {
            if (configurable.Value)
                newLogLevels |= logLevelFlag;
        }
        
        CfgEnabledLogLevels.Value = newLogLevels;
    }
    
    
    // --- UI Creation --- //
    
    /// <summary>
    /// Creates the <see cref="OpLabel"/>s and <see cref="OpCheckBox"/>es
    /// for dev toggle <see cref="Configurable{T}"/>s.
    /// </summary>
    /// <remarks>Places UI elements in a grid based on <see cref="_devToggleUIData"/>.</remarks>
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
    private void CreateLogLevelUI()
    {
        Assert(DevTab is not null);
        
        Vector2 rootPos = new(480f, 25f);
        Vector2 rootPosOffset = new(0f, 32f);
        
        for (int i = 0; i < LogLevelData.Length; i++)
        {
            (LogLevel logLevelFlag, Color color, Configurable<bool> configurable) = LogLevelData[i];
            
            Vector2 pos = rootPos + (rootPosOffset * i);
            Vector2 labelSize = new(55f, 24f);
            Vector2 checkBoxOffset = new(labelSize.x + 10f, 0f);
            
            OpLabel label = new(pos, labelSize, $"{logLevelFlag}:", FLabelAlignment.Right)
            {
                color = color
            };
            
            // Configurable MUST not be cosmetic (null key), otherwise OnValueChanged
            // will be invoked with the default value when the remix menu closes.
            OpCheckBox checkBox = new(configurable, pos + checkBoxOffset)
            {
                colorEdge = color,
                colorFill = Color.Lerp(color, Color.black, 0.83f)
            };
            
            CheckBoxByLogLevelFlag[logLevelFlag] = checkBox;
            LogLevelByCheckBox[checkBox] = logLevelFlag;
            
            DevTab.AddItems(label, checkBox);
        }
    }
    
    
    
    // --- UI Events --- //
    
    
    
    // --- OI Events --- //
    
    private void OnConfigChanged_()
    {
        UpdateEnabledLogLevels();
    }
    
    private void OnUnload_()
    {
        GeneralTab = null;
        DevTab = null;
        DevTabTitleLabel = null;
        DevTabInfoLabel = null;
        
        CheckBoxByLogLevelFlag = [];
        LogLevelByCheckBox = [];
        
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
    /// <exception cref="ArgumentOutOfRangeException">
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
        if (   gridCoord.x < 0
            || gridCoord.x > _devToggleUIData.GetLength(0)
            || gridCoord.y < 0
            || gridCoord.y > _devToggleUIData.GetLength(1))
            throw new ArgumentOutOfRangeException(nameof(gridCoord), gridCoord, "Coordinate is out of bounds.");
        
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