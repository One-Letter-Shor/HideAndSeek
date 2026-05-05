using System.Diagnostics.CodeAnalysis;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Arena;

internal sealed class HideAndSeekClientData : OnlineEntity.EntityData
{
    public bool IsWillingToSeek { get; set; }
    
    
    /// <summary>Registers client data via <see cref="ClientSettings.AddData"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown if already registered.</exception>
    internal static void RegisterNewInstance(ArenaOnlineGameMode arenaOnline)
    {
        if (arenaOnline.clientSettings.TryGetData(typeof(HideAndSeekClientData), out _))
            throw new InvalidOperationException("Client data is already registered.");
        
        arenaOnline.clientSettings.AddData(new HideAndSeekClientData());
    }
    
    /// <summary>
    /// Gets the <see cref="HideAndSeekClientData"/> instance that
    /// correlates to the <see cref="OnlineManager.mePlayer"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:
    /// <br/>- There is no <see cref="Lobby"/>.
    /// <br/>- The <see cref="ClientSettings"/> could not be found.
    /// </exception>
    public static HideAndSeekClientData GetMyData()
    {
        if (OnlineManager.lobby is null) throw new InvalidOperationException("There is no lobby.");
        if (!OnlineManager.lobby.clientSettings.ContainsKey(OnlineManager.mePlayer)) throw new InvalidOperationException($"Could not find client settings. [ {string.Join(", ",OnlineManager.lobby.clientSettings)} ]");
        
        return OnlineManager.lobby
                            .clientSettings[OnlineManager.mePlayer]
                            .GetData<HideAndSeekClientData>();
    }
    
    /// <summary>
    /// Attempts to get the <see cref="HideAndSeekClientData"/> instance that
    /// correlates to the <see cref="OnlineManager.mePlayer"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the data is found, otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if there is no <see cref="Lobby"/>.</exception>
    /// <remarks>
    /// Fails gracefully on both <see cref="Lobby.clientSettings"/> and
    /// <see cref="ClientSettings.TryGetData"/>. (returns <see langword="false"/>)
    /// </remarks>
    public static bool TryGetMyData([NotNullWhen(true)] out HideAndSeekClientData? data)
    {
        if (OnlineManager.lobby is null) throw new InvalidOperationException("There is no lobby.");
        
        data = null;
        
        if (
            !OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out ClientSettings clientSettings)
            || !clientSettings.TryGetData(out HideAndSeekClientData _data)
        ) return false;
        
        data = _data;
        return true;
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
        
        public override void ReadTo(OnlineEntity.EntityData entityData, OnlineEntity onlineEntity)
        {
            AssertIs(entityData, out HideAndSeekClientData data);
            
            data.IsWillingToSeek = IsWillingToSeek;
        }
        
        public override Type GetDataType() => typeof(HideAndSeekClientData);
    }
}