using HarmonyLib;
using UnityEngine;

namespace DAC.Modules;

/// <summary>
/// This module prevents players from ending the game under certain circumstances
/// </summary>
[HarmonyPatch]
internal static class EndGameEarlyHack
{
    /// <summary>
    /// Prevent ending the game under invalid circumstances
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndGameServerRpc))]
    [HarmonyPrefix]
    private static bool OnEndGame(StartOfRound __instance, int playerClientId)
    {
        var actualPlayerWhoTriggered = __instance.ExecutingPlayer();

        if ((int)actualPlayerWhoTriggered.playerClientId != playerClientId)
        {
            if (actualPlayerWhoTriggered.ReportHack(Detection.EndGame,
                    $"Player {actualPlayerWhoTriggered.playerUsername} tried to end the game whilst impersonating another player"))
                return false;
        }

        var player = StartOfRound.Instance.allPlayerScripts[playerClientId];

        if (player.isPlayerDead)
        {
            if (player.ReportHack(Detection.EndGame,
                    $"Player {player.playerUsername} tried to end the game whilst being dead"))
                return false;
        }

        var lever = Object.FindObjectOfType<StartMatchLever>();

        if (Vector3.Distance(player.transform.position, lever.transform.position) > 5f)
        {
            if (player.ReportHack(Detection.EndGame,
                    $"Player {player.playerUsername} tried to end the game whilst being too far away from the lever"))
                return false;
        }

        return true;
    }
}