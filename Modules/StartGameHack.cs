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

    private static bool _calledByHandler;
    private static int _playerWhoTriggered;
    
    /// <summary>
    /// Store who triggered the action
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.__rpc_handler_1089447320))]
    [HarmonyPrefix]
    private static void OnBeforeStartGame(ref __RpcParams rpcParams)
    {
        _playerWhoTriggered = (int)rpcParams.Server.Receive.SenderClientId;
        _calledByHandler = true;
    }
    
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGameServerRpc))]
    [HarmonyPrefix]
    private static bool OnStartGame(StartOfRound __instance)
    {
        if (!_calledByHandler)
            _playerWhoTriggered = 0;
        
        var player = StartOfRound.Instance.allPlayerScripts[_playerWhoTriggered];
        
        if (!_initialHostStartedShip && _playerWhoTriggered != 0)
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

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGameServerRpc))]
    [HarmonyPostfix]
    private static void AfterStartGame()
    {
        _calledByHandler = false;
    }
}