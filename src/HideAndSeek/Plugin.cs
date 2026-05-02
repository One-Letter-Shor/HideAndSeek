using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using OneLetterShor.HideAndSeek.Compat;
using OneLetterShor.HideAndSeek.Hooking;
using Logger_ = OneLetterShor.HideAndSeek.Logging.Logger;
using SecurityAction = System.Security.Permissions.SecurityAction;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
[assembly: AssemblyVersion(Plugin.Version)]

namespace OneLetterShor.HideAndSeek;

[BepInDependency(ExtraInfo.RainMeadowGuidBepInEx)]
[BepInPlugin(Guid, Name, Version)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string
        Guid = "OneLetterShor.HideAndSeek",
        Name = "Hide N Seek",
        Version = "0.0.0";
    public static Plugin Instance { get; private set; } = null!;
    public static Cfg.Options Options { get; private set; } = null!;
    public static ModManager.Mod Mod { get; private set; } = null!;
    internal static ManualLogSource __Logger = null!;
    
    /// <remarks>Primarily for assertions.</remarks>
    public static bool IsInitialized { get; private set; } = false;
    
    public void ApplyHooksAndEvents()
    {
        RainMeadowHooks.Apply();
    }
    
    private Plugin()
    {
        Assert(!IsInitialized);
        Instance = this;
        __Logger = Logger;
    }
    
    private void OnEnable()
    {
        Assert(!IsInitialized);
        Options = new Cfg.Options();
        On.RainWorld.OnModsInit += On_RainWorld_OnModsInit;
    }
    
    private void On_RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        if (IsInitialized) { orig(self); return; }
        
        try
        {
            Mod = ModManager.ActiveMods.Find(mod => mod.id == Guid);
            DependencyState.CheckMods();
            MachineConnector.SetRegisteredOI(Mod.id, Options);
            ApplyHooksAndEvents();
            
            Assert(Mod.id == Guid);
            Assert(Mod.name == Name);
            Assert(Mod.version == Version);
        }
        catch (Exception exception)
        {
            Logger_.Fatal(exception);
        }
        
        IsInitialized = true;
        
        orig(self);
    }
}