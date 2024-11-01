using MelonLoader;
using HarmonyLib;
using UnityEngine;
using BigBoxVR.BattleRoyale.Models.Shared;
using BigBoxVR;
using System.Threading;
using MyTrueGear;
using System.IO;
using Il2CppMicrosoft.Win32;
using System;

namespace PopulationOne_TrueGear
{
    public static class BuildInfo
    {
        public const string Name = "PopulationOne_TrueGear"; // Name of the Mod.  (MUST BE SET)
        public const string Description = "TrueGear Mod for PopulationOne"; // Description for the Mod.  (Set as null if none)
        public const string Author = "HuangLY"; // Author of the Mod.  (MUST BE SET)
        public const string Company = null; // Company that made the Mod.  (Set as null if none)
        public const string Version = "1.0.0"; // Version of the Mod.  (MUST BE SET)
        public const string DownloadLink = null; // Download Link for the Mod.  (Set as null if none)
    }

    public class Main : MelonMod
    {
        private static bool isFlying = false;
        private static bool isFalling = false;
        private static bool isWoosh = false;
        private static bool isPodState = false;
        private static bool isRubbingDefib = false;
        private static bool isShakeConsumable = false;
        private static bool isMaxArmor = false;
        private static bool isPickUp = false;
        private static bool isClimb = false;


        private static bool canClimb = true;
        private static bool canFallDamage = true;

        public static bool isTwoHand = false;
        public static uint myNetId = 0;
        public static PlayerContainer playerContainer = null;
        public static PodState value = PodState.None;
        public static int oldValue = 0;
        private static long fallMillis = -1;

