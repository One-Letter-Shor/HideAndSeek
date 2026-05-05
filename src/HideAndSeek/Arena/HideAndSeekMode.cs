using System.Diagnostics.CodeAnalysis;
using On.ArenaBehaviors;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Arena;

/// <remarks>
/// Settings are automatically saved unless
/// <see cref="CanApplySettings"/> is <see langword="false"/>.
/// </remarks>
public sealed partial class HideAndSeekMode : ExternalArenaGameMode
{
    public static ArenaSetup.GameTypeID Id { get; } = new(Plugin.Name);
    
    public bool CanApplySettings { get; set; } = true;
    public int               HideDurationSeconds      { get; set => ApplySetting(value, out field, Plugin.Options.CfgHideDurationSeconds);    } = Plugin.Options.HideDurationSeconds;
    public int               SeekDurationSeconds      { get; set => ApplySetting(value, out field, Plugin.Options.CfgSeekDurationSeconds);    } = Plugin.Options.SeekDurationSeconds;
    public int               SeekerCount              { get; set => ApplySetting(value, out field, Plugin.Options.CfgSeekerCount);            } = Plugin.Options.SeekerCount;
    public SeekerSelection   EnabledSeekerSelection   { get; set => ApplySetting(value, out field, Plugin.Options.CfgEnabledSeekerSelection); } = Plugin.Options.EnabledSeekerSelection;
    public TaggingMethods    EnabledTaggingMethods    { get; set => ApplySetting(value, out field, Plugin.Options.CfgEnabledTaggingMethods);  } = Plugin.Options.EnabledTaggingMethods;
    public TagResult         EnabledTagResult         { get; set => ApplySetting(value, out field, Plugin.Options.CfgEnabledTagResult);       } = Plugin.Options.EnabledTagResult;
    
    public override int TimerDuration
    {
        get => throw new InvalidOperationException("This should not be used.");
        set => throw new InvalidOperationException("This should not be used.");
    }
    public override ArenaSetup.GameTypeID GetGameModeId => Id;
    
    
    /// <summary>Registers game mode via <see cref="ArenaOnlineGameMode.AddExternalGameModes"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown if already registered.</exception>
    internal static void RegisterNewInstance(ArenaOnlineGameMode arenaOnline)
    {
        if (arenaOnline.registeredGameModes.TryGetValue(Id.value, out _))
            throw new InvalidOperationException($"Game mode is already registered. registered: [ {string.Join(", ", arenaOnline.registeredGameModes.Keys)} ]");
        
        arenaOnline.AddExternalGameModes(Id, new HideAndSeekMode());
    }
    
    public static bool IsHideAndSeekMode(ArenaOnlineGameMode arenaOnline, [NotNullWhen(true)] out HideAndSeekMode? hideAndSeek)
    {
        string modeName = Id.value;
        if (!arenaOnline.registeredGameModes.TryGetValue(modeName, out ExternalArenaGameMode registeredMode)) 
            throw new InvalidOperationException($"Could not find game mode. registered: [ {string.Join(", ", arenaOnline.registeredGameModes.Keys)} ]");
        
        hideAndSeek = null;
        if (arenaOnline.currentGameMode == modeName)
        {
            hideAndSeek = (HideAndSeekMode)registeredMode;
            return true;
        }
        
        return false;
    }
    
    
    public override bool SpawnBatflies(FliesWorldAI fliesWorldAI, int spawnRoom) => false;
    
    public override string TimerText() => "Quickly, hide!";
    
    public override int SetTimer(ArenaOnlineGameMode arenaOnline) => arenaOnline.setupTime = HideDurationSeconds;
    
    public override bool IsExitsOpen(
        ArenaOnlineGameMode arenaOnline,
        ExitManager.orig_ExitsOpen orig,
        ArenaBehaviors.ExitManager exitManager)
    {
        return orig(exitManager);
    }
    
    public override void ArenaSessionCtor(
        ArenaOnlineGameMode arenaOnline,
        On.ArenaGameSession.orig_ctor orig,
        ArenaGameSession arena,
        RainWorldGame game)
    {
        base.ArenaSessionCtor(arenaOnline, orig, arena, game);
        
        LogGameInfo(arena, arenaOnline);
    }
    
    /// <summary>
    /// Clamps value based on the <see cref="Configurable{T}"/>
    /// and writes to it if currently the host.
    /// </summary>
    /// <remarks>
    /// If <see cref="CanApplySettings"/> is <see langword="false"/>,
    /// writing to the <see cref="Configurable{T}"/> is disabled.
    /// </remarks>
    private void ApplySetting<T>(T value, out T field, Configurable<T> configurable)
    {
        Assert(OnlineManager.lobby is not null);
        
        value = configurable.ClampValue(value);
        
        if (CanApplySettings && OnlineManager.lobby.isOwner)
            configurable.Value = value;
        
        
        field = value;
    }
    
    private void LogGameInfo(ArenaGameSession arena, ArenaOnlineGameMode arenaOnline) // temp
    {
        Logger.Info(
            $"""
            INFO:
            - [arena]    players:  -  -  -  -  -  -  -  -  -  -  [ {string.Join(", ", arena.Players)} ]
            - [arena]    arena sitting players:   -  -  -  -  -  [ {string.Join(", ", arena.arenaSitting.players)} ]
            - [online]   waiting for next round count:  -  -  -  [ {string.Join(", ", arenaOnline.playersLateWaitingInLobbyForNextRound.Select(inLobbyId => ArenaHelpers.FindOnlinePlayerByLobbyId(inLobbyId)?.id.name ?? "null"))} ]
            - [online]   equal to online sitting: -  -  -  -  -  {arenaOnline.playersEqualToOnlineSitting}
            """
        );
    }
}