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
internal static class TerminalHack
{
    private static int playerUsingTerminal;
    
    /// <summary>
    /// Store who is using the terminal (and prevent users from kicking other users out the terminal)
    /// </summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.SetTerminalInUseServerRpc))]
    [HarmonyPrefix]
    private static bool OnSetTerminalInUse(Terminal __instance, bool inUse)
    {
        if (!inUse && playerUsingTerminal != -1 && playerUsingTerminal != (int)__instance.ExecutingPlayer().playerClientId)
            return false;
            
        playerUsingTerminal = inUse ? (int)__instance.ExecutingPlayer().playerClientId : -1;

        return true;
    }

    /// <summary>
    /// Reset terminal user if they disconnect while using the terminal
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC))]
    [HarmonyPostfix]
    private static void OnPlayerDisconnect(int playerObjectNumber)
    {
        if (playerUsingTerminal == playerObjectNumber)
            playerUsingTerminal = -1;
    }

    /// <summary>
    /// Prevent returning items from storage if not interacting with the terminal
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ReturnUnlockableFromStorageServerRpc))]
    [HarmonyPrefix]
    private static bool OnReturnUnlockableFromStorage(StartOfRound __instance)
    {
        return (int)__instance.ExecutingPlayer().playerClientId == playerUsingTerminal;
    }

    /// <summary>
    /// Prevent spoofing price of items
    /// </summary>
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.BuyItemsServerRpc))]
    [HarmonyPrefix]
    private static bool OnBoughtItems(Terminal __instance, int[] boughtItems, ref int newGroupCredits)
    {
        var playerWhoTriggered = __instance.ExecutingPlayer();

        if ((int)playerWhoTriggered.playerClientId != playerUsingTerminal)
        {
            if (playerWhoTriggered.ReportHack(Detection.Terminal,
                    $"Player {playerWhoTriggered.playerUsername} attempted to buy items whilst not interacting with the terminal"))
                return false;
        }

        var currentCredits = __instance.groupCredits;
        var totalCostOfItems = boughtItems.Sum(item =>
            (int)((float)__instance.buyableItemsList[item].creditsWorth * __instance.itemSalesPercentages[item] /
                  100f));

        if (currentCredits - totalCostOfItems != newGroupCredits)
        {
            if (playerWhoTriggered.ReportHack(Detection.TerminalPrice,
                    $"Player {playerWhoTriggered.playerUsername} attempted to buy items for an invalid price (Expected: {currentCredits - totalCostOfItems}, Got: {newGroupCredits})"))
                return false;
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

        if ((int)playerWhoTriggered.playerClientId != playerUsingTerminal)
        {
            if (playerWhoTriggered.ReportHack(Detection.Terminal,
                    $"Player {playerWhoTriggered.playerUsername} attempted to buy unlockable whilst not interacting with the terminal"))
                return false;
        }

        var terminal = Object.FindObjectOfType<Terminal>();
        var realPrice = __instance.unlockablesList.unlockables[unlockableID].shopSelectionNode.itemCost;

        if (terminal.groupCredits - realPrice != newGroupCreditsAmount)
        {
            if (playerWhoTriggered.ReportHack(Detection.TerminalPrice,
                    $"Player {playerWhoTriggered.playerUsername} attempted to buy ship unlockable for an invalid price (Expected: {terminal.groupCredits - realPrice}, Got: {newGroupCreditsAmount})"))
                return false;
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

        if ((int)playerWhoTriggered.playerClientId != playerUsingTerminal)
        {
            if (playerWhoTriggered.ReportHack(Detection.Terminal,
                    $"Player {playerWhoTriggered.playerUsername} attempted to reroute moons whilst not interacting with the terminal"))
                return false;
        }

        var terminal = Object.FindObjectOfType<Terminal>();
        
        // What the fuck
        // I have my doubts that this works on modded moons
        var realPrice = terminal.terminalNodes.allKeywords.SelectMany(kw => kw.compatibleNouns)
            .Where(noun => noun.result.buyRerouteToMoon == -2).SelectMany(noun => noun.result.terminalOptions)
            .Select(opt => opt.result)
            .First(opt => opt.buyRerouteToMoon == levelID).itemCost;

        if (terminal.groupCredits - realPrice != newGroupCreditsAmount)
        {
            if (playerWhoTriggered.ReportHack(Detection.TerminalPrice,
                    $"Player {playerWhoTriggered.playerUsername} attempted to reroute ship for an invalid price (Expected: {terminal.groupCredits - realPrice}, Got: {newGroupCreditsAmount})"))
                return false;
        }

        if (terminal.groupCredits - realPrice < 0)
            return false;

        newGroupCreditsAmount = terminal.groupCredits - realPrice;

        return true;
    }

    /// <summary>
    /// Prevent the large steel doors from being controlled when not interacting with the terminal
    /// </summary>
    [HarmonyPatch(typeof(TerminalAccessibleObject), nameof(TerminalAccessibleObject.SetDoorOpenServerRpc))]
    [HarmonyPrefix]
    private static bool OnSetDoorOpen(TerminalAccessibleObject __instance)
    {
        var player = __instance.ExecutingPlayer();

        if ((int)player.playerClientId != playerUsingTerminal)
        {
            if (player.ReportHack(Detection.Terminal,
                    $"Player {player.playerUsername} tried to control large doors without interacting with the terminal"))
                return false;
        }

        return true;
    }
}