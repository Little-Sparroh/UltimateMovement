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
    }

    // Dictionary mapping gun types to their mod data (data-driven, easy to extend)
    // Made public static to allow access from Patches class
    public static readonly Dictionary<GunType, GunModData> GunMods = new()
    {
        { GunType.Cycler, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1 } },
        { GunType.Jackrabbit, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1 } },
        { GunType.SwarmLauncher, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1 } },
        { GunType.DMLR, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1 } },
        { GunType.PlateLauncher, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1 } },
        { GunType.LeadFlinger, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1 } },
        { GunType.Gunship, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1 } },
        { GunType.Trident, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1 } },
        { GunType.Globbler, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1 } },
        { GunType.Carver, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1 } },
        { GunType.Shocklance, new GunModData { CanFireWhileSprinting = 1, CanFireWhileSliding = 1 } }
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

        // Update patch to restore animations
        MethodInfo updateMethod = AccessTools.Method(typeof(Gun), "Update");
        if (updateMethod == null)
        {
            Logger.LogError("Could not find Gun.Update method!");
            return;
        }
        HarmonyMethod updatePostfix = new HarmonyMethod(typeof(Patches), nameof(Patches.UpdatePostfix));
        harmony.Patch(updateMethod, postfix: updatePostfix);

        // OnFiredBullet patch for piercing
        MethodInfo firedBulletMethod = AccessTools.Method(typeof(Gun), "OnFiredBullet");
        if (firedBulletMethod == null)
        {
            Logger.LogError("Could not find Gun.OnFiredBullet method!");
            return;
        }
        HarmonyMethod postfixFired = new HarmonyMethod(typeof(Patches), nameof(Patches.AddPiercingMultiHits));
        harmony.Patch(firedBulletMethod, postfix: postfixFired);

        Logger.LogInfo($"{harmony.Id} loaded!");
    }
}

static class Patches
{
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

        // Single merged method call for all base stat modifications
        ModifyGunBaseStats(prefab, gunType, log);
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
    private static void ModifyGunBaseStats(IGear prefab, FreeFireMod.GunType gunType, ManualLogSource log)
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

        log.LogInfo($"Completed base stat modifications for {gunName}");
    }

    public static void UpdatePostfix(Gun __instance)
    {
        ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource("FreeFireMod");

        FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
        FieldInfo animatorField = AccessTools.Field(typeof(Gun), "animator");

        Player player = (Player)playerField.GetValue(__instance);
        PlayerAnimation animator = (PlayerAnimation)animatorField.GetValue(__instance);

        PropertyInfo isSprintingProp = AccessTools.Property(typeof(Player), "IsSprinting");
        bool isSprinting = (bool)isSprintingProp.GetValue(player);

        PropertyInfo slidingProp = AccessTools.Property(typeof(Player), "Sliding");
        bool isSliding = (bool)slidingProp.GetValue(player);

        FieldInfo runningField = AccessTools.Field(typeof(PlayerAnimation), "Running");
        if (isSprinting && (int)runningField.GetValue(animator) != 2)
        {
            runningField.SetValue(animator, 2);
            log.LogInfo("Restored sprint animation");
        }

        FieldInfo slidingAnimField = AccessTools.Field(typeof(PlayerAnimation), "Sliding");
        if (isSliding && !(bool)slidingAnimField.GetValue(animator))
        {
            slidingAnimField.SetValue(animator, true);
            log.LogInfo("Restored slide animation");
        }
    }

    // Your existing AddPiercingMultiHits method (unchanged)
    public static void AddPiercingMultiHits(Gun __instance /* Add other params as needed */)
    {
        // Implementation here (you didn't provide it, but assuming it's there)
    }
}