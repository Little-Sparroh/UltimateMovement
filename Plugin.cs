using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Pigeon.Movement;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[BepInPlugin("yourname.mycopunk.firewhilesprinting", "FireWhileSprinting", "1.0.0")]
[MycoMod(null, ModFlags.IsClientSide)]
public class FreeFireMod : BaseUnityPlugin
{
    private Harmony harmony;

    // Enum for gun types to avoid string checks in switch
    public enum GunType
    {
        Unknown,
        Cycler,
        Jackrabbit,
        SwarmLauncher,
        DMLR,
        PlateLauncher,
        LeadFlinger,
        Gunship,
        Trident,
        Globbler,
        Carver,
        Shocklance
        // Add more as needed
    }

    // Data structure for all modifiable stats per gun type
    public struct GunModData
    {
        public int CanFireWhileSprinting { get; set; }
        public int CanFireWhileSliding { get; set; }
        public int CanAimWhileSliding { get; set; }
        public int CanAimWhileReloading { get; set; }
        public int CanAimWhileSprinting { get; set; } // New field for the added functionality
    }

    // Dictionary mapping gun types to their mod data (data-driven, easy to extend)
    // Made public static to allow access from Patches class
    public static readonly Dictionary<GunType, GunModData> GunMods = new()
    {
        { GunType.Cycler, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1, CanAimWhileSliding = 1, CanAimWhileReloading = 1, CanAimWhileSprinting = 1 } },
        { GunType.Jackrabbit, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1, CanAimWhileSliding = 1, CanAimWhileReloading = 1, CanAimWhileSprinting = 1 } },
        { GunType.SwarmLauncher, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1, CanAimWhileSliding = 1, CanAimWhileReloading = 1, CanAimWhileSprinting = 1 } },
        { GunType.DMLR, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1, CanAimWhileSliding = 1, CanAimWhileReloading = 1, CanAimWhileSprinting = 1 } },
        { GunType.PlateLauncher, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1, CanAimWhileSliding = 1, CanAimWhileReloading = 1, CanAimWhileSprinting = 1 } },
        { GunType.LeadFlinger, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1, CanAimWhileSliding = 1, CanAimWhileReloading = 1, CanAimWhileSprinting = 1 } },
        { GunType.Gunship, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1, CanAimWhileSliding = 1, CanAimWhileReloading = 1, CanAimWhileSprinting = 1 } },
        { GunType.Trident, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1, CanAimWhileSliding = 1, CanAimWhileReloading = 1, CanAimWhileSprinting = 1 } },
        { GunType.Globbler, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1, CanAimWhileSliding = 1, CanAimWhileReloading = 1, CanAimWhileSprinting = 1 } },
        { GunType.Carver, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1, CanAimWhileSliding = 1, CanAimWhileReloading = 1, CanAimWhileSprinting = 1 } },
        { GunType.Shocklance, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1, CanAimWhileSliding = 1, CanAimWhileReloading = 1, CanAimWhileSprinting = 1 } }
        // Extend as needed for other guns
    };

    private void Awake()
    {
        var harmony = new Harmony("yourname.mycopunk.firewhilesprinting");

        // Setup patch
        MethodInfo setupMethod = AccessTools.Method(typeof(Gun), "Setup", new Type[] { typeof(Player), typeof(PlayerAnimation), typeof(IGear) });
        if (setupMethod == null)
        {
            Logger.LogError("Could not find Gun.Setup method!");
            return;
        }
        HarmonyMethod prefix = new HarmonyMethod(typeof(Patches), nameof(Patches.ModifyWeaponPrefix));
        harmony.Patch(setupMethod, prefix: prefix);

        // Patch OnStartAim to handle sprint resume
        MethodInfo onStartAimMethod = AccessTools.Method(typeof(Gun), "OnStartAim");
        if (onStartAimMethod == null)
        {
            Logger.LogError("Could not find Gun.OnStartAim method!");
            return;
        }
        HarmonyMethod onStartAimPrefix = new HarmonyMethod(typeof(Patches), nameof(Patches.OnStartAimPrefix));
        HarmonyMethod onStartAimPostfix = new HarmonyMethod(typeof(Patches), nameof(Patches.OnStartAimPostfix));
        harmony.Patch(onStartAimMethod, prefix: onStartAimPrefix, postfix: onStartAimPostfix);

        // Patch Gun.CanAim to skip sprint check
        MethodInfo canAimMethod = AccessTools.Method(typeof(Gun), "CanAim");
        if (canAimMethod == null)
        {
            Logger.LogError("Could not find Gun.CanAim method!");
            return;
        }
        HarmonyMethod canAimPrefix = new HarmonyMethod(typeof(Patches), nameof(Patches.CanAimPrefix));
        harmony.Patch(canAimMethod, prefix: canAimPrefix);

        // Patch Gun.HandleFiring to allow tap firing while sprinting
        MethodInfo handleFiringMethod = AccessTools.Method(typeof(Gun), "HandleFiring");
        if (handleFiringMethod == null)
        {
            Logger.LogError("Could not find Gun.HandleFiring method!");
            return;
        }
        HarmonyMethod handleFiringPrefix = new HarmonyMethod(typeof(Patches), nameof(Patches.HandleFiringPrefix));
        harmony.Patch(handleFiringMethod, prefix: handleFiringPrefix);

        // New patch for forcing wallrun
        MethodInfo getterMethod = AccessTools.PropertyGetter(typeof(Player), "EnableWallrun");
        if (getterMethod == null)
        {
            //Logger.LogError("Could not find Player.EnableWallrun getter!");
            return;
        }
        HarmonyMethod wallrunPrefix = new HarmonyMethod(typeof(Patches), nameof(Patches.EnableWallrunGetPrefix));
        harmony.Patch(getterMethod, prefix: wallrunPrefix);

        Logger.LogInfo($"{harmony.Id} loaded!");
    }
}

