using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using RevivePlayerOnTimer.Patches;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using Unity.Netcode;
using UnityEngine;
using static RevivePlayerOnTimer.RevivePlayerOnTimer;

namespace RevivePlayerOnTimer
{
    // ----------------------------------------------- MAIN CLASS ----------------------------------------------- 
    [BepInPlugin("Angst-RevivePlayerOnTimer", "RevivePlayerOnTimer", "1.0.0.0")]
    public class RevivePlayerOnTimer : BaseUnityPlugin
    {
        // config
        public static ConfigEntry<int>? deathTimerLength;

        private readonly Harmony harmony = new Harmony("Angst-RevivePlayerOnTimer");
        private static RevivePlayerOnTimer? Instance;
        public ManualLogSource? mls;

        public static Dictionary<ulong, PlayerStatus> playerStatusDictionary;

        private void Awake()
        {
            if (Instance = null)
            {
                Instance = this;
            }

            NetcodePatcher(); // ONLY RUN ONCE

            renewDictionary();

            // config
            deathTimerLength =
            base.Config.Bind(
            "Angst",
            "Death Timer Length",
            5,
            "Time until player is automatically revived."
            );

            // logger
            mls = BepInEx.Logging.Logger.CreateLogSource("Angst-RevivePlayerOnTimer");
            mls.LogInfo("Robot is online.");
            mls.LogInfo("Reviewing primary directives...");

            // patches
            harmony.PatchAll(typeof(RevivePlayerOnTimer));
            harmony.PatchAll(typeof(GameNetcodeStuffPatch));
            harmony.PatchAll(typeof(GameNetworkManagerPatch));
            //harmony.PatchAll(typeof(StartOfRoundPatch));
        }

        public void renewDictionary()
        {
            if (playerStatusDictionary != null)
            {
                playerStatusDictionary.Clear();
            }
            else
            {
                playerStatusDictionary = new Dictionary<ulong, PlayerStatus>();
            }
        }

        // --------------------------------------------------- REVIVE CODE ---------------------------------------------------

