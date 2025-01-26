using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using RevivePlayerOnTimer.Modules;
using BepInEx.Logging;
using static RevivePlayerOnTimer.RevivePlayerOnTimer;

namespace RevivePlayerOnTimer.Patches
{

    [HarmonyPatch]
    public class GameNetworkManagerPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void Init()
        {
            if (networkPrefab != null)
                return;

            networkPrefab = Modules.NetworkPrefabs.CreateNetworkPrefab("RevivePlayerOnTimer");
            networkPrefab.AddComponent<RPOTNetworkHandler>();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
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
            //mls.LogInfo("subbed to network handler.");
            //SubscribeToHandler();
        }
        //static void SubscribeToHandler()
        //{
        //    RPOTNetworkHandler.LevelEvent += ReceivedEventFromServer;
        //}

        //// currently does not unsubscribe!
        //// how to unsub when player leaves lobby?
        //static void UnsubscribeFromHandler()
        //{
        //    RPOTNetworkHandler.LevelEvent -= ReceivedEventFromServer;
        //}

        //static void ReceivedEventFromServer(string eventName)
        //{
        //    // Event Code Here
        //}

        //static void SendEventToClients(string eventName)
        //{
        //    if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
        //        return;

        //    RPOTNetworkHandler.Instance.EventClientRpc(eventName);
        //}

        static GameObject networkPrefab;
    }
}