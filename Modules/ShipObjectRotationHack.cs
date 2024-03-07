using HarmonyLib;
using UnityEngine;

namespace DAC.Modules;

/// <summary>
/// This module prevents malicious players from applying arbitrary and senseless rotations to placed ship objects
/// </summary>
[HarmonyPatch]
internal static class ShipObjectRotationHack
{
    // TODO: Implement
    [HarmonyPatch(typeof(ShipBuildModeManager), nameof(ShipBuildModeManager.PlaceShipObjectServerRpc))]
    [HarmonyPostfix]
    private static void OnPlaceObject(Vector3 placementRotation)
    {
        Logger.LogDebug($"Rotation: {placementRotation}");
    }
}