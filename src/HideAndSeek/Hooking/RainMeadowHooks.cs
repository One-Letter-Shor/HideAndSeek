using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using OneLetterShor.HideAndSeek.Arena;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;
using RainMeadow.UI;

namespace OneLetterShor.HideAndSeek.Hooking;

public static class RainMeadowHooks
{
    internal static void Apply()
    {
        _ = new Hook(
            typeof(ArenaOnlineGameMode).GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                [ typeof(Lobby) ],
                null
            ),
            On_RainMeadow_ArenaOnlineGameMode_ctor
        );
        
        _ = new Hook(
            typeof(ArenaOnlineGameMode).GetMethod(
                nameof(ArenaOnlineGameMode.ResourceAvailable),
                BindingFlags.Public | BindingFlags.Instance
            ),
            On_RainMeadow_ArenaOnlineGameMode_ResourceAvailable
        );
        
        _ = new Hook(
            typeof(ArenaOnlineGameMode).GetMethod(
                nameof(ArenaOnlineGameMode.AddClientData),
                BindingFlags.Public | BindingFlags.Instance
            ),
            On_RainMeadow_ArenaOnlineGameMode_AddClientData
        );
        
        _ = new Hook(
            typeof(ArenaOnlineLobbyMenu).GetMethod(
                nameof(ArenaOnlineLobbyMenu.StartGame),
                BindingFlags.Public | BindingFlags.Instance
            ),
            On_RainMeadow_ArenaOnlineLobbyMenu_StartGame
        );
        
        // _ = new ILHook( // TODO: Is this hook needed?
        //     typeof(GameplayExtensions).GetMethod(
        //         nameof(GameplayExtensions.FriendlyFireSafetyCandidate),
        //         BindingFlags.Public | BindingFlags.Static
        //     ),
        //     IL_GameplayExtensions_FriendlyFireSafetyCandidate
        // );
        
        _ = new ILHook(
            typeof(OnlinePlayerDisplay).GetMethod(
                nameof(OnlinePlayerDisplay.Update),
                BindingFlags.Public | BindingFlags.Instance
            ),
            IL_RainMeadow_OnlinePlayerDisplay_Update
        );
        
