using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using OneLetterShor.HideAndSeek.Arena;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Hooking;

public static class RainWorldHooks
{
    internal static void Apply()
    {
        On.RainWorldGame.GrafUpdate += On_RainWorldGame_GrafUpdate;
        On.Player.Collide += On_Player_Collide;
        On.Rock.HitSomething += On_Rock_HitSomething;
        
        _ = new ILHook(
            typeof(Player).GetMethod(
                nameof(Player.ClassMechanicsSaint),
                BindingFlags.Public | BindingFlags.Instance
            ),
            IL_Player_ClassMechanicsSaint
        );
    }
    
    private static void IL_Player_ClassMechanicsSaint(ILContext il)
    {
        /*
         (last updated: 6/6/26)
         
         
         Current code:
         ...
         foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
         {
             if (Custom.DistLess(bodyChunk.pos, vector2, num6 + bodyChunk.rad) && this.room.VisualContact(bodyChunk.pos, vector2))
             {
                 bodyChunk.vel += Custom.RNV() * 36f;
                 if (physicalObject is Creature)
                 {
                     if (!(physicalObject as Creature).dead)
                         flag2 = true;
                     (physicalObject as Creature).Die();
                 }
         ...
         
         Note: Rain Meadow hooks the same part of this method. (Arena/ArenaHooks.cs:1633)
        
         Desired code (excluding Rain Meadow's emitted instructions):
         ...
         foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
         {
             if (Custom.DistLess(bodyChunk.pos, vector2, num6 + bodyChunk.rad) && this.room.VisualContact(bodyChunk.pos, vector2))
             {
                 bodyChunk.vel += Custom.RNV() * 36f;
                 if (physicalObject is Creature)
                 {
                     if (!(physicalObject as Creature).dead)
                         flag2 = true;
                     
                     if (HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek) &&
                         otherPO is Player otherPlayer)
                     {
                         OnlineCreature saintOCreature = saintPlayer.abstractCreature.GetOnlineCreature()!;
                         OnlineCreature otherOCreature = otherPlayer.abstractCreature.GetOnlineCreature()!;
                         
                         if (hideAndSeek.LobbyData.EnabledTaggingMethods.HasFlag(TaggingMethods.Contact) &&
                             saintOCreature.isMine &&
                             saintOCreature.owner.IsSeeker &&
                             !otherOCreature.owner.IsSeeker
                         )
                             hideAndSeek.MakeSeeker(otherOCreature.owner);
                     }
                     else
                         (physicalObject as Creature).Die();
                 }
         ...
         
         example of the instructions (simplified)
         
         physObj                      // Emitted from Hide and Seek
         self                         // Emitted from Hide and Seek
         <Func(self, physObj, bool)>  // Emitted from Hide and Seek
         brtrue skip <-               // Emitted from Hide and Seek
         physObj
         isinst Creature
         self                         // Emitted from Rain Meadow
         physObj                      // Emitted from Rain Meadow
         <Action(self, physObj)>      // Emitted from Rain Meadow
         Die()
         skip:                        // Emitted from Hide and Seek
        */
        
        // TODO: Disable flinging and sound when the hit player is not a hider and when the ascender is not a seeker.
        try
        {
            ILCursor cursor = new(il);
            ILLabel skip = cursor.DefineLabel();
            
            const int locIndex18 = 18; // No source code name provided. physical object that is used to kill if ascended.
            
            cursor.GotoNext(
                MoveType.After,
                i => i.MatchCallvirt<Creature>(nameof(Creature.Die)) // The only usage of Die() is when saint is successfully ascending something.
            );
            
            cursor.MarkLabel(skip);
            
            // TODO: Why the slug does there need to be two GotoPrev()s when the instructions are back to back. (No other IL hook is interfering, the index change from one to the next is 1)
            
            // Move before the instructions needed to call Die().
            cursor.GotoPrev(
                MoveType.Before,
                i => i.MatchIsinst<Creature>()
            ).GotoPrev(
                MoveType.Before,
                i => i.MatchLdloc(locIndex18)
            );
            
            
            // Skip Rain World and Rain Meadow code if the game mode is Hide N Seek.
            // Determine if the ascension should tag someone.
            
            cursor.Emit(OpCodes.Ldarg, 0);
            cursor.Emit(OpCodes.Ldloc, locIndex18);
            cursor.EmitDelegate(
                (Player saintPlayer, PhysicalObject otherPO) =>
                {
                    if (HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek) &&
                        otherPO is Player otherPlayer)
                    {
                        OnlineCreature saintOCreature = saintPlayer.abstractCreature.GetOnlineCreature()!;
                        OnlineCreature otherOCreature = otherPlayer.abstractCreature.GetOnlineCreature()!;
                        
                        if (hideAndSeek.LobbyData.EnabledTaggingMethods.HasFlag(TaggingMethods.Contact) &&
                            saintOCreature.isMine &&
                            saintOCreature.owner.IsSeeker &&
                            !otherOCreature.owner.IsSeeker)
                        {
                            Logger.Debug($"{saintOCreature.owner} hit {otherOCreature.owner} with ascension!");
                            hideAndSeek.MakeSeeker(otherOCreature.owner);
                        }
                        
                        return true;
                    }
                    
                    return false;
                }
            );
            
            cursor.Emit(OpCodes.Brtrue, skip);
        }
        catch (Exception exception)
        {
            Logger.Fatal(exception);
        }
    }
    
    private static void On_RainWorldGame_GrafUpdate(
        On.RainWorldGame.orig_GrafUpdate orig,
        RainWorldGame self,
        float timeStacker)
    {
        if (HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            if (Input.GetKeyDown(KeyCode.PageDown))
                hideAndSeek.MakeSeeker(OnlineManager.mePlayer!);
            if (Input.GetKeyDown(KeyCode.End))
                hideAndSeek.RemoveSeeker(OnlineManager.mePlayer!);
        }
        
        orig(self, timeStacker);
    }
    
    private static bool On_Rock_HitSomething(
        On.Rock.orig_HitSomething orig,
        Rock self,
        SharedPhysics.CollisionResult result,
        bool eu)
    {
        bool hasHit = orig(self, result, eu);
        
        if (hasHit &&
            HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek) &&
            hideAndSeek.LobbyData.EnabledTaggingMethods.HasFlag(TaggingMethods.Rock) &&
            self.thrownBy is Player throwerPlayer &&
            result.obj is Player hitPlayer)
        {
            OnlineCreature throwerOCreature = throwerPlayer.abstractCreature.GetOnlineCreature()!; // TODO: Possible NRE?
            OnlineCreature hitOCreature = hitPlayer.abstractCreature.GetOnlineCreature()!;         // TODO: Possible NRE?
            
            if (throwerOCreature.isMine &&
                throwerOCreature.owner.IsSeeker &&
                !hitOCreature.owner.IsSeeker)
            {
                Logger.Debug($"{throwerOCreature.owner} hit {hitOCreature.owner} with a rock!");
                hideAndSeek.MakeSeeker(hitOCreature.owner);
            }
        }
        
        return hasHit;
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
            hideAndSeek.LobbyData.EnabledTaggingMethods.HasFlag(TaggingMethods.Contact) &&
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