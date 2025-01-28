using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using RevivePlayerOnTimer;
using RevivePlayerOnTimer.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Timers;
using Unity.Netcode;
using UnityEngine;
using static RevivePlayerOnTimer.RevivePlayerOnTimer;

/*  The current workflow: 
    When a client dies, they send their actualClientId and playerIsDead status to the server.
    The server creates a new instance of a class PlayerStatus and saves it in a dictionary.
    When a new PlayerStatus is intitialized/updated, a DeathTimer is started.
    When the unity timer is complete, it calls DeathTimerEVENT which calls RevivePlayerClientRPC
    ^ I'm sorry if this workflow is weird or obscure. I'm open to suggestions.
    
    Current issue: calling RevivePlayerClientRPC seems to function if I call it from somewhere like StartOfRoundPatch.StartGame,
    but it doesn't do anything if I call it from within DeathTimerEvent. Perhaps because RPOTNetworkHandler.Instance is not correct?

    Another tentative issue: I have not tested my custom ReviveDeadPlayer method yet. Still trying to get it to run on the client first.
 */
namespace RevivePlayerOnTimer
{
    // ----------------------------------------------- MAIN CLASS ----------------------------------------------- 
    [BepInPlugin("Angst-RevivePlayerOnTimer", "RevivePlayerOnTimer", "1.0.0.0")]
    [BepInDependency("Angst-ScalingDailyQuota", BepInDependency.DependencyFlags.SoftDependency)]

    public class RevivePlayerOnTimer : BaseUnityPlugin
    {
        public static ConfigEntry<int>? deathTimerLength;
        public static ConfigEntry<bool>? timerReviveIncreasesQuota;
        public static ConfigEntry<int>? reviveQuotaPentaltyAmount;

        public static bool flagScalingDailyQuotaInstalled = false;
        private readonly Harmony harmony = new Harmony("Angst-RevivePlayerOnTimer");
        public static RevivePlayerOnTimer? Instance;
        public static ManualLogSource mls;
        public static Dictionary<ulong, PlayerStatus> playerStatusDictionary;

        private void Awake()
        {
            if (Instance = null)
            {
                Instance = this;
            }

            // logger
            mls = BepInEx.Logging.Logger.CreateLogSource("Angst-RevivePlayerOnTimer");
            mls.LogInfo("Robot is online.");
            mls.LogInfo("Reviewing primary directives...");

            NetcodePatcher(); // ONLY RUN ONCE
            renewDictionary();
            // check for soft dependency
            flagScalingDailyQuotaInstalled = CheckForSoftDependencies("Angst-ScalingDailyQuota");

            // config
            deathTimerLength =
            base.Config.Bind(
            "Angst",
            "Death Timer Length",
            5,
            "Time until player is automatically revived."
            );

            timerReviveIncreasesQuota =
            base.Config.Bind(
            "Angst",
            "Timer Revive Increases Quota?",
            false,
            "If true. The quota will increase by a set penaly amount when a player is revived. REQUIRES Angst-ScalingDailyQuota to sync quota values between client."
            );

            reviveQuotaPentaltyAmount =
            base.Config.Bind(
            "Angst",
            "Quota Penalty Increase Amount",
            100,
            "The quota will increase by this amount when a player is revived. REQUIRES Angst-ScalingDailyQuota"
            );





            // patches
            harmony.PatchAll(typeof(RevivePlayerOnTimer));
            harmony.PatchAll(typeof(GameNetcodeStuffPatch));
            harmony.PatchAll(typeof(GameNetworkManagerPatch));
            harmony.PatchAll(typeof(RPOTNetworkHandlerPatch));
        }

        // check for soft dependency Angst-ScalingDailyQuota
        private static bool CheckForSoftDependencies(string pluginGUID)
        {
            foreach (var plugin in Chainloader.PluginInfos.Values)
            {

                if (plugin.Metadata.GUID == pluginGUID)
                {
                    mls.LogInfo("Angst-ScalingDailyQuota mod found!");
                    return true;
                }
            }
            mls.LogWarning("Angst-ScalingDailyQuota mod NOT found! Some features may not work");
            return false;
        }

