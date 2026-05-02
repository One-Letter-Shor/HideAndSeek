using RainMeadow;

namespace OneLetterShor.HideAndSeek.Arena;

internal sealed class HideAndSeekLobbyData : OnlineResource.ResourceData
{
    /// <summary>Registers lobby data via <see cref="Lobby.AddData"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown if already registered.</exception>
    internal static void RegisterNewInstance(Lobby lobby)
    {
        if (lobby.TryGetData<HideAndSeekLobbyData>(out _))
            throw new InvalidOperationException("Lobby data is already registered.");
        
        lobby.AddData(new HideAndSeekLobbyData());
    }
    
    
    public override ResourceDataState MakeState(OnlineResource resource)
    {
        AssertIs(resource, out Lobby lobby);
        
        return new State(lobby);
    }
    
    internal sealed class State : ResourceDataState
    {
        private const string _settings = nameof(_settings);
        
        [OnlineField(group = _settings)]
        public int HideDurationSeconds;
        
        [OnlineField(group = _settings)]
        public int RoundDurationSeconds;
        
        [OnlineField(group = _settings)]
        public int SeekerCount;
        
        [OnlineField(group = _settings)]
        public byte EnabledSeekerSelection;
        
        [OnlineField(group = _settings)]
        public byte EnabledTaggingMethods;
        
        [OnlineField(group = _settings)]
        public byte EnabledTagResult;
        
        
        /// <remarks>Rain Meadow requires a ctor with no params.</remarks>
        public State() { Logger.Mark(); }
        
        internal State(Lobby lobby)
        {
            AssertIs(lobby.gameMode, out ArenaOnlineGameMode arenaOnline);
            
            if (!HideAndSeekMode.IsHideAndSeekMode(arenaOnline, out HideAndSeekMode? hideAndSeek)) return;
            
            HideDurationSeconds    = hideAndSeek.HideDurationSeconds;
            RoundDurationSeconds   = hideAndSeek.SeekDurationSeconds;
            SeekerCount            = hideAndSeek.SeekerCount;
            EnabledSeekerSelection = (byte)hideAndSeek.EnabledSeekerSelection;
            EnabledTaggingMethods  = (byte)hideAndSeek.EnabledTaggingMethods;
            EnabledTagResult       = (byte)hideAndSeek.EnabledTagResult;
        }
        
        public override void ReadTo(OnlineResource.ResourceData data, OnlineResource onlineResource)
        {
            Logger.Mark();
            AssertIs(onlineResource, out Lobby lobby);
            AssertIs(lobby.gameMode, out ArenaOnlineGameMode arenaOnline);
            
            if (!HideAndSeekMode.IsHideAndSeekMode(arenaOnline, out HideAndSeekMode? hideAndSeek)) return;
            
            hideAndSeek.HideDurationSeconds    = HideDurationSeconds;
            hideAndSeek.SeekDurationSeconds   = RoundDurationSeconds;
            hideAndSeek.SeekerCount            = SeekerCount;
            hideAndSeek.EnabledSeekerSelection = (SeekerSelection)EnabledSeekerSelection;
            hideAndSeek.EnabledTaggingMethods  = (TaggingMethods)EnabledTaggingMethods;
            hideAndSeek.EnabledTagResult       = (TagResult)EnabledTagResult;
        }
        
        public override Type GetDataType() => typeof(HideAndSeekLobbyData);
    }
}