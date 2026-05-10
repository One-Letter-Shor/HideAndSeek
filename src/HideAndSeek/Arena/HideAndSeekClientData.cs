using RainMeadow;

namespace OneLetterShor.HideAndSeek.Arena;

/// <remarks>
/// Settings are automatically saved unless
/// <see cref="CanApplySettings"/> is <see langword="false"/>.
/// </remarks>
public sealed class HideAndSeekClientData : OnlineEntity.EntityData
{
    /// <summary>Registers client data via <see cref="ClientSettings.AddData"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown if already registered.</exception>
    internal static void RegisterNewInstance(ArenaOnlineGameMode arenaOnline)
    {
        if (arenaOnline.clientSettings.TryGetData(typeof(HideAndSeekClientData), out _))
            throw new InvalidOperationException("Client data is already registered.");
        
        arenaOnline.clientSettings.AddData(new HideAndSeekClientData());
    }
    
    public bool CanApplySettings { get; set; } = true;
    public bool IsWillingToSeek { get; set => ApplySetting(value, out field, Plugin.Options.CfgIsWillingToSeek); } = Plugin.Options.IsWillingToSeek;
    
    /// <summary>
    /// Clamps the value based on <paramref name="configurable"/>
    /// and writes to it if both the client data is for the me player and
    /// <see cref="CanApplySettings"/> is <see langword="true"/>.
    /// </summary>
    private void ApplySetting<T>(T value, out T field, Configurable<T> configurable)
    {
        Assert(OnlineManager.lobby is not null);
        
        value = configurable.ClampValue(value);
        
        // TODO: Only save if the client data is for me player. 
        if (CanApplySettings)
            configurable.Value = value;
        
        field = value;
    }
    
    public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
    {
        return new State(this);
    }
    
    internal sealed class State : EntityDataState
    {
        private const string _settings = nameof(_settings);
        
        [OnlineField(group = _settings)]
        public bool IsWillingToSeek;
        
        
        /// <remarks>Rain Meadow requires a ctor with no params.</remarks>
        public State() { }
        
        internal State(HideAndSeekClientData data)
        {
            IsWillingToSeek = data.IsWillingToSeek;
        }
        
        public override void ReadTo(OnlineEntity.EntityData data_, OnlineEntity onlineEntity)
        {
            AssertIs(data_, out HideAndSeekClientData data);
            
            data.IsWillingToSeek = IsWillingToSeek;
        }
        
        public override Type GetDataType() => typeof(HideAndSeekClientData);
    }
}