        public static void ReviveDeadPlayer(ulong playerID)
        {
            ManualLogSource mls = BepInEx.Logging.Logger.CreateLogSource("Angst-RevivePlayerOnTimer");


            StartOfRound.Instance.allPlayersDead = false;

            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                //Debug.Log("Reviving players A");
                StartOfRound.Instance.allPlayerScripts[i].ResetPlayerBloodObjects(StartOfRound.Instance.allPlayerScripts[i].isPlayerDead);
                if (StartOfRound.Instance.allPlayerScripts[i].actualClientId != playerID || (!StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && !StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled))
                {
                    mls.LogInfo("Skipping player " + i + " revive.");
                    continue;
                }
                StartOfRound.Instance.allPlayerScripts[i].isClimbingLadder = false;
                StartOfRound.Instance.allPlayerScripts[i].clampLooking = false;
                StartOfRound.Instance.allPlayerScripts[i].inVehicleAnimation = false;
                StartOfRound.Instance.allPlayerScripts[i].disableMoveInput = false;
                StartOfRound.Instance.allPlayerScripts[i].ResetZAndXRotation();
                StartOfRound.Instance.allPlayerScripts[i].thisController.enabled = true;
                StartOfRound.Instance.allPlayerScripts[i].health = 100;
                StartOfRound.Instance.allPlayerScripts[i].hasBeenCriticallyInjured = false;
                StartOfRound.Instance.allPlayerScripts[i].disableLookInput = false;
                StartOfRound.Instance.allPlayerScripts[i].disableInteract = false;
                //Debug.Log("Reviving players B");
                if (StartOfRound.Instance.allPlayerScripts[i].isPlayerDead)
                {
                    StartOfRound.Instance.allPlayerScripts[i].isPlayerDead = false;
                    //sync player status here?
                    StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled = true;
                    StartOfRound.Instance.allPlayerScripts[i].isInElevator = true;
                    StartOfRound.Instance.allPlayerScripts[i].isInHangarShipRoom = true;
                    StartOfRound.Instance.allPlayerScripts[i].isInsideFactory = false;
                    StartOfRound.Instance.allPlayerScripts[i].parentedToElevatorLastFrame = false;
                    StartOfRound.Instance.allPlayerScripts[i].overrideGameOverSpectatePivot = null;
                    StartOfRound.Instance.SetPlayerObjectExtrapolate(enable: false);
                    StartOfRound.Instance.allPlayerScripts[i].TeleportPlayer(StartOfRound.Instance.GetPlayerSpawnPosition(i));
                    StartOfRound.Instance.allPlayerScripts[i].setPositionOfDeadPlayer = false;
                    StartOfRound.Instance.allPlayerScripts[i].DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[i], enable: true, disableLocalArms: true);
                    StartOfRound.Instance.allPlayerScripts[i].helmetLight.enabled = false;
                    //Debug.Log("Reviving players C");
                    StartOfRound.Instance.allPlayerScripts[i].Crouch(crouch: false);
                    StartOfRound.Instance.allPlayerScripts[i].criticallyInjured = false;
                    if (StartOfRound.Instance.allPlayerScripts[i].playerBodyAnimator != null)
                    {
                        StartOfRound.Instance.allPlayerScripts[i].playerBodyAnimator.SetBool("Limp", value: false);
                    }
                    StartOfRound.Instance.allPlayerScripts[i].bleedingHeavily = false;
                    StartOfRound.Instance.allPlayerScripts[i].activatingItem = false;
                    StartOfRound.Instance.allPlayerScripts[i].twoHanded = false;
                    StartOfRound.Instance.allPlayerScripts[i].inShockingMinigame = false;
                    StartOfRound.Instance.allPlayerScripts[i].inSpecialInteractAnimation = false;
                    StartOfRound.Instance.allPlayerScripts[i].freeRotationInInteractAnimation = false;
                    StartOfRound.Instance.allPlayerScripts[i].disableSyncInAnimation = false;
                    StartOfRound.Instance.allPlayerScripts[i].inAnimationWithEnemy = null;
                    StartOfRound.Instance.allPlayerScripts[i].holdingWalkieTalkie = false;
                    StartOfRound.Instance.allPlayerScripts[i].speakingToWalkieTalkie = false;
                    //Debug.Log("Reviving players D");
                    StartOfRound.Instance.allPlayerScripts[i].isSinking = false;
                    StartOfRound.Instance.allPlayerScripts[i].isUnderwater = false;
                    StartOfRound.Instance.allPlayerScripts[i].sinkingValue = 0f;
                    StartOfRound.Instance.allPlayerScripts[i].statusEffectAudio.Stop();
                    StartOfRound.Instance.allPlayerScripts[i].DisableJetpackControlsLocally();
                    StartOfRound.Instance.allPlayerScripts[i].health = 100;
                    //Debug.Log("Reviving players E");
                    StartOfRound.Instance.allPlayerScripts[i].mapRadarDotAnimator.SetBool("dead", value: false);
                    StartOfRound.Instance.allPlayerScripts[i].externalForceAutoFade = UnityEngine.Vector3.zero;
                    if (StartOfRound.Instance.allPlayerScripts[i].IsOwner)
                    {
                        HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", value: false);
                        StartOfRound.Instance.allPlayerScripts[i].hasBegunSpectating = false;
                        HUDManager.Instance.RemoveSpectateUI();
                        HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                        StartOfRound.Instance.allPlayerScripts[i].hinderedMultiplier = 1f;
                        StartOfRound.Instance.allPlayerScripts[i].isMovementHindered = 0;
                        StartOfRound.Instance.allPlayerScripts[i].sourcesCausingSinking = 0;
                        //Debug.Log("Reviving players E2");
                        StartOfRound.Instance.allPlayerScripts[i].reverbPreset = StartOfRound.Instance.shipReverb;
                    }
                }
                //Debug.Log("Reviving players F");
                SoundManager.Instance.earsRingingTimer = 0f;
                StartOfRound.Instance.allPlayerScripts[i].voiceMuffledByEnemy = false;
                SoundManager.Instance.playerVoicePitchTargets[i] = 1f;
                SoundManager.Instance.SetPlayerPitch(1f, i);
                if (StartOfRound.Instance.allPlayerScripts[i].currentVoiceChatIngameSettings == null)
                {
                    StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
                }
                if (StartOfRound.Instance.allPlayerScripts[i].currentVoiceChatIngameSettings != null)
                {
                    if (StartOfRound.Instance.allPlayerScripts[i].currentVoiceChatIngameSettings.voiceAudio == null)
                    {
                        StartOfRound.Instance.allPlayerScripts[i].currentVoiceChatIngameSettings.InitializeComponents();
                    }
                    if (StartOfRound.Instance.allPlayerScripts[i].currentVoiceChatIngameSettings.voiceAudio == null)
                    {
                        return;
                    }
                    StartOfRound.Instance.allPlayerScripts[i].currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
                }
                //Debug.Log("Reviving players G");
            }
            PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
            playerControllerB.bleedingHeavily = false;
            playerControllerB.criticallyInjured = false;
            playerControllerB.playerBodyAnimator.SetBool("Limp", value: false);
            playerControllerB.health = 100;
            HUDManager.Instance.UpdateHealthUI(100, hurtPlayer: false);
            playerControllerB.spectatedPlayerScript = null;
            HUDManager.Instance.audioListenerLowPass.enabled = false;
            //Debug.Log("Reviving players H");
            StartOfRound.Instance.SetSpectateCameraToGameOverMode(enableGameOver: false, playerControllerB);

