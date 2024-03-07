using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DAC.Modules;

/// <summary>
/// Prevents players that have a masked item in their inventory to be able to spawn mimics on other players
/// </summary>
[HarmonyPatch]
internal static class SpawnMimicHack
{
    private static Dictionary<HauntedMaskItem, Vector3> maskPlayerPositions = [];

    /// <summary>
    /// Start storing the position of the player holding the mask
    /// </summary>
    [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.EquipItem))]
    [HarmonyPostfix]
    private static void OnEquipMaskItem(HauntedMaskItem __instance)
    {
        if (maskPlayerPositions.ContainsKey(__instance))
            maskPlayerPositions.Remove(__instance);
        
        maskPlayerPositions.Add(__instance, __instance.previousPlayerHeldBy.transform.position);
    }
    
    /// <summary>
    /// Update the position of the player holding the mask
    /// </summary>
    [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.Update))]
    [HarmonyPostfix]
    private static void OnUpdate(HauntedMaskItem __instance)
    {
        if (__instance.previousPlayerHeldBy != null)
        {
            maskPlayerPositions[__instance] = __instance.previousPlayerHeldBy.transform.position;
        }
    }
    
    /// <summary>
    /// Prevent spawning a mimic if the mask isn't attached or if the mask should have been destroyed
    /// </summary>
    [HarmonyPatch(typeof(HauntedMaskItem), nameof(HauntedMaskItem.CreateMimicServerRpc))]
    [HarmonyPrefix]
    private static bool OnCreateMimic(HauntedMaskItem __instance, ref Vector3 playerPositionAtDeath)
    {
        if (!__instance.attaching || !maskPlayerPositions.ContainsKey(__instance))
            return false;

        playerPositionAtDeath = maskPlayerPositions[__instance];

        maskPlayerPositions.Remove(__instance);

        return true;
    }
}