using HarmonyLib;

namespace DAC.Modules;

/// <summary>
/// This module prevents other players from impersonating other users in chat
/// </summary>
[HarmonyPatch]
internal static class ChatSpoofHack
{
    /// <summary>
    /// Prevent players from impersonating another player in the chat
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddPlayerChatMessageServerRpc))]
    [HarmonyPrefix]
    private static void OnBeforeAddChatMessage(HUDManager __instance, ref int playerId)
    {
        var expectedPlayer = __instance.ExecutingPlayer();
        var receivedPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
        
        if (playerId != (int)expectedPlayer.playerClientId)
        {
            Logger.LogWarning(
                $"Player {expectedPlayer.playerUsername} tried to impersonate {receivedPlayer.playerUsername} in the chat");

            playerId = (int)expectedPlayer.playerClientId;
        }
    }
}