public class ModGunData : MonoBehaviour
{
    public int CanAimWhileSprinting { get; set; }
    public bool WasSprinting { get; set; } // Temporary storage for sprint state
}

static class Patches
{
    private static readonly FieldInfo lockSprintingField = AccessTools.Field(typeof(Gun), "lockSprinting");

    public static void ModifyWeaponPrefix(Gun __instance, IGear prefab)
    {
        ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource("FreeFireMod");

        log.LogInfo($"Processing gun in Setup: {__instance.gameObject.name}");

        // Single type detection (still uses if-else, but only once)
        FreeFireMod.GunType gunType = DetermineGunType(__instance);
        if (gunType == FreeFireMod.GunType.Unknown)
        {
            log.LogInfo($"Skipped gun: Name = {__instance.gameObject.name}, Type = {__instance.GetType().Name}");
            return;
        }

        // Add custom component to the gun instance for extended data
        var modGunData = __instance.gameObject.AddComponent<ModGunData>();

        // Single merged method call for all base stat modifications
        ModifyGunBaseStats(prefab, gunType, log, modGunData);
    }

    // Helper to determine type (could be optimized further with a dictionary of name/type pairs if needed)
    private static FreeFireMod.GunType DetermineGunType(Gun gun)
    {
        string name = gun.gameObject.name.ToUpperInvariant();
        if (name.Contains("SMG") && gun is CartridgeSMG) return FreeFireMod.GunType.Cycler;
        if (name.Contains("BOUNCE SHOTGUN") && gun is BounceShotgun) return FreeFireMod.GunType.Jackrabbit;
        if (name.Contains("SWARM GUN") && gun is SwarmGun) return FreeFireMod.GunType.SwarmLauncher;
        if (name.Contains("SCOUT") && gun is ScoutLaserRifle) return FreeFireMod.GunType.DMLR;
        if (name.Contains("PLATE LAUNCHER") && gun is PlateLauncher) return FreeFireMod.GunType.PlateLauncher;
        if (name.Contains("FAST SHOTGUN") && gun is FastReloadShotgun) return FreeFireMod.GunType.LeadFlinger;
        if (name.Contains("MINI CANNON") && gun is MiniCannon) return FreeFireMod.GunType.Gunship;
        if (name.Contains("WIDE GUN") && gun is WideGun) return FreeFireMod.GunType.Trident;
        if (name.Contains("GLOBBLER") && gun is Globbler) return FreeFireMod.GunType.Globbler;
        if (name.Contains("THE CARVER") && gun is TheCarver) return FreeFireMod.GunType.Carver;
        if (name.Contains("SHOCKLANCE") && gun is Shocklance) return FreeFireMod.GunType.Shocklance;
        // Add more as needed
        return FreeFireMod.GunType.Unknown;
    }

