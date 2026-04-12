using MonoMod.RuntimeDetour;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Arena;

internal sealed class HideAndSeekLobbyData : OnlineResource.ResourceData
{
    internal static void ApplyHooksAndEvents()
    {
        _ = new Hook(
            RainMeadowCache.Lobby_ActivateImpl,
            On_RainMeadow_Lobby_ActivateImpl
        );
    }
    
    private static void On_RainMeadow_Lobby_ActivateImpl(Action<Lobby> orig, Lobby self)
    {
        orig(self);
        OnlineManager.lobby.AddData(new HideAndSeekLobbyData());
    }
    
    public override ResourceDataState MakeState(OnlineResource resource)
    {
        AssertIs(resource, out Lobby lobby);
        
        return new State(this, lobby);
    }
    
    internal sealed class State : ResourceDataState
    {
        private const string _group = nameof(HideAndSeekLobbyData);
        
        [OnlineField(group = _group)]
        public int RoundDurationSeconds;
        
        [OnlineField(group = _group)]
        public int SeekerCount;
        
        [OnlineField(group = _group)]
        public byte EnabledSeekerSelectionType;
        
        [OnlineField(group = _group)]
        public byte EnabledTaggingTypes;
        
        [OnlineField(group = _group)]
        public byte EnabledTagResultType;
        
        
        /// <remarks>Rain Meadow requires a ctor with no params.</remarks>
        public State() { Logger.Mark(); }
        
        internal State(HideAndSeekLobbyData data, Lobby lobby)
        {
            AssertIs(lobby.gameMode, out ArenaOnlineGameMode arenaOnline);
            
            if (!HideAndSeekMode.IsHideAndSeekMode(arenaOnline, out HideAndSeekMode? hideAndSeek)) return;
            
            RoundDurationSeconds       = hideAndSeek.RoundDurationSeconds;
            SeekerCount                = hideAndSeek.SeekerCount;
            EnabledSeekerSelectionType = (byte)hideAndSeek.EnabledSeekerSelectionType;
            EnabledTaggingTypes        = (byte)hideAndSeek.EnabledTaggingTypes;
            EnabledTagResultType       = (byte)hideAndSeek.EnabledTagResultType;
        }
        
        public override void ReadTo(OnlineResource.ResourceData data, OnlineResource onlineResource)
        {
            Logger.Mark();
            AssertIs(onlineResource, out Lobby lobby);
            AssertIs(lobby.gameMode, out ArenaOnlineGameMode arenaOnline);
            
            if (!HideAndSeekMode.IsHideAndSeekMode(arenaOnline, out HideAndSeekMode? hideAndSeek)) return;
            
            hideAndSeek.RoundDurationSeconds       = RoundDurationSeconds;
            hideAndSeek.SeekerCount                = SeekerCount;
            hideAndSeek.EnabledSeekerSelectionType = (SeekerSelectionType)EnabledSeekerSelectionType;
            hideAndSeek.EnabledTaggingTypes        = (TaggingTypes)EnabledTaggingTypes;
            hideAndSeek.EnabledTagResultType       = (TagResultType)EnabledTagResultType;
        }
        
        public override Type GetDataType() => typeof(HideAndSeekLobbyData);
    }
}