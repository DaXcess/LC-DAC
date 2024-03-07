using HarmonyLib;
using Unity.Netcode;

namespace DAC.Modules;

/// <summary>
/// This module prevents other players from impersonating other users in chat
/// </summary>
[HarmonyPatch]
internal static class ChatSpoofHack
{
    private static bool _calledByHandler;
    private static int _actualPlayerId;
    
    /// <summary>
    /// Store a reference to the player that was actually sending the chat message
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.__rpc_handler_2930587515))]
    [HarmonyPrefix]
    private static void OnBeforeHandleChatMessage(ref __RpcParams rpcParams)
    {
        _calledByHandler = true;
        _actualPlayerId = (int)rpcParams.Server.Receive.SenderClientId;
    }

    /// <summary>
    /// Prevent players from impersonating another player in the chat
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddPlayerChatMessageServerRpc))]
    [HarmonyPrefix]
    private static void OnBeforeAddChatMessage(ref int playerId)
    {
        if (!_calledByHandler)
            _actualPlayerId = 0;
        
        var expectedPlayer = StartOfRound.Instance.allPlayerScripts[_actualPlayerId];
        var receivedPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
        
        if (playerId != _actualPlayerId)
        {
            Logger.LogWarning(
                $"Player {expectedPlayer.playerUsername} tried to impersonate {receivedPlayer.playerUsername} in the chat");

            playerId = _actualPlayerId;
        }
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddPlayerChatMessageServerRpc))]
    [HarmonyPostfix]
    private static void AfterAddChatMessage()
    {
        _calledByHandler = false;
    }
}