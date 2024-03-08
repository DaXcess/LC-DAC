using System.Linq;
using HarmonyLib;
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
    private static int _playerUsingTerminal;

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.SetTerminalInUseServerRpc))]
    [HarmonyPostfix]
    private static void OnSetTerminalInUse(Terminal __instance, bool inUse)
    {
        _playerUsingTerminal = inUse ? (int)__instance.ExecutingPlayer().playerClientId : -1;
    }

    /// <summary>
    /// Prevent spoofing price of items
    /// </summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.BuyItemsServerRpc))]
    [HarmonyPrefix]
    private static bool OnBoughtItems(Terminal __instance, int[] boughtItems, ref int newGroupCredits)
    {
        var playerWhoTriggered = __instance.ExecutingPlayer();

        if ((int)playerWhoTriggered.playerClientId != _playerUsingTerminal)
        {
            Logger.LogWarning(
                $"Player {playerWhoTriggered.playerUsername} attempted to buy items whilst not interacting with the terminal");
            return false;
        }

        var currentCredits = __instance.groupCredits;
        var totalCostOfItems = boughtItems.Sum(item =>
            (int)((float)__instance.buyableItemsList[item].creditsWorth * __instance.itemSalesPercentages[item] /
                  100f));

        if (currentCredits - totalCostOfItems != newGroupCredits)
        {
            Logger.LogWarning(
                $"Player {playerWhoTriggered.playerUsername} attempted to buy items for an invalid price (Expected: {currentCredits - totalCostOfItems}, Got: {newGroupCredits})");
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
        var playerWhoTriggered = __instance.ExecutingPlayer();

        if ((int)playerWhoTriggered.playerClientId != _playerUsingTerminal)
        {
            Logger.LogWarning(
                $"Player {playerWhoTriggered.playerUsername} attempted to buy unlockable whilst not interacting with the terminal");
            return false;
        }

        var terminal = Object.FindObjectOfType<Terminal>();
        var realPrice = __instance.unlockablesList.unlockables[unlockableID].shopSelectionNode.itemCost;

        if (terminal.groupCredits - realPrice != newGroupCreditsAmount)
        {
            Logger.LogWarning(
                $"Player {playerWhoTriggered.playerUsername} attempted to buy ship unlockable for an invalid price (Expected: {terminal.groupCredits - realPrice}, Got: {newGroupCreditsAmount})");
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
        var playerWhoTriggered = __instance.ExecutingPlayer();

        if ((int)playerWhoTriggered.playerClientId != _playerUsingTerminal)
        {
            Logger.LogWarning(
                $"Player {playerWhoTriggered.playerUsername} attempted to reroute moons whilst not interacting with the terminal");
            return false;
        }

        var terminal = Object.FindObjectOfType<Terminal>();
        
        // What the fuck
        var realPrice = terminal.terminalNodes.allKeywords.SelectMany(kw => kw.compatibleNouns)
            .Where(noun => noun.result.buyRerouteToMoon == -2).SelectMany(noun => noun.result.terminalOptions)
            .Select(opt => opt.result)
            .First(opt => opt.buyRerouteToMoon == levelID).itemCost;
        
        if (terminal.groupCredits - realPrice != newGroupCreditsAmount)
        {
            Logger.LogWarning(
                $"Player {playerWhoTriggered.playerUsername} attempted to reroute ship for an invalid price (Expected: {terminal.groupCredits - realPrice}, Got: {newGroupCreditsAmount})");
        }

        if (terminal.groupCredits - realPrice < 0)
            return false;

        newGroupCreditsAmount = terminal.groupCredits - realPrice;

        return true;
    }
}