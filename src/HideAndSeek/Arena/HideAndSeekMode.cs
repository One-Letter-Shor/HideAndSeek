using System.Diagnostics.CodeAnalysis;
using MonoMod.RuntimeDetour;
using On.ArenaBehaviors;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed partial class HideAndSeekMode : ExternalArenaGameMode
{
    public static ArenaSetup.GameTypeID GameModeId { get; } = new(Plugin.Name);
    
    
    public int                   RoundDurationSeconds         { get; set => ApplyLobbySetting(value, out field, Cfg.Options.Instance.CfgRoundDurationSeconds);       } = Cfg.Options.RoundDurationSeconds;
    public int                   SeekerCount                  { get; set => ApplyLobbySetting(value, out field, Cfg.Options.Instance.CfgSeekerCount);                } = Cfg.Options.SeekerCount;
    public SeekerSelectionType   EnabledSeekerSelectionType   { get; set => ApplyLobbySetting(value, out field, Cfg.Options.Instance.CfgEnabledSeekerSelectionType); } = Cfg.Options.EnabledSeekerSelectionType;
    public TaggingTypes          EnabledTaggingTypes          { get; set => ApplyLobbySetting(value, out field, Cfg.Options.Instance.CfgEnabledTaggingTypes);        } = Cfg.Options.EnabledTaggingTypes;
    public TagResultType         EnabledTagResultType         { get; set => ApplyLobbySetting(value, out field, Cfg.Options.Instance.CfgEnabledTagResultType);       } = Cfg.Options.EnabledTagResultType;
    
    /// <remarks>Time measured in seconds.</remarks>
    public override int TimerDuration { get; set; }
    
    public List<OnlinePlayer> Seekers { get; private set; } = [];
    public List<OnlinePlayer> Hiders { get; private set; } = [];
    
    public override ArenaSetup.GameTypeID GetGameModeId
    {
        get => GameModeId;
        set => throw new InvalidOperationException("Setter should not be used.");
    }
    
    public static void ApplyHooksAndEvents() 
    {
        _ = new Hook(
            RainMeadowCache.ArenaOnlineGameMode_ctor,
            On_RainMeadow_ArenaOnlineGameMode_ctor
        );
    }
    
    /// <summary>Registers this game mode in each <see cref="ArenaOnlineGameMode"/> instance.</summary>
    private static void On_RainMeadow_ArenaOnlineGameMode_ctor(
        Action<ArenaOnlineGameMode, Lobby> orig,
        ArenaOnlineGameMode self,
        Lobby lobby)
    {
        orig(self, lobby);
        self.AddExternalGameModes(GameModeId, new HideAndSeekMode());
    }
    
    public static bool IsHideAndSeekMode(ArenaOnlineGameMode arenaOnline, [NotNullWhen(true)] out HideAndSeekMode? hideAndSeek)
    {
        string modeName = GameModeId.value;
        if (!arenaOnline.registeredGameModes.TryGetValue(modeName, out ExternalArenaGameMode registeredMode))
            throw new InvalidOperationException($"Could not find a game mode related to game type id. registered: [ {string.Join(", ", arenaOnline.registeredGameModes)} ]");
        
        hideAndSeek = null;
        if (arenaOnline.currentGameMode == modeName)
        {
            hideAndSeek = (HideAndSeekMode)registeredMode;
            return true;
        }
        
        return false;
    }
    
    public override string TimerText() => "Quickly, hide!";
    
    public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom) => false;
    
    public override bool IsExitsOpen(ArenaOnlineGameMode arenaOnline, ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
    {
        return orig(self);
    }
    
    private void ApplyLobbySetting<T>(T value, out T field, Configurable<T> configurable)
    {
        Assert(OnlineManager.lobby is not null);
        
        value = configurable.ClampValue(value);
        Logger.Debug($"{configurable} -> {value}");
        
        if (OnlineManager.lobby.isOwner)
            configurable.Value = value;
        
        field = value;
    }
}