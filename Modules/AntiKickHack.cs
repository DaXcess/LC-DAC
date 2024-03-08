using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;

namespace DAC.Modules;

/// <summary>
/// This module prevents users from hiding their identity which makes them unable to be kicked
/// </summary>
[HarmonyPatch]
internal static class AntiKickHack
{
    private static readonly HashSet<ulong> activePlayers = [];

    /// <summary>
    /// Detect Steam ID spoofing and register player when sending values
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SendNewPlayerValuesServerRpc))]
    [HarmonyPrefix]
    private static bool OnSendNewPlayerValues(PlayerControllerB __instance, ref ulong newPlayerSteamId)
    {
        var executingPlayer = __instance.ExecutingPlayer();
        var actualSteamId = DACManager.GetSteamId(executingPlayer.actualClientId);

        if (actualSteamId != newPlayerSteamId)
        {
            if (__instance.ReportHack(Detection.AntiKick,
                    $"Player {executingPlayer.playerUsername} tried to spoof their Steam ID (Expected: {actualSteamId}, Got: {newPlayerSteamId})"))
                return false;

            newPlayerSteamId = actualSteamId;
        }

        activePlayers.Add(executingPlayer.actualClientId);

        return true;
    }

    /// <summary>
    /// Use the `UpdatePlayerPositionServerRpc` to detect whether or not a player has synchronized their Steam ID
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.UpdatePlayerPositionServerRpc))]
    [HarmonyPrefix]
    private static bool OnUpdatePlayerPosition(PlayerControllerB __instance)
    {
        var executingPlayer = __instance.ExecutingPlayer();
        if (activePlayers.Contains(executingPlayer.actualClientId)) return true;

        return !__instance.ReportHack(Detection.AntiKick,
            $"Player {executingPlayer.playerUsername} is performing actions before synchronizing their Steam ID");
    }
}