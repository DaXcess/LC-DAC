using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace DAC;

[HarmonyPatch]
public static class DACManager
{
    private static readonly Dictionary<Detection, DetectionResponse> detections = new()
    {
        { Detection.AntiKick, DetectionResponse.KickPlayer },
        { Detection.ChatSpoof, DetectionResponse.BanPlayer },
        { Detection.EndGame, DetectionResponse.IgnoreRpc },
        { Detection.FriendlyFireSpoof, DetectionResponse.BanPlayer },
        { Detection.FriendlyFire, DetectionResponse.IgnoreRpc },
        { Detection.ShipObjectRatelimit, DetectionResponse.IgnoreRpc },
        { Detection.ShipObjectRotation, DetectionResponse.IgnoreRpc },
        { Detection.StartGame, DetectionResponse.IgnoreRpc },
        { Detection.Terminal, DetectionResponse.IgnoreRpc },
        { Detection.TerminalPrice, DetectionResponse.IgnoreRpc }, // Even on WarnOnly, the price will get rectified
        { Detection.Placement, DetectionResponse.IgnoreRpc }
    };

    private static readonly Dictionary<ulong, ulong> ngoIdToSteamId = [];
    private static readonly Dictionary<ulong, Detection> bannedSteamIds = [];

    public static ulong GetSteamId(ulong playerId) => ngoIdToSteamId[playerId];

    public static bool ReportHack(this PlayerControllerB player, Detection detection, string message)
    {
        // Ignore local player (host)
        if (player == StartOfRound.Instance.localPlayerController)
            return false;

        Logger.LogWarning($"[{detection}] ${message}");

        if (detections[detection] is not (DetectionResponse.KickPlayer or DetectionResponse.BanPlayer))
            return detections[detection] != DetectionResponse.WarnOnly;

        if (detections[detection] is DetectionResponse.BanPlayer)
            bannedSteamIds.Add(player.playerSteamId, detection);

        NetworkManager.Singleton.DisconnectClient(player.actualClientId);
        HUDManager.Instance.AddTextToChatOnServer(
            $"{player.playerUsername} was {(detections[detection] is DetectionResponse.KickPlayer ? "kicked" : "banned")} for {detection}");

        return detections[detection] != DetectionResponse.WarnOnly;
    }

    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.ConnectionApproval))]
    [HarmonyPostfix]
    private static void OnConnectionApproval(GameNetworkManager __instance,
        ref NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        if (__instance.disableSteam || !response.Approved || !__instance.currentLobby.HasValue)
            return;

        var payload = Encoding.ASCII.GetString(request.Payload).Split(",");

        if (payload.Length < 2 || !ulong.TryParse(payload[1], out var steamId))
        {
            response.Reason = "[DAC] Client negotiation failed";
            response.Approved = false;

            return;
        }

        if (__instance.currentLobby.Value.Members.All(user => user.Id.Value != steamId))
        {
            response.Reason = "[DAC] Steam Client ID rejected";
            response.Approved = false;

            return;
        }

        if (bannedSteamIds.ContainsKey(steamId))
        {
            response.Reason =
                "[DAC] You have been banned from this lobby for cheating";
            response.Approved = false;

            return;
        }

        ngoIdToSteamId.Add(request.ClientNetworkId, steamId);
    }
}

public enum Detection
{
    AntiKick,
    ChatSpoof,
    EndGame,
    FriendlyFireSpoof,
    FriendlyFire,
    ShipObjectRatelimit,
    ShipObjectRotation,
    StartGame,
    Terminal,
    TerminalPrice,
    Placement
}

public enum DetectionResponse
{
    WarnOnly,
    IgnoreRpc,
    KickPlayer,
    BanPlayer
}