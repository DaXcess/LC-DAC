using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace DAC.Modules;

/// <summary>
/// This module prevents players from buying items, ship unlockables or moon reroutes for a price that doesn't match
/// the host's price
///
/// Also includes preventing players from executing certain terminal commands without being in the terminal
/// </summary>
[HarmonyPatch]
internal static class TerminalCreditsHack
{
    private static bool _calledByHandler;
    private static int _playerWhoTriggered;

    private static int _playerUsingTerminal;
    
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.__rpc_handler_4047492032))]
    [HarmonyPrefix]
    private static void OnBeforeSetTerminalInUse(ref __RpcParams rpcParams)
    {
        _calledByHandler = true;
        _playerWhoTriggered = (int)rpcParams.Server.Receive.SenderClientId;
    }
    
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.__rpc_handler_4003509079))]
    [HarmonyPrefix]
    private static void OnBeforeBuyItems(ref __RpcParams rpcParams)
    {
        _calledByHandler = true;
        _playerWhoTriggered = (int)rpcParams.Server.Receive.SenderClientId;
    }
    
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.__rpc_handler_3953483456))]
    [HarmonyPrefix]
    private static void OnBeforeBuyShipUnlockable(ref __RpcParams rpcParams)
    {
        _calledByHandler = true;
        _playerWhoTriggered = (int)rpcParams.Server.Receive.SenderClientId;
    }
    
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.__rpc_handler_1134466287))]
    [HarmonyPrefix]
    private static void OnBeforeChangeLevel(ref __RpcParams rpcParams)
    {
        _calledByHandler = true;
        _playerWhoTriggered = (int)rpcParams.Server.Receive.SenderClientId;
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.SetTerminalInUseServerRpc))]
    [HarmonyPostfix]
    private static void OnSetTerminalInUse(bool inUse)
    {
        if (!_calledByHandler)
            _playerWhoTriggered = 0;
        
        _playerUsingTerminal = inUse ? _playerWhoTriggered : -1;
    }
    
    /// <summary>
    /// Prevent spoofing price of items
    /// </summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.BuyItemsServerRpc))]
    [HarmonyPrefix]
    private static bool OnBoughtItems(Terminal __instance, int[] boughtItems, ref int newGroupCredits)
    {
        if (!_calledByHandler)
            _playerWhoTriggered = 0;

        if (_playerWhoTriggered != _playerUsingTerminal)
        {
            Logger.LogWarning($"Player attempted to buy items whilst not interacting with the terminal");
            return false;
        }
        
        var currentCredits = __instance.groupCredits;
        var totalCostOfItems = boughtItems.Sum(item =>
            (int)((float)__instance.buyableItemsList[item].creditsWorth * __instance.itemSalesPercentages[item] /
                  100f));

        if (currentCredits - totalCostOfItems != newGroupCredits)
        {
            Logger.LogWarning(
                $"Player attempted to buy items for an invalid price (Expected: {currentCredits - totalCostOfItems}, Got: {newGroupCredits})");
        }

        if (currentCredits - totalCostOfItems < 0)
            return false;

        newGroupCredits = currentCredits - totalCostOfItems;

        return true;
    }

    /// <summary>
    /// Prevent spoofing price of ship unlockables
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.BuyShipUnlockableServerRpc))]
    [HarmonyPrefix]
    private static bool OnBoughtUnlockable(StartOfRound __instance, int unlockableID, ref int newGroupCreditsAmount)
    {
        if (!_calledByHandler)
            _playerWhoTriggered = 0;
        
        if (_playerWhoTriggered != _playerUsingTerminal)
        {
            Logger.LogWarning($"Player attempted to buy unlockable whilst not interacting with the terminal");
            return false;
        }
        
        var terminal = Object.FindObjectOfType<Terminal>();
        var realPrice = __instance.unlockablesList.unlockables[unlockableID].shopSelectionNode.itemCost;

        if (terminal.groupCredits - realPrice != newGroupCreditsAmount)
        {
            Logger.LogWarning(
                $"Player attempted to buy ship unlockable for an invalid price (Expected: {terminal.groupCredits - realPrice}, Got: {newGroupCreditsAmount})");
        }

        if (terminal.groupCredits - realPrice < 0)
            return false;

        newGroupCreditsAmount = terminal.groupCredits - realPrice;
        
        return true;
    }

    /// <summary>
    /// Prevent spoofing price of moons
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ChangeLevelServerRpc))]
    [HarmonyPrefix]
    private static bool OnChangeLevel(StartOfRound __instance, int levelID, ref int newGroupCreditsAmount)
    {
        if (!_calledByHandler)
            _playerWhoTriggered = 0;
        
        if (_playerWhoTriggered != _playerUsingTerminal)
        {
            Logger.LogWarning($"Player attempted to reroute moons whilst not interacting with the terminal");
            return false;
        }
        
        var terminal = Object.FindObjectOfType<Terminal>();
        var realPrice = terminal.terminalNodes.terminalNodes.First(node => node.buyRerouteToMoon == levelID).itemCost;

        if (terminal.groupCredits - realPrice != newGroupCreditsAmount)
        {
            Logger.LogWarning(
                $"Player attempted to reroute ship for an invalid price (Expected: {terminal.groupCredits - realPrice}, Got: {newGroupCreditsAmount})");
        }

        if (terminal.groupCredits - realPrice < 0)
            return false;

        newGroupCreditsAmount = terminal.groupCredits - realPrice;

        return true;
    }
}