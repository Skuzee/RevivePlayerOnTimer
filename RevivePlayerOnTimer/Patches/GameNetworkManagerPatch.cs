using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using RevivePlayerOnTimer.Modules;
using BepInEx.Logging;
using static RevivePlayerOnTimer.RevivePlayerOnTimer;

namespace RevivePlayerOnTimer.Patches
{

    // Creates a Network Hander, registers it to the network, and spawns it at StartOfRound.Awake

    [HarmonyPatch]
    public class GameNetworkManagerPatch
    {
        static GameObject networkPrefab;


        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void Init()
        {
            if (networkPrefab != null)
                return;

            networkPrefab = Modules.NetworkPrefabs.CreateNetworkPrefab("RevivePlayerOnTimer");
            networkPrefab.AddComponent<RPOTNetworkHandler>();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);

            RevivePlayerOnTimer.renewDictionary();
        }


        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {
            ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource("Angst-RevivePlayerOnTimer");

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                mls.LogInfo("spawning network handler.");
                var networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }

    }
}