        // init the dictionary, or clear it (at the beginning of a new server/game/round etc.)
        public static void renewDictionary()
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
            mls = BepInEx.Logging.Logger.CreateLogSource("Angst-RevivePlayerOnTimer");

            PlayerControllerB pl; // = StartOfRound.Instance.allPlayerScripts.SingleOrDefault(PlayerControllerB => PlayerControllerB.actualClientId == playerID);
            StartOfRound.Instance.allPlayersDead = false;

            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                pl = StartOfRound.Instance.allPlayerScripts[i];

                //Debug.Log("Reviving players A");
                pl.ResetPlayerBloodObjects(pl.isPlayerDead);
                if (pl.actualClientId != playerID || (!pl.isPlayerDead && !pl.isPlayerControlled))
                {
                    mls.LogInfo("Skipping uncontrollerd player " + i);
                    continue;
                }
                pl.isClimbingLadder = false;
                pl.clampLooking = false;
                pl.inVehicleAnimation = false;
                pl.disableMoveInput = false;
                pl.ResetZAndXRotation();
                pl.thisController.enabled = true;
                pl.health = 100;
                pl.hasBeenCriticallyInjured = false;
                pl.disableLookInput = false;
                pl.disableInteract = false;
                //Debug.Log("Reviving players B");
                if (pl.isPlayerDead)
                {
                    pl.isPlayerDead = false;
                    //sync player status here?
                    pl.isPlayerControlled = true;
                    pl.isInElevator = true;
                    pl.isInHangarShipRoom = true;
                    pl.isInsideFactory = false;
                    pl.parentedToElevatorLastFrame = false;
                    pl.overrideGameOverSpectatePivot = null;
                    StartOfRound.Instance.SetPlayerObjectExtrapolate(enable: false);
                    pl.TeleportPlayer(StartOfRound.Instance.GetPlayerSpawnPosition(i));
                    pl.setPositionOfDeadPlayer = false;
                    pl.DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[i], enable: true, disableLocalArms: true);
                    pl.helmetLight.enabled = false;
                    //Debug.Log("Reviving players C");
                    pl.Crouch(crouch: false);
                    pl.criticallyInjured = false;
                    if (pl.playerBodyAnimator != null)
                    {
                        pl.playerBodyAnimator.SetBool("Limp", value: false);
                    }
                    pl.bleedingHeavily = false;
                    pl.activatingItem = false;
                    pl.twoHanded = false;
                    pl.inShockingMinigame = false;
                    pl.inSpecialInteractAnimation = false;
                    pl.freeRotationInInteractAnimation = false;
                    pl.disableSyncInAnimation = false;
                    pl.inAnimationWithEnemy = null;
                    pl.holdingWalkieTalkie = false;
                    pl.speakingToWalkieTalkie = false;
                    //Debug.Log("Reviving players D");
                    pl.isSinking = false;
                    pl.isUnderwater = false;
                    pl.sinkingValue = 0f;
                    pl.statusEffectAudio.Stop();
                    pl.DisableJetpackControlsLocally();
                    pl.health = 100;
                    //Debug.Log("Reviving players E");
                    pl.mapRadarDotAnimator.SetBool("dead", value: false);
                    pl.externalForceAutoFade = UnityEngine.Vector3.zero;
                    if (pl.IsOwner)
                    {
                        HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", value: false);
                        pl.hasBegunSpectating = false;
                        HUDManager.Instance.RemoveSpectateUI();
                        HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                        pl.hinderedMultiplier = 1f;
                        pl.isMovementHindered = 0;
                        pl.sourcesCausingSinking = 0;
                        //Debug.Log("Reviving players E2");
                        pl.reverbPreset = StartOfRound.Instance.shipReverb;
                    }
                }
                //Debug.Log("Reviving players F");
                SoundManager.Instance.earsRingingTimer = 0f;
                pl.voiceMuffledByEnemy = false;
                SoundManager.Instance.playerVoicePitchTargets[i] = 1f;
                SoundManager.Instance.SetPlayerPitch(1f, i);
                if (pl.currentVoiceChatIngameSettings == null)
                {
                    StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
                }
                if (pl.currentVoiceChatIngameSettings != null)
                {
                    if (pl.currentVoiceChatIngameSettings.voiceAudio == null)
                    {
                        pl.currentVoiceChatIngameSettings.InitializeComponents();
                    }
                    if (pl.currentVoiceChatIngameSettings.voiceAudio == null)
                    {
                        return;
                    }
                    pl.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
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
            // idea, consider not deleting the ragdolls and make them worth a rebate if return to the company.
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


    // ------------------------------------------------- NETWORK HANDLER -------------------------------------------------  
    public class RPOTNetworkHandler : NetworkBehaviour
    {
        public static RPOTNetworkHandler Instance { get; private set; }
        public static ManualLogSource mls;

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            Instance = this;

            base.OnNetworkSpawn();
            mls = BepInEx.Logging.Logger.CreateLogSource("Angst-RevivePlayerOnTimer");
        }


        // Sends updated player status to server and saves it in a dictionary.
        [ServerRpc(RequireOwnership = false)]
        public void SyncPlayerStatusServerRpc(bool playerIsDead, ServerRpcParams serverRpcParams = default)
        {
            var playerID = serverRpcParams.Receive.SenderClientId;
            if (RevivePlayerOnTimer.playerStatusDictionary.ContainsKey(playerID))
            {
                RevivePlayerOnTimer.playerStatusDictionary[playerID] = new PlayerStatus(playerID, playerIsDead);
            }
            else
            {
                RevivePlayerOnTimer.playerStatusDictionary.Add(playerID, new PlayerStatus(playerID, playerIsDead));
            }
        }


        [ClientRpc]
        public void RevivePlayerClientRpc(ulong playerID)
        {
            mls.LogInfo("Player " + playerID + " revived");
            ReviveDeadPlayer(playerID);
            // revive player
            // resync player status? might not be needed
        }
    }


