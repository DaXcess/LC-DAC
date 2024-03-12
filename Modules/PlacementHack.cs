using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace DAC.Modules;

/// <summary>
/// This module prevents items from being discarded far away from the player
/// </summary>
[HarmonyPatch]
internal static class PlacementHack
{
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ThrowObjectServerRpc))]
    [HarmonyPrefix]
    private static bool OnThrowObject(PlayerControllerB __instance, Vector3 targetFloorPosition)
    {
        var player = __instance.ExecutingPlayer();

        if (!(Vector3.Distance(player.transform.position, targetFloorPosition) > 3f)) return true;
        
        return !__instance.ReportHack(Detection.Placement,
            $"Player {player.playerUsername} tried to throw an object to a location too far away");
    }
}