using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using OneLetterShor.HideAndSeek.Arena;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;

namespace OneLetterShor.HideAndSeek.Hooking;

public static class RainWorldHooks
{
    internal static void Apply()
    {
        On.ProcessManager.Update += On_ProcessManager_Update;
        On.Player.Collide += On_Player_Collide;
        On.Rock.HitSomething += On_Rock_HitSomething;
        On.PlayerGraphics.DrawSprites += On_PlayerGraphics_DrawSprites;
        
        IL.Player.ClassMechanicsSaint += IL_Player_ClassMechanicsSaint;
    }
    
    private static bool _isToggleSeekerPressed { get; set; }
    private static bool _previousIsToggleSeekerPressed { get; set; }
    private static bool _isToggleInitialSeekerPressed { get; set; }
    private static bool _previousIsToggleInitialSeekerPressed { get; set; }
    private static void On_ProcessManager_Update(
        On.ProcessManager.orig_Update orig,
        ProcessManager self,
        float time)
    {
        _previousIsToggleSeekerPressed        = _isToggleSeekerPressed;
        _previousIsToggleInitialSeekerPressed = _isToggleInitialSeekerPressed;
        _isToggleSeekerPressed        = Input.GetKey(KeyCode.PageDown);
        _isToggleInitialSeekerPressed = Input.GetKey(KeyCode.End);
        
        if (HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek) &&
            hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled)
        {
            if (_isToggleSeekerPressed && !_previousIsToggleSeekerPressed)
                hideAndSeek.DebugToggleSeeker(OnlineManager.mePlayer!);
            if (_isToggleInitialSeekerPressed && !_previousIsToggleInitialSeekerPressed)
                hideAndSeek.DebugToggleInitialSeeker(OnlineManager.mePlayer!);
        }
        
