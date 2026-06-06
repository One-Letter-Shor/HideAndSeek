using OneLetterShor.HideAndSeek.Arena;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Hooking;

public static class RainWorldHooks
{
    internal static void Apply()
    {
        On.Player.Collide += On_Player_Collide;
    }
    
    private static void On_Player_Collide(
        On.Player.orig_Collide orig,
        Player self,
        PhysicalObject otherObject,
        int chunkIndex,
        int otherChunkIndex)
    {
        // TODO: Handle devtool teleporting and stuff.
        
        if (HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek) &&
            otherObject is Player otherPlayer)
        {
            OnlineCreature oCreature = self.abstractCreature.GetOnlineCreature()!;             // TODO: Possible NRE?
            OnlineCreature otherOCreature = otherPlayer.abstractCreature.GetOnlineCreature()!; // TODO: Possible NRE?
            
            if (oCreature.isMine &&
                oCreature.owner.IsSeeker &&
                !otherOCreature.owner.IsSeeker)
            {
                Logger.Debug($"{oCreature.owner} collided with {otherOCreature.owner}!");
                hideAndSeek.MakeSeeker(otherOCreature.owner);
            }
        }
        
        orig(self, otherObject, chunkIndex, otherChunkIndex);
    }
}