    public class PlayerStatus
    {
        public ulong playerID { get; set; } = 0;
        public bool playerIsDead { get; set; } = false;
        private IEnumerator deathTimerCoroutine;
        static ManualLogSource mls;
        
        public PlayerStatus(ulong id, bool pid)
        {
            mls = BepInEx.Logging.Logger.CreateLogSource("Angst-RevivePlayerOnTimer");
            playerID = id;
            playerIsDead = pid;
            deathTimerCoroutine = DeathTimerCoroutine();

            mls.LogInfo("Player " + playerID + " status updated " + (playerIsDead ? "(they're dead)" : "(they're alive)"));

            if (playerIsDead)
            {
                mls.LogInfo("Player " + playerID + " death timer started");

                // find which instance in allPlayerScripts contains the correct player, and start a coroutine
                PlayerControllerB pl = StartOfRound.Instance.allPlayerScripts.SingleOrDefault(PlayerControllerB => PlayerControllerB.actualClientId == playerID);
                pl.StartCoroutine(deathTimerCoroutine);
            }
        }

        private static void SoftDependatCode()
        {
            var newQuota = TimeOfDay.Instance.profitQuota - reviveQuotaPentaltyAmount.Value;
            ScalingDailyQuota.ScalingDailyQuota.SDQNetworkHandler.Instance.SyncDailyQuotaClientRPC(1, 1, 1, 1);
        }
        private IEnumerator DeathTimerCoroutine()
        {
            float timerLength = Math.Max(RevivePlayerOnTimer.deathTimerLength.Value, 5);
            yield return new WaitForSeconds(timerLength);

            mls.LogInfo("Player " + playerID + " death timer finished");

            if (flagScalingDailyQuotaInstalled && timerReviveIncreasesQuota.Value) { SoftDependatCode(); }
            else
            {
                mls.LogError("Please install the missing mod \"Angst-ScalingDailyQuota\" if you wish to increment the quota when a player is revived.");
                mls.LogError("...LINK TO MOD TBD...");
            }

            try
            {
                RPOTNetworkHandler.Instance.RevivePlayerClientRpc(playerID);
            }
            catch (Exception err)
            {
                mls.LogError("error on network handler");
                mls.LogError(err);
            }
            finally
            {
                ReviveDeadPlayer(playerID);
            }
        }
    }
}
