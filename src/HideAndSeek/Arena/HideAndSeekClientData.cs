using MonoMod.RuntimeDetour;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;
using UnityEngine.Assertions;

namespace OneLetterShor.HideAndSeek.Arena;

internal sealed class HideAndSeekClientData : OnlineEntity.EntityData
{
    /// <summary>
    /// Gets the <see cref="HideAndSeekClientData"/> instance that
    /// correlates to the <see cref="OnlineManager.mePlayer"/>.
    /// </summary>
    /// <exception cref="AssertionException">
    /// Thrown when:
    /// <list type="bullet">
    /// <item><description>There is no <see cref="Lobby"/>.</description></item>
    /// <item><description>The <see cref="ClientSettings"/> does not contain the me player's data.</description></item>
    /// </list>
    /// </exception>
    public static HideAndSeekClientData MyClientData
    {
        get
        {
            Assert(OnlineManager.lobby is not null);
            Assert(OnlineManager.lobby.clientSettings.ContainsKey(OnlineManager.mePlayer));
            
            return OnlineManager.lobby
                                .clientSettings[OnlineManager.mePlayer]
                                .GetData<HideAndSeekClientData>();
        }
    }
    
    public bool IsWillingToSeek { get; set => ApplyClientSetting(value, out field, Cfg.Options.Instance.CfgIsWillingToSeek); }
    
    
    internal static void ApplyHooksAndEvents()
    {
        _ = new Hook(
            RainMeadowCache.ArenaOnlineGameMode_AddClientData,
            On_RainMeadow_ArenaOnlineGameMode_AddClientData
        );
    }
    
    private static void On_RainMeadow_ArenaOnlineGameMode_AddClientData(Action<ArenaOnlineGameMode> orig, ArenaOnlineGameMode self)
    {
        orig(self);
        Logger.Mark("?ploo");
        self.clientSettings.AddData(new HideAndSeekClientData());
    }
    
    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(this);
    }
    
    public void ApplyClientSetting<T>(T value, out T field, Configurable<T> configurable)
    {
        Assert(OnlineManager.lobby is not null);
        
        value = configurable.ClampValue(value);
        Logger.Debug($"{configurable} -> {value}");
        
        if (this == MyClientData)
            configurable.Value = value;
        
        field = value;
    }
    
    internal sealed class State : EntityDataState
    {
        private const string _group = nameof(HideAndSeekClientData);
        
        [OnlineField(group = _group)]
        public bool IsWillingToSeek;
        
        
        /// <remarks>Rain Meadow requires a ctor with no params.</remarks>
        public State() { Logger.Mark(); }
        
        internal State(HideAndSeekClientData data)
        {
            IsWillingToSeek = data.IsWillingToSeek;
        }
        
        public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
        {
            AssertIs(entityData, out HideAndSeekClientData data);
            
            Logger.Mark($"Is willing to seek {IsWillingToSeek}");
            
            data.IsWillingToSeek = IsWillingToSeek;
        }
        
        public override Type GetDataType() => typeof(HideAndSeekLobbyData);
    }
}