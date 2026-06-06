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
            typeof(OnlineHUD).GetMethod(
                nameof(OnlineHUD.Draw),
                BindingFlags.Public | BindingFlags.Instance
            ),
            IL_RainMeadow_OnlineHUD_Draw
        );
        
        _ = new Hook(
            typeof(RainMeadow.RainMeadow).GetMethod(
                "Weapon_HitThisObject",
                BindingFlags.NonPublic | BindingFlags.Instance
            ),
            On_RainMeadow_RainMeadow_Weapon_HitThisObject
        );
    }
    
    private static void On_RainMeadow_ArenaOnlineGameMode_ctor(
        Action<ArenaOnlineGameMode, Lobby> orig,
        ArenaOnlineGameMode self,
        Lobby lobby)
    {
        orig(self, lobby);
        self.AddExternalGameModes(HideAndSeekMode.Id, new HideAndSeekMode());
    }
    
    private static void On_RainMeadow_ArenaOnlineGameMode_ResourceAvailable(
        Action<ArenaOnlineGameMode, OnlineResource> orig,
        ArenaOnlineGameMode self,
        OnlineResource onlineResource)
    {
        orig(self, onlineResource);
        
        if (onlineResource is Lobby lobby && lobby.isOwner)
            lobby.AddData(new HideAndSeekLobbyData());
    }
    
    private static void On_RainMeadow_ArenaOnlineGameMode_AddClientData(
        Action<ArenaOnlineGameMode> orig,
        ArenaOnlineGameMode self)
    {
        orig(self);
        self.clientSettings.AddData(new HideAndSeekClientData { IsMine = true });
    }
    
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
    
    private static void IL_RainMeadow_OnlineHUD_Draw(ILContext il)
    {
        /*
         (OnlineUIComponents/OnlineHud.cs:28 - last updated: 6/4/26)
         
         Current code: 
         ...
         if (!RainMeadow.rainMeadowOptions.FriendViewClickToActivate.Value)
             RainMeadow.rainMeadowOptions.ShowFriends.Value = Input.GetKey(RainMeadow.rainMeadowOptions.FriendsListKey.Value);
         else if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.FriendsListKey.Value))
             RainMeadow.rainMeadowOptions.ShowFriends.Value ^= true;
         ...
         
         Desired code:
         ...
         if (!HideAndSeekMode.IsHideAndSeekMode(out _) || OnlineManager.mePlayer.CanEnableNametags)
         {
             if (!RainMeadow.rainMeadowOptions.FriendViewClickToActivate.Value)
                 RainMeadow.rainMeadowOptions.ShowFriends.Value = Input.GetKey(RainMeadow.rainMeadowOptions.FriendsListKey.Value);
             else if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.FriendsListKey.Value))
                 RainMeadow.rainMeadowOptions.ShowFriends.Value ^= true;
         }
         ...
        */
        
        try
        {
            ILCursor cursor = new(il);
            ILLabel skip = cursor.DefineLabel();
            
            // Emit Hide and Seek's check.
            cursor.EmitDelegate(
                () => !HideAndSeekMode.IsHideAndSeekMode(out _) ||
                      OnlineManager.mePlayer.CanEnableNametags
            );
            cursor.Emit(OpCodes.Brfalse, skip);
            
            // Go past the if body, then past the else if body.
            cursor.GotoNext(
                MoveType.After,
                i => i.MatchCallvirt(typeof(Configurable<bool>).GetProperty(nameof(Configurable<>.Value))!.GetSetMethod())
            );
            cursor.GotoNext(
                MoveType.After,
                i => i.MatchCallvirt(typeof(Configurable<bool>).GetProperty(nameof(Configurable<>.Value))!.GetSetMethod()),
                i => i.MatchNop()
            );
            
            cursor.MarkLabel(skip);
        }
        catch (Exception exception)
        {
            Logger.Fatal(exception);
        }
    }
    
    // Rain Meadow's hook protects players from dangerous weapons when piggybacking and in team mode.
    // This overrides their hook to protect all players when the weapon is dangerous, and it is Hide and Seek mode. 
    private static bool On_RainMeadow_RainMeadow_Weapon_HitThisObject(
        Func<RainMeadow.RainMeadow, On.Weapon.orig_HitThisObject, Weapon, PhysicalObject, bool> orig,
        RainMeadow.RainMeadow self,
        On.Weapon.orig_HitThisObject hitThisObjectOrig,
        Weapon weapon,
        PhysicalObject physicalObject)
    {
        if (HideAndSeekMode.IsHideAndSeekMode(out _) &&
            physicalObject is Player &&
            WeaponIsDangerous(weapon)
        )
            return false;
        
        return orig(self, hitThisObjectOrig, weapon, physicalObject);
        
        static bool WeaponIsDangerous(Weapon weapon) // Rain Meadow's method for this is private for whatever reason.
        {
            return weapon is Spear ||
                   ModManager.DLCShared && weapon is LillyPuck;
        }
    }
}