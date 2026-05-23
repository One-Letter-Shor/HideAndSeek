using RainMeadow;

namespace OneLetterShor.HideAndSeek.Arena;

/// <remarks>
/// Settings are automatically saved unless
/// <see cref="CanApplySettings"/> is <see langword="false"/>.
/// </remarks>
public sealed class HideAndSeekClientData : OnlineEntity.EntityData
{
    public static bool CanApplySettings { get; set; } = true;
    
    /// <remarks>Init-only setter.</remarks>
    public bool IsMine { get; internal set; } = false;
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
        
        if (CanApplySettings && IsMine)
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
            Assert(onlineEntity.isMine == data.IsMine);
            
            data.IsWillingToSeek = IsWillingToSeek;
        }
        
        public override Type GetDataType() => typeof(HideAndSeekClientData);
    }
}