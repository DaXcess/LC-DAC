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

        private static readonly Harmony RuntimeHarmony = new("io.daxcess.lc-anticheat-runtime");
        private static readonly Harmony EntryHarmony = new("io.daxcess.lc-anticheat-entrypoint");

        private void Awake()
        {
            DAC.Logger.SetSource(Logger);

            EntryHarmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.Start)),
                postfix: new HarmonyMethod(typeof(Plugin), nameof(OnGameEntered)));
            EntryHarmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.OnDestroy)),
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
            
            RuntimeHarmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void OnGameLeft()
        {
            RuntimeHarmony.UnpatchSelf();
        }
    }
}