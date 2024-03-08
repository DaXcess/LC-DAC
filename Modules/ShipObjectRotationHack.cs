using HarmonyLib;
using UnityEngine;

namespace DAC.Modules;

/// <summary>
/// This module prevents malicious players from applying arbitrary and senseless rotations to placed ship objects
/// </summary>
[HarmonyPatch]
internal static class ShipObjectRotationHack
{
    [HarmonyPatch(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.PlaceShipObjectServerRpc))]
    [HarmonyPrefix]
    private static bool OnPlaceObject(ShipBuildModeManager __instance, ref Vector3 newRotation)
    {
        if (Mathf.Abs(newRotation.x - 270) > 0.1f || Mathf.Abs(newRotation.z) > 0.1f)
        {
            Logger.LogWarning(
                $"Player {__instance.ExecutingPlayer().playerUsername} tried setting an invalid rotation on a ship object");
            return false;
        }

        return true;
    }
}