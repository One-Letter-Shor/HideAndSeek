using OneLetterShor.HideAndSeek.Utils;
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
        if (!GameHelper.IsInGame)
        {
            Logger.Debug($"Not currently in game. {fromText}");
            return;
        }
        
        ChatLogManager.LogSystemMessage($"{target.id.DisplayName} was tagged by {tagger.id.DisplayName}.");
    }
}