        public static TrueGearMod _TrueGear = null;
        public override void OnInitializeMelon() {
            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Main));
            _TrueGear = new TrueGearMod();
            MelonLogger.Msg("OnApplicationStart");            
        }

        


        [HarmonyPostfix, HarmonyPatch(typeof(PlayerContainer), "OnFixedUpdate")]
        private static void PlayerContainer_OnFixedUpdate_Postfix(PlayerContainer __instance)
        {
            if (!__instance.isLocalPlayer)
            {
                return;
            }
            myNetId = __instance.netId;
            playerContainer = __instance;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BattleContextView), "Awake")]
        private static void BattleContextView_Awake_Postfix()
        {
            GlobalsListen.EventListen();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HandControllerMediator), "OnLocalPlayerClimbHandChanged")]
        private static void HandControllerMediator_OnLocalPlayerClimbHandChanged_Prefix(HandControllerMediator __instance, uint netId, Handedness value)
        {
            if (netId != myNetId)
            {
                return;
            }
            if (value != Handedness.Unknown && canClimb)
            {
                if (isFlying || isFalling)
                {
                    isFlying = false;
                    isFalling = false;
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("StopFall");
                    MelonLogger.Msg("StopFly");
                    _TrueGear.StopFall();
                    _TrueGear.StopFly();
                }                

                isClimb = true;
                canClimb = false;
                if (value == Handedness.Left)
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("LeftHandClimb");
                    _TrueGear.Play("LeftHandPickupItem");
                }
                if (value == Handedness.Right)
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("RightHandClimb");
                    _TrueGear.Play("RightHandPickupItem");
                }
                Timer climbTimer = new Timer(ClimbTimerCallBack, null, 150, Timeout.Infinite);
            }
            else if (value == Handedness.Unknown)
            {
                MelonLogger.Msg(value);
                isClimb = false;
            }
        }
        private static void ClimbTimerCallBack(System.Object o)
        {
            canClimb = true;
        }
        
        
        [HarmonyPrefix, HarmonyPatch(typeof(LootItem), "FlyItem")]
        private static void LootItem_FlyItem_Prefix(LootItem __instance, PlayerContainer container, bool dominantHand)
        {   
            if (container.isLocalPlayer)
            {
                if (isPickUp)
                {
                    return;
                }
                isPickUp = true;
                if (GlobalsListen.myDominantHand == Handedness.Left)
                {
                    if (dominantHand)
                    {
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg("LeftHandPickupItem");
                        _TrueGear.Play("LeftHandPickupItem");
                    }
                    else
                    {
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg("RightHandPickupItem");
                        _TrueGear.Play("RightHandPickupItem");
                    }
                }
                else
                {
                    if (dominantHand)
                    {
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg("RightHandPickupItem");
                        _TrueGear.Play("RightHandPickupItem");
                    }
                    else
                    {
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg("LeftHandPickupItem");
                        _TrueGear.Play("LeftHandPickupItem");
                    }
                }
                Timer PickUpTimer = new Timer(PickUpTimerCallBack,null,100,Timeout.Infinite);
            }            
        }

        private static void PickUpTimerCallBack(object o)
        { 
            isPickUp = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MeleeShield), "ShieldActive", MethodType.Setter)]
        private static void MeleeShield_ShieldActive_Postfix(MeleeShield __instance, bool value)
        {
            if (__instance.melee.isLocalPlayer)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("StartMeleeShield");
                _TrueGear.Play("StartMeleeShield");
            }            
        }

        [HarmonyPostfix, HarmonyPatch(typeof(MeleeShield), "QueueShieldHitHaptic")]
        private static void MeleeShield_QueueShieldHitHaptic_Postfix(MeleeShield __instance)
        {
            if (__instance.melee.isLocalPlayer)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("ShieldBlock");
                _TrueGear.Play("ShieldBlock");
            }            
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerContainer), "CmdStartFistBump")]
        private static void PlayerContainer_CmdStartFistBump_Postfix(PlayerContainer __instance,Handedness handedness)
        {
            if (!__instance.isLocalPlayer)
            {
                if (handedness == Handedness.Left)
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("StartLeftFistBump");
                    _TrueGear.StartLeftFistBump();
                }
                else
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("StartRightFistBump");
                    _TrueGear.StartRightFistBump();
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerContainer), "CmdStopFistBump")]
        private static void PlayerContainer_CmdStopFistBump_Postfix(PlayerContainer __instance, Handedness handedness)
        {

            if (!__instance.isLocalPlayer)
            {
                if (handedness == Handedness.Left)
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("StopLeftFistBump");
                    _TrueGear.StopLeftFistBump();
                }
                else
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("StopRightFistBump");
                    _TrueGear.StopRightFistBump();
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerData), "SyncHealth")]
        private static void PlayerData_SyncHealth_Postfix(PlayerData __instance, int oldValue, int newValue)
        {
            if (!__instance.isLocalPlayer)
            {
                return;
            }
            if (newValue < 25 && oldValue >= 25)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("StartHeartBeat");
                _TrueGear.StartHeartBeat();
            }
            else if (newValue >= 25 && oldValue < 25)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("StopHeartBeat");
                _TrueGear.StopHeartBeat();
            }
            if (newValue >= PlayerData.MaxHealth)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("FullHealth");
                _TrueGear.Play("FullHealth");
            }
            else if(oldValue < newValue)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("Healing");
                _TrueGear.Play("Healing");
            }
            if (newValue <= 0 && oldValue > 0)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("PlayerDeath");
                _TrueGear.Play("PlayerDeath");
                MelonLogger.Msg("StopHeartBeat");
                _TrueGear.StopHeartBeat();
            }
        }

        private static long GetMillis()
        {
            long currentTicks = DateTime.Now.Ticks;
            DateTime dtFrom = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return (currentTicks - dtFrom.Ticks) / 10000;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerData), "MotionState", MethodType.Setter)]
        private static void PlayerData_MotionState_Postfix(PlayerData __instance, MotionState value)
        {
            if (!__instance.isLocalPlayer)
            {
                return;
            }
            if (fallMillis != -1)
            {
                fallMillis = GetMillis();
            }

            if (value == MotionState.Flying && !isFlying && !isClimb)
            {
                isFlying = true;
                isFalling = false;
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("StartFly");
                MelonLogger.Msg("StopFall");
                _TrueGear.StartFly();
                _TrueGear.StopFall();
            }

            if (value == MotionState.Falling && !isFalling && !isClimb)
            {
                isFalling = true;
                isFlying = false;
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("StartFall");
                MelonLogger.Msg("StopFly");
                _TrueGear.StartFall();
                _TrueGear.StopFly();
            }

            if ((value == MotionState.Bipedal || value == MotionState.Idle) && (isFalling || isFlying))
            {
                isFlying = false;
                isFalling = false;
                MelonLogger.Msg("StopFall");
                MelonLogger.Msg("StopFly");
                _TrueGear.StopFall();
                _TrueGear.StopFly();
                if (GetMillis() - fallMillis > 700)
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("FallDamage");
                    _TrueGear.Play("FallDamage");
                }
                else
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("FallOnGround");
                    _TrueGear.Play("FallOnGround");
                }
                fallMillis = -1;
            }
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerData), "Networkarmor", MethodType.Setter)]
        private static void PlayerData_Networkarmor_Postfix(PlayerData __instance, int value)
        {
            if (!__instance.isLocalPlayer)
            {
                return;
            }
            if (value >= __instance.PlayerMaxArmor)
            {
                if (!isMaxArmor)
                {
                    isMaxArmor = true;
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("FullHealth");
                    _TrueGear.Play("FullHealth");
                }                
            }
            else if(oldValue < value)
            {
                isMaxArmor = false;
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("AddShield");
                _TrueGear.Play("AddShield");
            }
            oldValue = value;
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerData), "NetworkplayerState", MethodType.Setter)]
        private static void PlayerData_NetworkplayerState_Postfix(PlayerData __instance, PlayerState value)
        {
            if (!__instance.isLocalPlayer)
            {
                return;
            }
            if (value != PlayerState.Active)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("StopMeleeShield");
                _TrueGear.Play("StopMeleeShield");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerDefib), "State", MethodType.Setter)]
        private static void PlayerDefib_State_Postfix(PlayerDefib __instance, PlayerDefib.DefibState value)
        {
            if (!__instance.isLocalPlayer)
            {
                return;
            }
            if (!isRubbingDefib)
            {
                isRubbingDefib = true;
                Timer rubbingDefibTimer = new Timer(RubbingDefibTimerCallBack, null, 150, Timeout.Infinite);
                if (value == PlayerDefib.DefibState.Rubbing)
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("RubbingDefib");
                    _TrueGear.Play("RubbingDefib");
                }
            }            
            if (value == PlayerDefib.DefibState.Charged)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("ChargedDefib");
                _TrueGear.Play("ChargedDefib");
            }
        }
        private static void RubbingDefibTimerCallBack(System.Object o)
        {
            isRubbingDefib = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerDefib), "UserCode_RpcReviveEffects__UInt32__String__String__Vector3__Vector3")]
        private static void PlayerDefib_UserCode_RpcReviveEffects_Postfix(PlayerDefib __instance, uint revivedPlayerNetId, Vector3 position, Vector3 lookAt)
        {
            if (!__instance.isLocalPlayer)
            {
                return;
            }
            MelonLogger.Msg("------------------------------------------");
            MelonLogger.Msg("UsedDefib");
            _TrueGear.Play("UsedDefib");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerDropPod), "Update")]
        private static void PlayerDropPod_Update_Postfix(PlayerDropPod __instance)
        {
            if (__instance.attachedContainer != playerContainer)
            {
                return;
            }
            value = __instance.State;

            if (!isPodState)
            {
                if (value == PodState.GlidingOpened && !isFlying && !isFalling)
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("StartFall");
                    _TrueGear.StartFall();
                }
                isPodState = true;
                if (value == PodState.WaitingToLaunch)
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("intoPod");
                    _TrueGear.Play("intoPod");    //进入开局的发射仓
                }
                else if (value == PodState.Launching)
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("LaunchingPod");
                    _TrueGear.Play("LaunchingPod");       //发射！！！！
                }
                else if (value == PodState.WaitingToDrop)
                {
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("DuringPod");
                    _TrueGear.Play("DuringPod");       //在发射仓中等待跳机！！！！
                }
                Timer podStateTimer = new Timer(PodStateTimerCallBack, null, 500, Timeout.Infinite);
            }

            if (value == PodState.Impacted)
            {
                if ((__instance.attachedContainer.playerData.MotionState == MotionState.Idle || __instance.attachedContainer.playerData.MotionState == MotionState.Bipedal) && canFallDamage )
                {
                    canFallDamage = false;
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("FallDamage");
                    _TrueGear.Play("FallDamage");
                    MelonLogger.Msg("StopFall");
                    _TrueGear.StopFall();
                    _TrueGear.StopFly();
                    fallMillis = -1;
                }
            }                
        }
        private static void PodStateTimerCallBack(System.Object o)
        {
            isPodState = false;
        }


        [HarmonyPrefix, HarmonyPatch(typeof(PlayerInventory), "NetworkequipIndex", MethodType.Setter)]
        private static void PlayerInventory_NetworkequipIndex_Prefix(PlayerInventory __instance, int value)
        {
            if (!__instance.isLocalPlayer)
            {
                return;
            }
            PlayerContainer playerContainer = PlayerContainer.Find(__instance.netId);
            PlayerData playerData = (playerContainer != null) ? playerContainer.Data : null;
            if (!(playerData == null || !playerData.isLocalPlayer))
            {
                if (GlobalsListen.myDominantHand != Handedness.Unknown)
                {
                    int networkequipIndex = __instance.NetworkequipIndex;
                    if (value != networkequipIndex)
                    {
                        if (value == 0)
                        {
                            MelonLogger.Msg("------------------------------------------");
                            MelonLogger.Msg($"{GlobalsListen.myDominantHand}HandWeaponHide");
                            _TrueGear.Play($"{GlobalsListen.myDominantHand}HandWeaponHide");
                        }
                        else
                        {
                            MelonLogger.Msg("------------------------------------------");
                            MelonLogger.Msg($"{GlobalsListen.myDominantHand}HandWeaponSelected");
                            _TrueGear.Play($"{GlobalsListen.myDominantHand}HandWeaponSelected");      //手上出现物品
                        }
                    }
                }
                    
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerMelee), "OnWoosh")]
        private static void PlayerMelee_OnWoosh_Prefix(PlayerMelee __instance)
        {
            if (!__instance.isLocalPlayer)
            {
                return;
            }
            if (GlobalsListen.myDominantHand != Handedness.Unknown && !isWoosh)
            {
                isWoosh = true;
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg($"{GlobalsListen.myDominantHand}HandMeleeWoosh");
                _TrueGear.Play($"{GlobalsListen.myDominantHand}HandMeleeWoosh");
                Timer wooshTimer = new Timer(WooshTimerCallBack, null, 150, Timeout.Infinite);
            }
        }        
        private static void WooshTimerCallBack(System.Object o)
        {
            isWoosh = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerMelee), "OnHitDamageable")]
        private static void PlayerMelee_OnHitDamageable_Prefix(PlayerMelee __instance, IDamageable damageable, Collider col, Vector3 swipeDirection)
        {
            if (!__instance.isLocalPlayer)
            {
                return;
            }
            if (GlobalsListen.myDominantHand != Handedness.Unknown)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg($"{GlobalsListen.myDominantHand}HandMeleeHit");
                _TrueGear.Play($"{GlobalsListen.myDominantHand}HandMeleeHit");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ShakeConsumable), "HandleShake")]
        private static void ShakeConsumable_HandleShake_Postfix(ShakeConsumable __instance, InventorySlot equippedSlot)
        {
            if (equippedSlot.Info.Type != InventoryItemType.BuffShieldShaker)
            {
                return;
            }
            if (!isShakeConsumable)
            {
                isShakeConsumable = true;
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg($"{GlobalsListen.myDominantHand}HandShakeConsumable");
                _TrueGear.Play($"{GlobalsListen.myDominantHand}HandShakeConsumable");
                Timer shakeConsumableTimer = new Timer(ShakeConsumableTimerCallBack, null, 200, Timeout.Infinite);
            }
         }
        private static void ShakeConsumableTimerCallBack(System.Object o)
        {
            isShakeConsumable = false;
        }


        [HarmonyPostfix, HarmonyPatch(typeof(UnetGameManager), "OnGameStateChanged")]
        private static void UnetGameManager_OnGameStateChanged_Postfix(GameState oldValue, GameState newValue)
        {
            if (newValue != GameState.MatchEnded)
            {
                return;
            }
            MelonLogger.Msg("------------------------------------------");
            MelonLogger.Msg("LevelFinished");
            _TrueGear.Play("LevelFinished");
            _TrueGear.StopHeartBeat();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerData), "TwoHand", MethodType.Setter)]
        private static void PlayerData_TwoHand_Prefix(PlayerData __instance,bool value)
        {
            if (!__instance.isLocalPlayer)
            {
                return;
            }
            isTwoHand = value;
        }



    }
}