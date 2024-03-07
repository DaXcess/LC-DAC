using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace DAC.Modules;

/// <summary>
/// This module prevents players from dealing friendly fire damage in situations where it shouldn't be possible
///
/// This module expects vanilla behavior, and may result in false positive with modded items
/// </summary>
[HarmonyPatch]
internal static class PlayerFriendlyFireHack
{
    private static bool _calledByHandler;
    private static int _actualPlayerWhoHit;

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.__rpc_handler_638895557))]
    [HarmonyPrefix]
    private static void OnBeforeDamageFromOtherPlayer(ref __RpcParams rpcParams)
    {
        _calledByHandler = true;
        _actualPlayerWhoHit = (int)rpcParams.Server.Receive.SenderClientId;
    }
    
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayerFromOtherClientServerRpc))]
    [HarmonyPrefix]
    private static bool OnDamageFromOtherPlayer(PlayerControllerB __instance, ref int damageAmount, int playerWhoHit)
    {
        if (!_calledByHandler)
            _actualPlayerWhoHit = 0;
        
        if (_actualPlayerWhoHit != playerWhoHit)
        {
            Logger.LogWarning(
                $"Player {StartOfRound.Instance.allPlayerScripts[_actualPlayerWhoHit]} tried to spoof dealing damage to another player");

            // Just block this damage outright instead of rectifying the player who hit
            return false;
        }

        var player = StartOfRound.Instance.allPlayerScripts[playerWhoHit];

        // Don't allow dead players to damage other players
        if (player.isPlayerDead)
        {
            Logger.LogWarning(
                $"Player {player.playerUsername} tried to damage {__instance.playerUsername} whilst being dead");
            return false;
        }

        // Don't allow players holding anything other than a Shovel or Shotgun to damage other players
        // This might prevent certain mods that add items to damage other players with from working
        if (player.currentlyHeldObjectServer is not Shovel && player.currentlyHeldObjectServer is not ShotgunItem)
        {
            Logger.LogWarning(
                $"Player {player.playerUsername} tried to damage {__instance.playerUsername} whilst not holding a valid item");
            return false;
        }

        if (player.currentlyHeldObjectServer is Shovel shovel)
        {
            // Shovel wielding players cannot damage other players when too far away
            if (Vector3.Distance(__instance.playerCollider.ClosestPoint(player.transform.position),
                    player.transform.position) > 2.5f)
            {
                Logger.LogWarning(
                    $"Player {player.playerUsername} tried to damage {__instance.playerUsername} with a shovel from too far away");
                return false;
            }

            var expectedDamageAmount = shovel.shovelHitForce switch
            {
                <= 2 => 10,
                <= 4 => 30,
                _ => 100,
            };

            // Double check damage amount
            if (damageAmount != expectedDamageAmount)
            {
                Logger.LogWarning(
                    $"Player {player.playerUsername} tried to damage {__instance.playerUsername} for an incorrect amount of damage");
                Logger.LogWarning($"Expected: {expectedDamageAmount}, Got: {damageAmount}");
            }

            damageAmount = expectedDamageAmount;
        }

        if (player.currentlyHeldObjectServer is ShotgunItem shotgun)
        {
            // Recalculate distance and double check damage amount
            var shotgunPosition = shotgun.transform.position;

            var distance = Vector3.Distance(__instance.playerCollider.ClosestPoint(shotgunPosition),
                shotgunPosition);

            var expectedDamageAmount = distance switch
            {
                < 3.7f => 100,
                < 6f => 30,
                < 15f => 10,
                _ => 0
            };

            if (expectedDamageAmount != damageAmount)
            {
                Logger.LogWarning(
                    $"Player {player.playerUsername} tried to damage {__instance.playerUsername} for an incorrect amount of damage");
                Logger.LogWarning($"Expected: {expectedDamageAmount}, Got: {damageAmount}");
            }

            if (expectedDamageAmount == 0)
                return false;

            damageAmount = expectedDamageAmount;
        }

        return true;
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayerFromOtherClientServerRpc))]
    [HarmonyPostfix]
    private static void AfterDamagePlayerFromOtherClient()
    {
        _calledByHandler = false;
    }
}