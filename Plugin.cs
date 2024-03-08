using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace DAC
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const string PLUGIN_GUID = "io.daxcess.lc-anticheat";
        private const string PLUGIN_NAME = "DAC";
        private const string PLUGIN_VERSION = "1.0.0";

        private static readonly Harmony patcher = new("io.daxcess.lc-anticheat-entrypoint");

        private void Awake()
        {
            DAC.Logger.SetSource(Logger);

            patcher.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.Start)),
                postfix: new HarmonyMethod(typeof(Plugin), nameof(OnGameEntered)));
            patcher.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.OnDestroy)),
                postfix: new HarmonyMethod(typeof(Plugin), nameof(OnGameLeft)));

            // Hide from mod list to prevent clients from finding out whether or not the server has anticheat
            Chainloader.PluginInfos.Remove(PLUGIN_GUID);
        }

        private static void OnGameEntered()
        {
            if (!GameNetworkManager.Instance.isHostingGame)
            {
                DAC.Logger.LogWarning("Not the host, disabling DAC!");
                return;
            }
            
            DACPatcher.ApplyPatches();
        }

        private static void OnGameLeft()
        {
            DACPatcher.RevertPatches();
        }
    }
}