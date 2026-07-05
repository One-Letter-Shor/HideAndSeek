using JetBrains.Annotations;
using RainMeadow;
using RainMeadow.Generics;

namespace OneLetterShor.HideAndSeek.Arena;

/// <remarks>
/// Settings are automatically saved unless
/// <see cref="CanApplySettings"/> is <see langword="false"/>.
/// </remarks>
public sealed class HideAndSeekLobbyData : OnlineResource.ResourceData
{
    public static bool CanApplySettings { get; set; } = true;
    
    public bool AreSeekerDebugToolsEnabled   { get; set => ApplySetting(value, out field, Plugin.Options.CfgAreSeekerDebugToolsEnabled); } = Plugin.Options.AreSeekerDebugToolsEnabled;
    
    public int               HideDurationSeconds      { get; set => ApplySetting(value, out field, Plugin.Options.CfgHideDurationSeconds);    } = Plugin.Options.HideDurationSeconds;
    public int               SeekDurationSeconds      { get; set => ApplySetting(value, out field, Plugin.Options.CfgSeekDurationSeconds);    } = Plugin.Options.SeekDurationSeconds;
    public int               SeekerCount              { get; set => ApplySetting(value, out field, Plugin.Options.CfgSeekerCount);            } = Plugin.Options.SeekerCount;
    public SeekerSelection   EnabledSeekerSelection   { get; set => ApplySetting(value, out field, Plugin.Options.CfgEnabledSeekerSelection); } = Plugin.Options.EnabledSeekerSelection;
    public TaggingMethods    EnabledTaggingMethods    { get; set => ApplySetting(value, out field, Plugin.Options.CfgEnabledTaggingMethods);  } = Plugin.Options.EnabledTaggingMethods;
    public TagResult         EnabledTagResult         { get; set => ApplySetting(value, out field, Plugin.Options.CfgEnabledTagResult);       } = Plugin.Options.EnabledTagResult;
    
    public Color SeekerBodyColor       { get; set => ApplySetting(value, out field, Plugin.Options.CfgSeekerBodyColor);     } = Plugin.Options.SeekerBodyColor;
    public Color SeekerEyeColor        { get; set => ApplySetting(value, out field, Plugin.Options.CfgSeekerEyeColor);      } = Plugin.Options.SeekerEyeColor;
    public Color SeekerTertiaryColor   { get; set => ApplySetting(value, out field, Plugin.Options.CfgSeekerTertiaryColor); } = Plugin.Options.SeekerTertiaryColor;
    
    // TODO: Add documentation for Seekers and InitialSeekers. The documentation should include the helper properties & methods and why this should not be directly accessed for adding and removing players.
    
    public List<OnlinePlayer> Seekers
    {
        get;
        set
        {
            if (!value.SequenceEqual(field))
                Logger.Debug($"Seekers: [ {string.Join(", ", value)} ]");
            
            field = value;
        }
    } = [];
    
    /// <summary>
    /// The <see cref="OnlinePlayer"/>s who started as seekers.
    /// </summary>
    public List<OnlinePlayer> InitialSeekers
    {
        get;
        set
        {
            if (!value.SequenceEqual(field))
                Logger.Debug($"Seekers: [ {string.Join(", ", value)} ]");
            
            field = value;
        }
    } = [];
    
    /// <summary>
    /// Clamps the value based on <paramref name="configurable"/>
    /// and writes to it if both the me player is the host and
    /// <see cref="CanApplySettings"/> is <see langword="true"/>.
    /// </summary>
    private void ApplySetting<T>(T value, out T field, Configurable<T> configurable)
    {
        Assert(OnlineManager.lobby is not null);
        
        value = configurable.ClampValue(value);
        
        if (CanApplySettings && OnlineManager.lobby.isOwner)
            configurable.Value = value;
        
        field = value;
    }
    
    public override ResourceDataState MakeState(OnlineResource resource)
    {
        AssertIs(resource, out Lobby lobby);
        
        return new State(this, lobby);
    }
    
    internal sealed class State : ResourceDataState
    {
        private const string
            _settings = nameof(_settings),
            _debug = nameof(_debug);
        
        [OnlineField(group = _debug)]
        public bool AreSeekerDebugToolsEnabled;
        
        [OnlineField(group = _settings)]
        public int HideDurationSeconds;
        
        [OnlineField(group = _settings)]
        public int SeekDurationSeconds;
        
        [OnlineField(group = _settings)]
        public int SeekerCount;
        
        [OnlineField(group = _settings)]
        public int EnabledSeekerSelection;
        