            // delete the dead ragdoll if it belongs to the player.
            RagdollGrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<RagdollGrabbableObject>();
            for (int j = 0; j < array.Length; j++)
            {
                if (array[j].ragdoll.playerScript.actualClientId != playerID)
                { continue; }

                if (!array[j].isHeld)
                {
                    if (StartOfRound.Instance.IsServer)
                    {
                        if (array[j].NetworkObject.IsSpawned)
                        {
                            array[j].NetworkObject.Despawn();
                        }
                        else
                        {
                            UnityEngine.Object.Destroy(array[j].gameObject);
                        }
                    }
                }
                else if (array[j].isHeld && array[j].playerHeldBy != null)
                {
                    array[j].playerHeldBy.DropAllHeldItems();
                }
            }

            // delete the dead body info if it belongs to the player.
            DeadBodyInfo[] array2 = UnityEngine.Object.FindObjectsOfType<DeadBodyInfo>();
            for (int k = 0; k < array2.Length; k++)
            {
                if (array2[k].playerScript.actualClientId != playerID)
                { continue; }
                UnityEngine.Object.Destroy(array2[k].gameObject);
            }
            StartOfRound.Instance.livingPlayers++;
            StartOfRound.Instance.allPlayersDead = false;
            StartOfRound.Instance.UpdatePlayerVoiceEffects();
            //StartOfRound.Instance.ResetMiscValues();
        }


        // ------------------------------------------------- NETCODE PATCHER ------------------------------------------------- 
        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }


    // ---------------------------------- Network Handler ---------------------------------- 
    public class RPOTNetworkHandler : NetworkBehaviour
    {
        ManualLogSource mls;
        public override void OnNetworkSpawn()
        {
            //LevelEvent = null;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            Instance = this;

            base.OnNetworkSpawn();
        }


        //[ClientRpc]
        //public void EventClientRpc(string eventName)
        //{
        //    LevelEvent?.Invoke(eventName); // If the event has subscribers (does not equal null), invoke the event
        //}

        //[ServerRpc(RequireOwnership = false)]
        //public void EventServerRPC(string eventName)
        //{
        //    LevelEvent?.Invoke(eventName);
        //}

        // Sends updated player status to server and saves it in a dictionary.


        [ServerRpc(RequireOwnership = false)]
        public void SyncPlayerStatusServerRPC(ulong playerID, bool playerIsDead)
        {
            try
            {
                RevivePlayerOnTimer.playerStatusDictionary.Add(playerID, new PlayerStatus(playerID, playerIsDead));
            }
            catch (ArgumentException)
            {
                RevivePlayerOnTimer.playerStatusDictionary[playerID] = new PlayerStatus(playerID, playerIsDead);
            }
        }

        [ClientRpc]
        public void RevivePlayerClientRpc(ulong playerID)
        {
            mls = BepInEx.Logging.Logger.CreateLogSource("Angst-RevivePlayerOnTimer");
            mls.LogInfo("RevivePlayerClientRpc Command Received: " + playerID + " revived????");
            ReviveDeadPlayer(playerID);
            // revive player
            // resync player status? might not be needed
        }

        //public static event Action<String> LevelEvent;

        public static RPOTNetworkHandler Instance { get; private set; }
    }


    public class PlayerStatus
    {
        public ulong playerID { get; set; } = 0;
        public bool playerIsDead { get; set; } = false;

        public static System.Timers.Timer playerDeathTimer;
        ManualLogSource mls;


        public PlayerStatus(ulong id, bool pid)
        {
            mls = BepInEx.Logging.Logger.CreateLogSource("Angst-RevivePlayerOnTimer");
            playerID = id;
            playerIsDead = pid;
            mls.LogInfo("A PlayerStatus was initialized with ID: " + playerID);

            if (playerIsDead)
            {
                mls.LogInfo("Starting Timer for player " + playerID);

                InitDeathTimer();
                playerDeathTimer.Start();
            }
            else
            {
                if (playerDeathTimer == null)
                { return; }

                playerDeathTimer.Stop();
                playerDeathTimer.Dispose();
            }
        }

        public void InitDeathTimer()
        {
            // set timer
            double num = Math.Max(RevivePlayerOnTimer.deathTimerLength.Value, 5)*1000;
            playerDeathTimer = new System.Timers.Timer(num);

            // subscribe to event
            playerDeathTimer.Elapsed += DeathTimerEVENT;
            playerDeathTimer.AutoReset = false;
        }

        private void DeathTimerEVENT(System.Object source, ElapsedEventArgs e)
        {

            mls.LogInfo("Reviving player " + playerID + " at " + e.SignalTime);
            //ReviveDeadPlayer(playerID);
            RPOTNetworkHandler.Instance.RevivePlayerClientRpc(playerID);
        }
    }
}
