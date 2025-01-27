using BepInEx.Logging;
using HarmonyLib;
using RevivePlayerOnTimer.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using RevivePlayerOnTimer;

namespace RevivePlayerOnTimer.Patches
{
    internal class RPOTNetworkHandlerPatch
    {
        [HarmonyPrefix, HarmonyPatch(typeof(RPOTNetworkHandler), nameof(RPOTNetworkHandler.RevivePlayerClientRpc))]
        static void SpawnNetworkHandler(RPOTNetworkHandler __instance)
        {
            RPOTNetworkHandler.mls.LogDebug(__instance.__rpc_exec_stage);
            //__instance.mls.LogDebug(__instance.networkManager == null ? "null" : __instance.networkManager);
        }
    }
}