        [OnlineField(group = _settings)]
        public int EnabledTaggingMethods;
        
        [OnlineField(group = _settings)]
        public int EnabledTagResult;
        
        
        [OnlineFieldColorRgb(group = _settings)]
        public Color SeekerBodyColor;
        
        [OnlineFieldColorRgb(group = _settings)]
        public Color SeekerEyeColor;
        
        [OnlineFieldColorRgb(group = _settings)]
        public Color SeekerTertiaryColor;
        
        // TODO: Why do these lists need to have groups or be marked as nullable in order to be serializable? If only one exists it is fine, but as soon as there are two it fails.
        [OnlineField(group = nameof(Seekers))]
        public DynamicOrderedUshorts Seekers = new([]);
        
        [OnlineField(group = nameof(InitialSeekers))]
        public DynamicOrderedUshorts InitialSeekers = new([]);
        
        // Rain Meadow requires a ctor with no params.
        [UsedImplicitly]
        public State() { }
        
        internal State(HideAndSeekLobbyData data, Lobby lobby)
        {
            Assert(lobby.gameMode is ArenaOnlineGameMode);
            
            if (!HideAndSeekMode.IsHideAndSeekMode(out _)) return;
            
            AreSeekerDebugToolsEnabled = data.AreSeekerDebugToolsEnabled;
            
            HideDurationSeconds    = data.HideDurationSeconds;
            SeekDurationSeconds    = data.SeekDurationSeconds;
            SeekerCount            = data.SeekerCount;
            EnabledSeekerSelection = (int)data.EnabledSeekerSelection;
            EnabledTaggingMethods  = (int)data.EnabledTaggingMethods;
            EnabledTagResult       = (int)data.EnabledTagResult;
            
            SeekerBodyColor     = data.SeekerBodyColor;
            SeekerEyeColor      = data.SeekerEyeColor;
            SeekerTertiaryColor = data.SeekerTertiaryColor;
            
            Seekers        = new DynamicOrderedUshorts(data.Seekers.Select(seeker => seeker.inLobbyId).ToList());
            InitialSeekers = new DynamicOrderedUshorts(data.InitialSeekers.Select(seeker => seeker.inLobbyId).ToList());
        }
        
        public override void ReadTo(OnlineResource.ResourceData data_, OnlineResource onlineResource)
        {
            AssertIs(data_, out HideAndSeekLobbyData data);
            Assert(onlineResource is Lobby);
            
            if (!HideAndSeekMode.IsHideAndSeekMode(out _)) return;
            
            data.AreSeekerDebugToolsEnabled = AreSeekerDebugToolsEnabled;
            
            data.HideDurationSeconds    = HideDurationSeconds;
            data.SeekDurationSeconds    = SeekDurationSeconds;
            data.SeekerCount            = SeekerCount;
            data.EnabledSeekerSelection = (SeekerSelection)EnabledSeekerSelection;
            data.EnabledTaggingMethods  = (TaggingMethods)EnabledTaggingMethods;
            data.EnabledTagResult       = (TagResult)EnabledTagResult;
            
            data.SeekerBodyColor     = SeekerBodyColor;
            data.SeekerEyeColor      = SeekerEyeColor;
            data.SeekerTertiaryColor = SeekerTertiaryColor;
            
            data.Seekers        = GetSeekersByInLobbyIds(Seekers.list, "seekers");
            data.InitialSeekers = GetSeekersByInLobbyIds(InitialSeekers.list, "initial seekers");
            
            return;
            
            static List<OnlinePlayer> GetSeekersByInLobbyIds(List<ushort> seekerInLobbyIds, string seekerLoggingName)
            {
                List<(ushort id, OnlinePlayer oPlayer)> seekerResults = seekerInLobbyIds
                                                                        .Select(id => (id, ArenaHelpers.FindOnlinePlayerByLobbyId(id)))
                                                                        .ToList();
                
                List<OnlinePlayer> seekers = seekerResults
                                             .Where(result => result.oPlayer is not null)
                                             .Select(result => result.oPlayer)
                                             .ToList();
                
                if (seekers.Count != seekerResults.Count)
                {
                    Logger.Warning(
                        $"""
                        Found {seekers.Count}/{seekerResults.Count} {seekerLoggingName}.
                        - {seekerLoggingName} results: [ {string.Join(", ", seekerResults.Select(result => $"({result.id}, {result.oPlayer?.ToString() ?? "null"})"))} ]
                        """
                    );
                }
                
                return seekers;
            }
        }
        
        public override Type GetDataType() => typeof(HideAndSeekLobbyData);
    }
}