    // Single merged method: Applies all base stat mods based on type
    // This replaces all the individual ModifyXXX methods, reducing method call overhead and code duplication
    private static void ModifyGunBaseStats(IGear prefab, FreeFireMod.GunType gunType, ManualLogSource log, ModGunData modGunData)
    {
        if (prefab == null || prefab is not Gun gunPrefab) return;

        ref var gunData = ref gunPrefab.GunData;
        string gunName = gunType.ToString(); // For logging

        // Lookup mod data (O(1) dictionary access)
        if (!FreeFireMod.GunMods.TryGetValue(gunType, out var mods))
        {
            log.LogWarning($"No mod data found for {gunName}!");
            return;
        }

        // Fire constraints modifications
        var originalSprint = gunData.fireConstraints.canFireWhileSprinting;
        gunData.fireConstraints.canFireWhileSprinting = (FireConstraints.ActionFireMode)mods.CanFireWhileSprinting;
        log.LogInfo($"Modified {gunName} canFireWhileSprinting: Original {originalSprint}, New {gunData.fireConstraints.canFireWhileSprinting}");

        var originalSlide = gunData.fireConstraints.canFireWhileSliding;
        gunData.fireConstraints.canFireWhileSliding = (FireConstraints.ActionFireMode)mods.CanFireWhileSliding;
        log.LogInfo($"Modified {gunName} canFireWhileSliding: Original {originalSlide}, New {gunData.fireConstraints.canFireWhileSliding}");

        var originalAimSlide = gunData.fireConstraints.canAimWhileSliding;
        gunData.fireConstraints.canAimWhileSliding = (FireConstraints.ActionFireMode)mods.CanAimWhileSliding;
        log.LogInfo($"Modified {gunName} canAimWhileSliding: Original {originalAimSlide}, New {gunData.fireConstraints.canAimWhileSliding}");

        var originalAimReload = gunData.fireConstraints.canAimWhileReloading;
        gunData.fireConstraints.canAimWhileReloading = mods.CanAimWhileReloading != 0;
        log.LogInfo($"Modified {gunName} canAimWhileReloading: Original {originalAimReload}, New {gunData.fireConstraints.canAimWhileReloading}");

        // Set custom CanAimWhileSprinting on the component
        modGunData.CanAimWhileSprinting = mods.CanAimWhileSprinting;
        log.LogInfo($"Modified {gunName} canAimWhileSprinting (custom): New {mods.CanAimWhileSprinting}");

        // Override lockSprinting to allow aiming while sprinting if enabled (reflection for private field)
        bool lockSprintingValue = (mods.CanAimWhileSprinting != 1); // false if allowing (assuming 1 == CanPerformDuring)
        lockSprintingField.SetValue(gunPrefab, lockSprintingValue);
        log.LogInfo($"Modified {gunName} lockSprinting: New {lockSprintingValue}");

        log.LogInfo($"Completed base stat modifications for {gunName}");
    }

    public static bool OnStartAimPrefix(Gun __instance)
    {
        var modGunData = __instance.gameObject.GetComponent<ModGunData>();
        if (modGunData != null && modGunData.CanAimWhileSprinting == 1)
        {
            FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
            Player player = (Player)playerField.GetValue(__instance);
            if (player != null)
            {
                PropertyInfo isSprintingProp = AccessTools.Property(typeof(Player), "IsSprinting");
                if (isSprintingProp != null)
                {
                    modGunData.WasSprinting = (bool)isSprintingProp.GetValue(player);
                }
            }
        }
        return true; // Run original
    }

    public static void OnStartAimPostfix(Gun __instance)
    {
        var modGunData = __instance.gameObject.GetComponent<ModGunData>();
        if (modGunData != null && modGunData.CanAimWhileSprinting == 1 && modGunData.WasSprinting)
        {
            FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
            Player player = (Player)playerField.GetValue(__instance);
            if (player != null)
            {
                MethodInfo resumeSprintMethod = AccessTools.Method(typeof(Player), "ResumeSprint");
                if (resumeSprintMethod != null)
                {
                    resumeSprintMethod.Invoke(player, null);
                }
            }
            modGunData.WasSprinting = false; // Reset
        }
    }

    public static bool CanAimPrefix(Gun __instance, ref bool __result)
    {
        var modGunData = __instance.gameObject.GetComponent<ModGunData>();
        if (modGunData != null && modGunData.CanAimWhileSprinting == 1)
        {
            FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
            Player player = (Player)playerField.GetValue(__instance);
            if (player != null)
            {
                PropertyInfo isSprintingProp = AccessTools.Property(typeof(Player), "IsSprinting");
                if (isSprintingProp != null && (bool)isSprintingProp.GetValue(player))
                {
                    FieldInfo isAimInputHeldField = AccessTools.Field(typeof(Gun), "isAimInputHeld");
                    bool isAimInputHeld = isAimInputHeldField != null ? (bool)isAimInputHeldField.GetValue(__instance) : false;
                    if (isAimInputHeld)
                    {
                        __result = true;
                        return false; // Skip original to avoid stop sprint
                    }
                }
            }
        }
        return true; // Run original
    }

    public static bool HandleFiringPrefix(Gun __instance)
    {
        var modGunData = __instance.gameObject.GetComponent<ModGunData>();
        if (modGunData != null && modGunData.CanAimWhileSprinting == 1)
        {
            FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
            Player player = (Player)playerField.GetValue(__instance);
            if (player != null)
            {
                PropertyInfo isSprintingProp = AccessTools.Property(typeof(Player), "IsSprinting");
                if (isSprintingProp != null && (bool)isSprintingProp.GetValue(player))
                {
                    if (PlayerInput.Controls.Player.Fire.WasPressedThisFrame())
                    {
                        MethodInfo fireMethod = AccessTools.Method(typeof(Gun), "Fire");
                        if (fireMethod != null)
                        {
                            fireMethod.Invoke(__instance, null);
                        }
                        return false; // Skip original to avoid delay
                    }
                }
            }
        }
        return true; // Run original
    }

    // New prefix for EnableWallrun getter
    public static bool EnableWallrunGetPrefix(Player __instance, ref bool __result)
    {
        if (!__instance.IsLocalPlayer) return true;

        __result = true;
        //log.LogInfo("Forced EnableWallrun to true for local player.");
        return false; // Skip original getter
    }
}