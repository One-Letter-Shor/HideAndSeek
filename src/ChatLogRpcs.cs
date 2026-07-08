using OneLetterShor.HideAndSeek.Arena;
using RainMeadow;

namespace OneLetterShor.HideAndSeek;

public static class ChatLogRpcs
{
    [RPCMethod]
    public static void SystemLogPlayerTaggedRpc(RPCEvent? rpcEvent, OnlinePlayer tagger, OnlinePlayer target)
    {
        Assert(rpcEvent?.from.isMe != true);
        string fromText = "From: " + (rpcEvent?.from.ToString() ?? "(local)") + ".";
        
        if (rpcEvent is not null && rpcEvent.from != OnlineManager.lobby!.owner)
        {
            Logger.Error($"RPC is not sent from host. {fromText}");
            return;
        }
        if (!HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            Logger.Warning($"The game mode is not Hide and Seek. {fromText}");
            return;
        }
        if (hideAndSeek.HasRecognizedSeekerWin &&
            !hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled)
        {
            Logger.Error(
                $"Seeker win has already been recognized. " +
                $"Continuing in the hopes that this is a minor bug. {fromText}"
            );
        }
        
        ChatLogManager.LogSystemMessage($"{target.id.DisplayName} was tagged by {tagger.id.DisplayName}.");
    }
    
    /// <remarks>
    /// Calls <see cref="SystemLogPlayerTaggedRpc"/>
    /// then <see cref="SystemLogSeekerWinRpc"/> to prevent
    /// the system messages from being out of order for clients.<br/>
    /// (<see cref="SystemLogSeekerWinRpc"/> sets
    /// <see cref="HideAndSeekMode.HasRecognizedSeekerWin"/>
    /// to <see langword="true"/> if me player is the host.)
    /// </remarks>
    [RPCMethod]
    public static void SystemLogLastPlayerTaggedRpc(RPCEvent? rpcEvent, OnlinePlayer tagger, OnlinePlayer target)
    {
        Assert(rpcEvent?.from.isMe != true);
        string fromText = "From: " + (rpcEvent?.from.ToString() ?? "(local)") + ".";
        
        if (rpcEvent is not null && rpcEvent.from != OnlineManager.lobby!.owner)
        {
            Logger.Error($"RPC is not sent from host. {fromText}");
            return;
        }
        if (!HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            Logger.Warning($"The game mode is not Hide and Seek. {fromText}");
            return;
        }
        if (hideAndSeek.HasRecognizedSeekerWin &&
            !hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled)
        {
            Logger.Error(
                $"Seeker win has already been recognized. " +
                $"Continuing in the hopes that this is a minor bug. {fromText}"
            );
        }
        
        SystemLogPlayerTaggedRpc(rpcEvent, tagger, target);
        SystemLogSeekerWinRpc(rpcEvent);
    }
    
    /// <remarks>
    /// Sets <see cref="HideAndSeekMode.HasRecognizedSeekerWin"/>
    /// to <see langword="true"/> if me player is the host.
    /// </remarks>
    [RPCMethod]
    public static void SystemLogSeekerWinRpc(RPCEvent? rpcEvent)
    {
        Assert(rpcEvent?.from.isMe != true);
        string fromText = "From: " + (rpcEvent?.from.ToString() ?? "(local)") + ".";
        
        if (rpcEvent is not null && rpcEvent.from != OnlineManager.lobby!.owner)
        {
            Logger.Error($"RPC is not sent from host. {fromText}");
            return;
        }
        if (!HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            Logger.Warning($"The game mode is not Hide and Seek. {fromText}");
            return;
        }
        if (hideAndSeek.HasRecognizedSeekerWin &&
            !hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled)
        {
            Logger.Error($"Seeker winning has already been recognized. {fromText}");
            return;
        }
        
        ChatLogManager.LogSystemMessage("The seekers have tagged everyone!");
        
        if (OnlineManager.lobby!.isOwner)
            hideAndSeek.HasRecognizedSeekerWin = true;
    }
    
    /// <remarks>
    /// Sets <see cref="HideAndSeekMode.HasRecognizedHiderWin"/>
    /// to <see langword="true"/> if me player is the host.
    /// </remarks>
    [RPCMethod]
    public static void SystemLogHiderWinRpc(RPCEvent? rpcEvent)
    {
        Assert(rpcEvent?.from.isMe != true);
        string fromText = "From: " + (rpcEvent?.from.ToString() ?? "(local)") + ".";
        
        if (rpcEvent is not null && rpcEvent.from != OnlineManager.lobby!.owner)
        {
            Logger.Error($"RPC is not sent from host. {fromText}");
            return;
        }
        if (!HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            Logger.Warning($"The game mode is not Hide and Seek. {fromText}");
            return;
        }
        if (hideAndSeek.HasRecognizedHiderWin &&
            !hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled)
        {
            Logger.Error($"Hider winning has already been recognized. {fromText}");
            return;
        }
        
        ChatLogManager.LogSystemMessage("Seeking time is over!  Hiders are now safe.");
        
        if (OnlineManager.lobby!.isOwner)
            hideAndSeek.HasRecognizedHiderWin = true;
    }
}