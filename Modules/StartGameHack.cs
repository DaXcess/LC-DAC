using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace DAC.Modules;

/// <summary>
/// Prevent players from starting the ship under invalid circumstances
/// </summary>
[HarmonyPatch]
internal static class StartGameHack
{
    private static bool _initialHostStartedShip;
    
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGameServerRpc))]
    [HarmonyPrefix]
    private static bool OnStartGame(StartOfRound __instance)
    {
        var player = __instance.ExecutingPlayer();
        
        if (!_initialHostStartedShip && player.playerClientId != 0)
        {
            Logger.LogWarning($"Player {player.playerUsername} tried to start the ship even though only the host may start the ship");
            return false;
        }

        var lever = Object.FindObjectOfType<StartMatchLever>();

        if (Vector3.Distance(lever.transform.position, player.transform.position) > 5f)
        {
            Logger.LogWarning($"Player {player.playerUsername} tried to start the ship from too far away");
            return false;
        }

        _initialHostStartedShip = true;

        return true;
    }
}