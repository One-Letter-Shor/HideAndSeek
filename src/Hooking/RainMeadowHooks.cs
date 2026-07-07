using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using OneLetterShor.HideAndSeek.Arena;
using OneLetterShor.HideAndSeek.Utils;
using RainMeadow;
using RainMeadow.UI;
using RainMeadow.UI.Pages;

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
        
        _ = new Hook(
            typeof(RainMeadow.RainMeadow).GetMethod(
                "Weapon_HitThisObject",
                BindingFlags.NonPublic | BindingFlags.Instance
            ),
            On_RainMeadow_RainMeadow_Weapon_HitThisObject
        );
        
        _ = new Hook(
            typeof(ArenaMainLobbyPage).GetMethod(
                nameof(ArenaMainLobbyPage.UpdateMatchButtons),
                BindingFlags.Public | BindingFlags.Instance
            ),
            On_RainMeadow_UI_Pages_ArenaMainLobbyPage_UpdateMatchButtons
        );
        
        _ = new ILHook(
            typeof(OnlinePlayerDisplay).GetMethod(
                nameof(OnlinePlayerDisplay.Update),
                BindingFlags.Public | BindingFlags.Instance
            ),
            IL_RainMeadow_OnlinePlayerDisplay_Update
        );
        
        _ = new ILHook(
            typeof(ChatLogOverlay).GetMethod(
                nameof(ChatLogOverlay.OpacityUpdate),
                BindingFlags.Public | BindingFlags.Instance
            ),
            IL_RainMeadow_ChatLogOverlay_OpacityUpdate
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
    
    // Select random seekers when the start button is pressed.
    private static void On_RainMeadow_ArenaOnlineLobbyMenu_StartGame(
        Action<ArenaOnlineLobbyMenu> orig,
        ArenaOnlineLobbyMenu self)
    {
        AssertIs(OnlineManager.lobby!.gameMode, out ArenaOnlineGameMode arenaOnline);
        
        if (HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek))
        {
            if (OnlineManager.lobby.isOwner &&
                arenaOnline.lobbyCountDown > 0 &&
                hideAndSeek.LobbyData.EnabledSeekerSelection == SeekerSelection.Random)
            {
                if (!hideAndSeek.CanStartNewGame(out string? failureReason) &&
                    !hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled)
                {
                    Logger.Error($"It should not be possible to start the game. Reason: {failureReason}");
                }
                
                hideAndSeek.SelectRandomSeekers();
            }
        }
        
        orig(self);
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
                ModManager.DLCShared && weapon is LillyPuck)
            {
                return false;
            }
            
            // Others should only phase if on different 'teams'.
            OnlineCreature? throwerOCreature = throwerPlayer.abstractCreature.GetOnlineCreature();
            OnlineCreature? hitOCreature = hitPlayer.abstractCreature.GetOnlineCreature();
            
            Assert(throwerOCreature is not null);
            Assert(hitOCreature is not null);
            
            if (throwerOCreature.isAvatar && hitOCreature.isAvatar &&
                throwerOCreature.owner.IsSeeker == hitOCreature.owner.IsSeeker)
            {
                return false;
            }
        }
        
        return orig(self, hitThisObjectOrig, weapon, hitPO);
    }
    
    // Ensure Hide and Seek can be started before allowing host to press start game.
    private static void On_RainMeadow_UI_Pages_ArenaMainLobbyPage_UpdateMatchButtons(
        Action<ArenaMainLobbyPage> orig,
        ArenaMainLobbyPage self)
    {
        orig(self);
        
        if (HideAndSeekMode.IsHideAndSeekMode(out HideAndSeekMode? hideAndSeek) &&
            (!hideAndSeek.CanStartNewGame() && !hideAndSeek.LobbyData.AreSeekerDebugToolsEnabled))
        {
            self.startButton?.buttonBehav.greyedOut = true;
        }
    }
    
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
         
         desired code:
         ...
         bool show = RainMeadow.rainMeadowOptions.ShowFriends.Value || (owner.clientSettings.isMine && onlineTimeSinceSpawn < 120);
         
         if (HideAndSeekMode.IsHideAndSeekMode(out _))
         {
             if (OnlineManager.mePlayer.IsSeeker && !player.IsSeeker)
             {
                 show = false;
                 pos.x = -1000;
                 this.alpha = 0f;
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
                            show = false;
                            self.pos.x = -1000;
                            self.alpha = 0f;
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
    
    // Prevent hiders from making seekers' chat fade.
    private static void IL_RainMeadow_ChatLogOverlay_OpacityUpdate(ILContext il)
    {
        /*
         (OnlineUIComponents/ChatLogOverlay.cs:148 - last updated: 6/22/26)
        
         current code:
         ...
         bool fade = false;
         
         if (inactivityTimer > RainMeadow.rainMeadowOptions.ChatInactivityTimer.Value * 40)
         {
             fade = true;
         }
         else
         {
             foreach (var avatar in OnlineManager.lobby.playerAvatars)
             {
                 var entity = avatar.Value.FindEntity(true);
                 if (entity is OnlineCreature oc && oc.abstractCreature != null && oc.abstractCreature.realizedCreature != null && !oc.abstractCreature.realizedCreature.dead)
                 {
                     if (chatRect.Contains(oc.abstractCreature.realizedCreature.mainBodyChunk.pos - chatHud.camera.pos))
                     {
                         // A player avatar is currently being obscured by chat.
                         fade = true;
                         break;
                     }
                 }
             }
         }
         ...
        
         desired code:
         ...
         bool fade = false;
           
         if (inactivityTimer > RainMeadow.rainMeadowOptions.ChatInactivityTimer.Value * 40)
         {
             fade = true;
         }
         else
         {
             foreach (var avatar in OnlineManager.lobby.playerAvatars)
             {
                 var entity = avatar.Value.FindEntity(true);
                 
                 if (entity is OnlineCreature oc && oc.abstractCreature != null && oc.abstractCreature.realizedCreature != null && !oc.abstractCreature.realizedCreature.dead)
                 {
                     if (HideAndSeekMode.IsHideAndSeekMode(out _) && OnlineManager.mePlayer.IsSeeker && !oc.owner.IsSeeker)
                         continue;
                     
                     if (chatRect.Contains(oc.abstractCreature.realizedCreature.mainBodyChunk.pos - chatHud.camera.pos))
                     {
                         // A player avatar is currently being obscured by chat.
                         fade = true;
                         break;
                     }
                 }
             }
         }
         ...
        */
        
        try
        {
            ILCursor cursor = new(il);
            ILLabel? continueLoop = null;
                
            const int ocLocIndex = 7; // Source code variable used in the foreach of player avatars to check the owner.
            
            cursor.GotoNext(
                MoveType.After,
                i => i.MatchCallvirt<Creature>("get_" + nameof(Creature.dead)) // dead is the last check in the if statement. 
            );
            cursor.GotoNext(
                MoveType.After,
                i => i.MatchBrfalse(out continueLoop) // This brfalse is equivalent to the continue keyword.
            );
            Assert(continueLoop is not null);
            
            cursor.Emit(OpCodes.Ldloc, ocLocIndex);
            cursor.EmitDelegate((OnlineCreature oc) => HideAndSeekMode.IsHideAndSeekMode(out _) && OnlineManager.mePlayer!.IsSeeker && !oc.owner.IsSeeker);
            cursor.Emit(OpCodes.Brtrue, continueLoop);
        }
        catch (Exception exception)
        {
            Logger.Fatal(exception);
        }
    }
}