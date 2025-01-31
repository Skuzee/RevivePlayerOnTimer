﻿using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using GameNetcodeStuff;
using BepInEx.Logging;

namespace RevivePlayerOnTimer.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class GameNetcodeStuffPatch
    {
        [HarmonyPatch("KillPlayer")]
        [HarmonyPostfix]
        static void KillPlayerPOST()
        {
            // When a player dies, send the status update to the server (confirmed working)
            RPOTNetworkHandler.Instance.SyncPlayerStatusServerRpc(true);
        }
    }
}