        orig(self, time);
    }
    
    // Tag players when colliding.
    private static void On_Player_Collide(
        On.Player.orig_Collide orig,
        Player self,
        PhysicalObject otherObject,
        int chunkIndex,
        int otherChunkIndex)
    {
        // TODO: Handle devtool teleporting.
        
        if (HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek) &&
            hideAndSeek.LobbyData.EnabledTaggingMethods.HasFlag(TaggingMethods.Contact) &&
            otherObject is Player otherPlayer)
        {
            OnlineCreature? selfOCreature  = self.abstractCreature.GetOnlineCreature();
            OnlineCreature? otherOCreature = otherPlayer.abstractCreature.GetOnlineCreature();
            
            Assert(selfOCreature is not null);
            Assert(otherOCreature is not null);
            
            if (selfOCreature.isMine &&
                selfOCreature.CanTag(otherOCreature))
            {
                Logger.Debug($"I tagged {otherOCreature.owner} by contact!");
                hideAndSeek.TagPlayer(otherOCreature.owner);
            }
        }
        
        orig(self, otherObject, chunkIndex, otherChunkIndex);
    }
    
    // Tag players when hit by a rock and protect against friendly fire.
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
            OnlineCreature? throwerOCreature = throwerPlayer.abstractCreature.GetOnlineCreature();
            OnlineCreature? hitOCreature = hitPlayer.abstractCreature.GetOnlineCreature();
            
            Assert(throwerOCreature is not null);
            Assert(hitOCreature is not null);
            
            if (throwerOCreature.isMine &&
                throwerOCreature.CanTag(hitOCreature))
            {
                Logger.Debug($"I tagged {hitOCreature.owner} with a rock!");
                hideAndSeek.TagPlayer(hitOCreature.owner);
            }
            
            if (throwerOCreature.owner.IsSeeker == hitOCreature.owner.IsSeeker)
                return false;
        }
        
        return hasHit;
    }
    
    // Implement custom seeker color that doesn't save and also can update on any frame (not just from palette updates).
    private static void On_PlayerGraphics_DrawSprites(
        On.PlayerGraphics.orig_DrawSprites orig,
        PlayerGraphics self,
        RoomCamera.SpriteLeaser sLeaser,
        RoomCamera roomCamera,
        float timeStacker,
        Vector2 camPos)
    {
        orig(self, sLeaser, roomCamera, timeStacker, camPos);
        
        // TODO: Check out what Meadow Customizations does. This seems overcomplicated.
        if (HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            RainMeadow.RainMeadow.creatureCustomizations.TryGetValue(self.player, out AvatarData creatureCustomization);
            
            if (creatureCustomization is SlugcatCustomization slugcatCustomization)
            {
                OnlinePlayer? oPlayer = self.player.abstractCreature.GetOnlineCreature()?.owner; 
                
                if (oPlayer is null) // Null when the session ends/transitions.
                    return;
                
                bool isArtificer   = ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer;
                bool isRivulet     = ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Rivulet;
                bool isSpearmaster = ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Spear;
                bool isSaint       = ModManager.MSC && self.player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint;
                
                // Body and eye color
                Color bodyColor = oPlayer.IsSeeker
                    ? hideAndSeek.LobbyData.SeekerBodyColor
                    : slugcatCustomization.bodyColor;
                
                Color eyeColor = oPlayer.IsSeeker
                    ? hideAndSeek.LobbyData.SeekerEyeColor
                    : slugcatCustomization.eyeColor;
                
                int[] bodySpriteIndexes = [ 0, 1, 2, 3, 4, 5, 6, 7, 8 ];
                int[] eyeSpriteIndexes  = [ 9 ];
                
                // Saint's ascension dots are supposed to be the primary color.
                if (isSaint)
                    bodySpriteIndexes = [ ..bodySpriteIndexes, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26 ]; 
                
                foreach (int i in bodySpriteIndexes)
                    sLeaser.sprites[i].color = bodyColor;
                
                // During saint's ascension, his eyes and X sprite change color rapidly regardless of custom colors.
                if (!(isSaint && self.player.monkAscension)) 
                    foreach (int i in eyeSpriteIndexes)
                        sLeaser.sprites[i].color = eyeColor;
                
                
                // Tertiary color
                if (slugcatCustomization.currentColors.Count > 3)
                    Logger.Warning($"Color count ({slugcatCustomization.currentColors.Count}) is above 3. Continuing while pretending it is 3. slugcat: {self.player.SlugCatClass}");
                
                bool hasTertiaryColor = slugcatCustomization.currentColors.Count >= 3;
                if (hasTertiaryColor)
                {
                    if (!ModManager.MSC)
                    {
                        Logger.Warning($"A tertiary color exists when MSC is disabled. (Could this be caused by another mod?) slugcat: {self.player.SlugCatClass}");
                        return;
                    }
                    
                    Color tertiaryColor = oPlayer.IsSeeker
                        ? hideAndSeek.LobbyData.SeekerTertiaryColor
                        : slugcatCustomization.currentColors[2];
                    
                    int[] tertiarySpriteIndexes;
                    
                    if (isArtificer)
                        tertiarySpriteIndexes = [ 12 ];
                    else if (isRivulet)
                    {
                        // Rivulet is different, there are sprites used to form a gradient between the body and gills.
                        int[] gradientGillSpriteIndexes = [ 12, 13, 14, 15, 16, 17 ];
                        foreach (int i in gradientGillSpriteIndexes)
                            sLeaser.sprites[i].color = bodyColor;
                        
                        tertiarySpriteIndexes = [ 18, 19, 20, 21, 22, 23 ];             // Gills
                    }
                    else if (isSpearmaster)
                        tertiarySpriteIndexes = [ 13, 14, 16, 19, 20, 22, 25, 26, 27 ]; // Spear dots
                    else if (isSaint)
                        tertiarySpriteIndexes = [ 12 ];                                 // Tongue
                    else
                    {
                        Logger.Warning($"Unknown slugcat with a tertiary color. (Could this be caused by another mod?) slugcat: {self.player.SlugCatClass}");
                        return;
                    }
                    
                    foreach (int i in tertiarySpriteIndexes)
                        sLeaser.sprites[i].color = tertiaryColor;
                }
                    
                /*
                 main body parts: 1-8
                 eyes: 9
                 
                 artificer scar: 12
                 
                 rivulet "inner" gills: 12-17
                 rivulet "outer" gills: 18-23
                 
                 spearmaster tail spots: 13, 14, 16, 19, 20, 22, 25, 26
                 spearmaster spear: 27
                 
                 saint tongue: 12
                 saint ascension x: 14
                 saint ascension dots: 15-26 (note: reverse order of how they decay)
                */
            }
            else
                Logger.Warning($"Could not find slugcat customization for {self.player} ({self.player.abstractCreature.GetOnlineCreature()})");
        }
    }
    
    
    // Tag players when ascended.
    private static void IL_Player_ClassMechanicsSaint(ILContext il)
    {
        /*
         (last updated: 6/19/26)
         Note: Rain Meadow hooks the same part of this method. (Arena/ArenaHooks.cs:1633)
         
         (excluding Rain Meadow's emitted instructions)
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
         
         (excluding Rain Meadow's emitted instructions)
         Desired code:
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
                         OnlineCreature saintOCreature = saintPlayer.abstractCreature.GetOnlineCreature();
                         OnlineCreature otherOCreature = otherPlayer.abstractCreature.GetOnlineCreature();
                         
                         Assert(saintOCreature is not null);
                         Assert(otherOCreature is not null);
                         
                         if (hideAndSeek.LobbyData.EnabledTaggingMethods.HasFlag(TaggingMethods.Ascension) &&
                             saintOCreature.isMine &&
                             saintOCreature.CanTag(otherOCreature)
                         )
                             hideAndSeek.AddSeeker(otherOCreature.owner);
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
            
            const int locIndex18 = 18; // No source code name provided. Physical object that is used to kill if ascended.
            
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
                        OnlineCreature? saintOCreature = saintPlayer.abstractCreature.GetOnlineCreature();
                        OnlineCreature? otherOCreature = otherPlayer.abstractCreature.GetOnlineCreature();
                        
                        Assert(saintOCreature is not null);
                        Assert(otherOCreature is not null);
                        
                        if (hideAndSeek.LobbyData.EnabledTaggingMethods.HasFlag(TaggingMethods.Ascension) &&
                            saintOCreature.isMine &&
                            saintOCreature.CanTag(otherOCreature))
                        {
                            Logger.Debug($"I tagged {otherOCreature.owner} with ascension!");
                            hideAndSeek.TagPlayer(otherOCreature.owner);
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
}