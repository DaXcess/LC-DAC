using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace DAC.Modules;

/// <summary>
/// This module prevents players from ending the game under certain circumstances
/// </summary>
[HarmonyPatch]
internal static class EndGameEarlyHack
{
    private static bool _calledByHandler;
    private static int _actualPlayerWhoTriggered;

    /// <summary>
    /// Store real player id
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.__rpc_handler_2028434619))]
    [HarmonyPrefix]
    private static void OnBeforeEndGame(ref __RpcParams rpcParams)
    {
        _calledByHandler = true;
        _actualPlayerWhoTriggered = (int)rpcParams.Server.Receive.SenderClientId;
    }
    
    /// <summary>
    /// Prevent ending the game under invalid circumstances
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndGameServerRpc))]
    [HarmonyPrefix]
    private static bool OnEndGame(int playerClientId)
    {
        if (!_calledByHandler)
            _actualPlayerWhoTriggered = 0;
        
        if (_actualPlayerWhoTriggered != playerClientId)
        {
            Logger.LogWarning(
                $"Player {StartOfRound.Instance.allPlayerScripts[_actualPlayerWhoTriggered].playerUsername} tried to leave the ship under an impersonated identity");
            return false;
        }

        var player = StartOfRound.Instance.allPlayerScripts[playerClientId];

        if (player.isPlayerDead)
        {
            Logger.LogWarning($"Player {player.playerUsername} tried to end the game whilst being dead");
            return false;
        }

        var lever = Object.FindObjectOfType<StartMatchLever>();

        if (Vector3.Distance(player.transform.position, lever.transform.position) > 5f)
        { 
            Logger.LogWarning($"Player {player.playerUsername} tried to end the game whilst being too far away from the lever");
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndGameServerRpc))]
    [HarmonyPostfix]
    private static void AfterEndGame()
    {
        _calledByHandler = false;
    }
}