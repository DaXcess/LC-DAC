using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace DAC;

public static class DACPatcher
{
    private static Harmony patcher = new("io.daxcess.lc-anticheat-runtime");
    private static Dictionary<NetworkBehaviour, ulong> rpcPlayerCache = [];
    
    public static void ApplyPatches()
    {
        var gameAssembly = StartOfRound.Instance.GetType().Assembly;
        var allTypes = gameAssembly.GetTypes().Where(type => type.IsSubclassOf(typeof(NetworkBehaviour)));

        foreach (var type in allTypes)
        {
            var allRpcMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Where(method =>
            {
                var @params = method.GetParameters();

                if (@params.Length != 3)
                    return false;

                if (@params[0].ParameterType != typeof(NetworkBehaviour) || @params[0].Name != "target")
                    return false;

                if (@params[1].ParameterType != typeof(FastBufferReader) || @params[1].Name != "reader")
                    return false;

                if (@params[2].ParameterType != typeof(__RpcParams) || @params[2].Name != "rpcParams")
                    return false;

                var bytes = method.GetMethodBody()?.GetILAsByteArray();
                if (bytes == null)
                    return false;

                var idx = Array.IndexOf(bytes, (byte)0x7D) - 4;
                if (idx < 0)
                    return false;

                if (bytes[idx] == 2)
                    return false;
                
                return true;
            });

            foreach (var method in allRpcMethods)
            {
                patcher.Patch(method,
                    new HarmonyMethod(((Action<NetworkBehaviour, __RpcParams>)BeforeRpcHandle).Method),
                    new HarmonyMethod(((Action<NetworkBehaviour>)AfterRpcHandle).Method));
            }
        }
        
        patcher.PatchAll(Assembly.GetExecutingAssembly());
    }

    public static void RevertPatches()
    {
        patcher.UnpatchSelf();
    }
    
    private static void BeforeRpcHandle(NetworkBehaviour target, __RpcParams rpcParams)
    {
        rpcPlayerCache[target] = rpcParams.Server.Receive.SenderClientId;
    }

    private static void AfterRpcHandle(NetworkBehaviour target)
    {
        rpcPlayerCache[target] = 0;
    }

    public static PlayerControllerB ExecutingPlayer(this NetworkBehaviour behaviour)
    {
        return StartOfRound.Instance.allPlayerScripts.First(player =>
            player.actualClientId == rpcPlayerCache[behaviour]);
    }
}