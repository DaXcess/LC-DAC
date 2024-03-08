using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DAC.Modules;

/// <summary>
/// This module prevents malicious players from applying arbitrary and senseless rotations to placed ship objects, or
/// placing objects too quickly
/// </summary>
[HarmonyPatch]
internal static class ShipObjectHack
{
    private static readonly Dictionary<ulong, float> playerObjectPlacementTime = [];

    [HarmonyPatch(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.PlaceShipObjectServerRpc))]
    [HarmonyPrefix]
    private static bool OnPlaceObject(ShipBuildModeManager __instance, ref Vector3 newRotation)
    {
        var executingPlayer = __instance.ExecutingPlayer();

        if (playerObjectPlacementTime.TryGetValue(executingPlayer.actualClientId, out var time) &&
            Time.realtimeSinceStartup - time < 0.25f)
        {
            if (executingPlayer.ReportHack(Detection.ShipObjectRatelimit,
                    $"Player {executingPlayer.playerUsername} is trying to place ship objects too quickly"))
                return false;
        }

        if (Mathf.Abs(newRotation.x - 270) > 0.1f || Mathf.Abs(newRotation.z) > 0.1f)
        {
            if (executingPlayer.ReportHack(Detection.ShipObjectRotation,
                    $"Player {__instance.ExecutingPlayer().playerUsername} tried setting an invalid rotation on a ship object"))
                return false;
        }

        playerObjectPlacementTime.Add(executingPlayer.actualClientId, Time.realtimeSinceStartup);

        return true;
    }
}