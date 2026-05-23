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
    
    public int               HideDurationSeconds      { get; set => ApplySetting(value, out field, Plugin.Options.CfgHideDurationSeconds);    } = Plugin.Options.HideDurationSeconds;
    public int               SeekDurationSeconds      { get; set => ApplySetting(value, out field, Plugin.Options.CfgSeekDurationSeconds);    } = Plugin.Options.SeekDurationSeconds;
    public int               SeekerCount              { get; set => ApplySetting(value, out field, Plugin.Options.CfgSeekerCount);            } = Plugin.Options.SeekerCount;
    public SeekerSelection   EnabledSeekerSelection   { get; set => ApplySetting(value, out field, Plugin.Options.CfgEnabledSeekerSelection); } = Plugin.Options.EnabledSeekerSelection;
    public TaggingMethods    EnabledTaggingMethods    { get; set => ApplySetting(value, out field, Plugin.Options.CfgEnabledTaggingMethods);  } = Plugin.Options.EnabledTaggingMethods;
    public TagResult         EnabledTagResult         { get; set => ApplySetting(value, out field, Plugin.Options.CfgEnabledTagResult);       } = Plugin.Options.EnabledTagResult;
    
    public List<OnlinePlayer> Seekers
    {
        get;
        set
        {
            if (value.SequenceEqual(field))
                Logger.Info($"Seekers: [ {string.Join(", ", value)} ]");
            
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
        private const string _settings = nameof(_settings);
        
        [OnlineField(group = _settings)]
        public int HideDurationSeconds;
        
        [OnlineField(group = _settings)]
        public int SeekDurationSeconds;
        
        [OnlineField(group = _settings)]
        public int SeekerCount;
        
        [OnlineField(group = _settings)]
        public byte EnabledSeekerSelection;
        
        [OnlineField(group = _settings)]
        public byte EnabledTaggingMethods;
        
        [OnlineField(group = _settings)]
        public byte EnabledTagResult;
        
        [OnlineField]
        public DynamicOrderedUshorts Seekers = new([]);
        
        /// <remarks>Rain Meadow requires a ctor with no params.</remarks>
        public State() { }
        
        internal State(HideAndSeekLobbyData data, Lobby lobby)
        {
            AssertIs(lobby.gameMode, out ArenaOnlineGameMode arenaOnline);
            
            if (!HideAndSeekMode.IsHideAndSeekMode(arenaOnline, out _)) return;
            
            HideDurationSeconds    = data.HideDurationSeconds;
            SeekDurationSeconds    = data.SeekDurationSeconds;
            SeekerCount            = data.SeekerCount;
            EnabledSeekerSelection = (byte)data.EnabledSeekerSelection;
            EnabledTaggingMethods  = (byte)data.EnabledTaggingMethods;
            EnabledTagResult       = (byte)data.EnabledTagResult;
            
            Seekers = new DynamicOrderedUshorts(data.Seekers.Select(seeker => seeker.inLobbyId).ToList());
        }
        
        // TODO: Handle enum casts gracefully.
        public override void ReadTo(OnlineResource.ResourceData data_, OnlineResource onlineResource)
        {
            AssertIs(data_, out HideAndSeekLobbyData data);
            AssertIs(onlineResource, out Lobby lobby);
            AssertIs(lobby.gameMode, out ArenaOnlineGameMode arenaOnline);
            
            if (!HideAndSeekMode.IsHideAndSeekMode(arenaOnline, out _)) return;
            
            data.HideDurationSeconds    = HideDurationSeconds;
            data.SeekDurationSeconds    = SeekDurationSeconds;
            data.SeekerCount            = SeekerCount;
            data.EnabledSeekerSelection = (SeekerSelection)EnabledSeekerSelection;
            data.EnabledTaggingMethods  = (TaggingMethods)EnabledTaggingMethods;
            data.EnabledTagResult       = (TagResult)EnabledTagResult;
            
            data.Seekers = GetSeekers();
            
            return;
            
            List<OnlinePlayer> GetSeekers()
            {
                List<(ushort id, OnlinePlayer oPlayer)> seekerResults = Seekers.list
                                                                               .Select(id => (id, ArenaHelpers.FindOnlinePlayerByLobbyId(id)))
                                                                               .ToList();
                
                List<OnlinePlayer> seekers = seekerResults
                                             .Where(result => result.oPlayer is not null)
                                             .Select(result => result.oPlayer)
                                             .ToList();
                
                if (seekers.Count != seekerResults.Count)
                {
                    Logger.Info(
                        $"""
                        Found {seekers.Count}/{seekerResults.Count} seekers.
                        - seeker results: [ {string.Join(", ", seekerResults.Select(result => $"({result.id}, {result.oPlayer?.ToString() ?? "null"})"))} ]
                        """
                    );
                }
                
                return seekers;
            }
        }
        
        public override Type GetDataType() => typeof(HideAndSeekLobbyData);
    }
}