using System.Diagnostics.CodeAnalysis;
using On.ArenaBehaviors;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Arena;

public sealed partial class HideAndSeekMode : ExternalArenaGameMode
{
    public static ArenaSetup.GameTypeID Id { get; } = new(Plugin.Name);
    
    /// <exception cref="KeyNotFoundException">Thrown if the lobby data is not registered yet.</exception>
    public HideAndSeekLobbyData LobbyData => OnlineManager.lobby!.GetData<HideAndSeekLobbyData>();
    
    /// <exception cref="KeyNotFoundException">Thrown if the client data is not registered yet.</exception>
    public HideAndSeekClientData MyClientData => OnlineManager.lobby!
                                                              .clientSettings[OnlineManager.mePlayer]
                                                              .GetData<HideAndSeekClientData>();
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
    
    /// <exception cref="InvalidOperationException">Thrown if the game mode is not registered.</exception>
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
    
    public override int SetTimer(ArenaOnlineGameMode arenaOnline) => arenaOnline.setupTime = LobbyData.HideDurationSeconds;
    
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