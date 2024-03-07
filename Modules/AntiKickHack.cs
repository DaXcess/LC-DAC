using HarmonyLib;

namespace DAC.Modules;

/// <summary>
/// This module prevents users from hiding their identity which makes them unable to be kicked
/// </summary>
[HarmonyPatch]
internal static class AntiKickHack
{
    // TODO: Figure out the best way to fix this hack
}