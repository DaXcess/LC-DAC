using HarmonyLib;

namespace DAC.Modules;

/// <summary>
/// This module prevents players from impersonating other users in chat
/// </summary>
[HarmonyPatch]
internal static class ChatSpoofHack
{
    /// <summary>
    /// Prevent players from impersonating another player in the chat
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddPlayerChatMessageServerRpc))]
    [HarmonyPrefix]
    private static bool OnBeforeAddChatMessage(HUDManager __instance, ref int playerId)
    {
        var expectedPlayer = __instance.ExecutingPlayer();
        var receivedPlayer = StartOfRound.Instance.allPlayerScripts[playerId];

        if (playerId == (int)expectedPlayer.playerClientId) return true;
        
        if (expectedPlayer.ReportHack(Detection.ChatSpoof,
                $"Player {expectedPlayer.playerUsername} tried to impersonate {receivedPlayer.playerUsername} in the chat"))
            return false;

        playerId = (int)expectedPlayer.playerClientId;

        return true;
    }
}