        _ = new Hook(
            typeof(RainMeadow.RainMeadow).GetMethod(
                "Weapon_HitThisObject",
                BindingFlags.NonPublic | BindingFlags.Instance
            ),
            On_RainMeadow_RainMeadow_Weapon_HitThisObject
        );
    }
    
    // Game mode
    private static void On_RainMeadow_ArenaOnlineGameMode_ctor(
        Action<ArenaOnlineGameMode, Lobby> orig,
        ArenaOnlineGameMode self,
        Lobby lobby)
    {
        orig(self, lobby);
        self.AddExternalGameModes(HideAndSeekMode.Id, new HideAndSeekMode());
    }
    
    // Lobby data
    private static void On_RainMeadow_ArenaOnlineGameMode_ResourceAvailable(
        Action<ArenaOnlineGameMode, OnlineResource> orig,
        ArenaOnlineGameMode self,
        OnlineResource onlineResource)
    {
        orig(self, onlineResource);
        
        if (onlineResource is Lobby lobby && lobby.isOwner)
            lobby.AddData(new HideAndSeekLobbyData());
    }
    
    // Client data
    private static void On_RainMeadow_ArenaOnlineGameMode_AddClientData(
        Action<ArenaOnlineGameMode> orig,
        ArenaOnlineGameMode self)
    {
        orig(self);
        self.clientSettings.AddData(new HideAndSeekClientData { IsMine = true });
    }
    
    // Choose random seekers when the start button is pressed.
    private static void On_RainMeadow_ArenaOnlineLobbyMenu_StartGame(
        Action<ArenaOnlineLobbyMenu> orig,
        ArenaOnlineLobbyMenu self)
    {
        AssertIs(OnlineManager.lobby!.gameMode, out ArenaOnlineGameMode arenaOnline);
        
        if (HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            if (OnlineManager.lobby.isOwner && arenaOnline.lobbyCountDown > 0)
                hideAndSeek.ChooseRandomSeekers();
        }
        
        orig(self);
    }
    
    // // Add seeker to seeker friendly fire protection.
    // private static void IL_GameplayExtensions_FriendlyFireSafetyCandidate(ILContext il)
    // {
    //     /*
    //      (OnlineUIComponents/GameplayOverrides.cs:22 - last updated: 6/1/26)
    //      
    //      
    //      Current code:
    //      ...
    //      if (RainMeadow.isArenaMode(out var arena))
    //      {
    //          if (
    //              creature.room.game.IsArenaSession
    //              && creature
    //                  .room
    //                  .game
    //                  .GetArenaGameSession
    //                  .arenaSitting
    //                  .gameTypeSetup
    //                  .spearsHitPlayers == false)
    //      ...
    //      
    //      Desired code:
    //      ...
    //      if (RainMeadow.isArenaMode(out var arena))
    //      {
    //          if (HideAndSeekMode.IsHideAndSeekMode(out _))
    //          {
    //              if (friend is null) return true;
    //              
    //              OnlineCreature selfOCreature = self.abstractCreature.GetOnlineCreature()!;
    //              OnlineCreature friendOCreature = friend?.abstractCreature.GetOnlineCreature()!;
    //              
    //              return selfOCreature.owner.IsSeeker && friendOCreature.owner.IsSeeker;
    //          }
    //          
    //          if (
    //              creature.room.game.IsArenaSession
    //              && creature
    //                  .room
    //                  .game
    //                  .GetArenaGameSession
    //                  .arenaSitting
    //                  .gameTypeSetup
    //                  .spearsHitPlayers == false)
    //      ...
    //     */
    //     
    //     try
    //     {
    //         
    //         ILCursor cursor = new(il);
    //         ILLabel skip = il.DefineLabel();
    //         
    //         const int 
    //             selfArgIndex = 0,
    //             friendArgIndex = 1;
    //         
    //         const int
    //             locIndex6 = 6; // Compiler generated, used to store and load the value from isArenaMode().
    //         
    //         cursor.GotoNext(
    //             MoveType.After,
    //             i => i.MatchCall<RainMeadow.RainMeadow>(nameof(RainMeadow.RainMeadow.isArenaMode)),
    //             i => i.MatchStloc(locIndex6),
    //             i => i.MatchLdloc(locIndex6),
    //             i => i.MatchBrfalse(out _)
    //         );
    //         
    //         // TODO: Maybe just always return true when it's Hide And Seek mode.
    //         
    //         cursor.EmitDelegate(() => HideAndSeekMode.IsHideAndSeekMode(out _));
    //         cursor.Emit(OpCodes.Brfalse, skip);
    //         
    //         cursor.Emit(OpCodes.Ldarg, friendArgIndex);
    //         cursor.Emit(OpCodes.Ldarg, selfArgIndex);
    //         cursor.EmitDelegate(
    //             (Creature self, Creature? friend) =>
    //             {
    //                 if (friend is null) return true;
    //                 
    //                 OnlineCreature selfOCreature = self.abstractCreature.GetOnlineCreature()!;
    //                 OnlineCreature friendOCreature = friend.abstractCreature.GetOnlineCreature()!;
    //                 
    //                 return selfOCreature.owner.IsSeeker && friendOCreature.owner.IsSeeker;
    //             }
    //         );
    //         
    //         cursor.Emit(OpCodes.Ret);
    //         cursor.MarkLabel(skip);
    //     }
    //     catch (Exception exception)
    //     {
    //         Logger.Fatal(exception);
    //     }
    // }
    
    // Prevent seekers from seeing hider nametags.
    private static void IL_RainMeadow_OnlinePlayerDisplay_Update(ILContext il)
    {
        /*
         (OnlineUIComponents/OnlinePlayerDisplay.cs:180 - last updated: 6/7/26)
         
         current code:
         ...
         bool show = RainMeadow.rainMeadowOptions.ShowFriends.Value || (owner.clientSettings.isMine && onlineTimeSinceSpawn < 120);
         if (RainMeadow.isArenaMode(out var a) && owner.RealizedPlayer?.isCamo == true)
         {
             bool isTeammate = TeamBattleMode.isTeamBattleMode(a, out _) && ArenaHelpers.CheckSameTeam(OnlineManager.mePlayer, player);

             if (!player.isMe && !isTeammate)
             {
                 show = false;
                 pos.x = -1000;
                 this.alpha = 0f;
             }
         }
         
         if (show || this.alpha > 0 || flashIcons)
         {
             ...
         }
         ...
         
         desired code:
         ...
         bool show = RainMeadow.rainMeadowOptions.ShowFriends.Value || (owner.clientSettings.isMine && onlineTimeSinceSpawn < 120);
         
         if (HideAndSeekMode.IsHideAndSeekMode(out _))
         {
             if (OnlineManager.mePlayer.IsSeeker && !player.IsSeeker)
             {
                 self.alpha = 0f;
                 show = false;
             }
         }
         
         if (RainMeadow.isArenaMode(out var a) && owner.RealizedPlayer?.isCamo == true)
         {
             bool isTeammate = TeamBattleMode.isTeamBattleMode(a, out _) && ArenaHelpers.CheckSameTeam(OnlineManager.mePlayer, player);

             if (!player.isMe && !isTeammate)
             {
                 show = false;
                 pos.x = -1000;
                 this.alpha = 0f;
             }
         }
         
         if (show || this.alpha > 0 || flashIcons)
         {
             ...
         }
         ...
        */
        
        try
        {
            ILCursor cursor = new(il);
            
            const int showLocIndex = 0; // Source code variable written and read from multiple times to check if the OnlinePlayerDisplay should be visible.
            
            cursor.GotoNext(
                MoveType.After,
                i => i.MatchStloc(showLocIndex)
            );
            
            cursor.Emit(OpCodes.Ldarg, 0);
            cursor.Emit(OpCodes.Ldloc, showLocIndex);
            cursor.EmitDelegate(
                (OnlinePlayerDisplay self, bool show) =>
                {
                    if (HideAndSeekMode.IsHideAndSeekMode(out _))
                    {
                        if (OnlineManager.mePlayer.IsSeeker && !self.player.IsSeeker)
                        {
                            self.alpha = 0f;
                            show = false;
                        }
                    }
                    
                    return show;
                }
            );
            cursor.Emit(OpCodes.Stloc, showLocIndex);
        }
        catch (Exception exception)
        {
            Logger.Fatal(exception);
        }
    }
    
    // Rain Meadow's hook protects players from dangerous weapons when piggybacking and in team mode.
    // This overrides their hook when the mode is Hide and Seek to protect players from weapons.
    private static bool On_RainMeadow_RainMeadow_Weapon_HitThisObject(
        Func<RainMeadow.RainMeadow, On.Weapon.orig_HitThisObject, Weapon, PhysicalObject, bool> orig,
        RainMeadow.RainMeadow self,
        On.Weapon.orig_HitThisObject hitThisObjectOrig,
        Weapon weapon,
        PhysicalObject hitPO)
    {
        if (HideAndSeekMode.IsHideAndSeekMode(out _) &&
            weapon.thrownBy is Player throwerPlayer &&
            hitPO is Player hitPlayer)
        {
            // Some weapons should non-conditionally phase.
            if (weapon is Spear ||
                ModManager.DLCShared && weapon is LillyPuck
            )
                return false;
            
            // Others should only phase if on different 'teams'.
            OnlineCreature throwerOCreature = throwerPlayer.abstractCreature.GetOnlineCreature()!;
            OnlineCreature hitOCreature = hitPlayer.abstractCreature.GetOnlineCreature()!;
            
            if (throwerOCreature.owner.IsSeeker == hitOCreature.owner.IsSeeker)
                return false;
        }
        
        return orig(self, hitThisObjectOrig, weapon, hitPO);
    }
}