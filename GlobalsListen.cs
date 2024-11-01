using Goyfs.Instance;
using Goyfs.Signal;
using MelonLoader;
using System.Collections.Generic;
using System.Threading;
using TrueGearSDK;
using UnhollowerRuntimeLib;
using UnityEngine;
using static MelonLoader.MelonLogger;
using static MelonLoader.Modules.MelonModule;
using static RootMotion.FinalIK.AimPoser;
using static RootMotion.FinalIK.HitReaction;
using static RootMotion.FinalIK.Recoil;


namespace PopulationOne_TrueGear
{
    public class GlobalsListen
    {
        public static Handedness myDominantHand = Handedness.Right;
        public static Handedness mySecondlyHand = Handedness.Left;

        public static int[] items = new int[30];

        public static Timer firearmPrimeCompleteTimer = null;
        public static bool canFirearmPrimeCompleteTimer = true;

        private static bool canDamage = true;

        public static void EventListen()
        {
            FirearmInsertAmmoCompleteSignal.AddLocalListener(new System.Action<uint>(delegate (uint netId) { FirearmInsertAmmoComplete(netId); }));
            PlayerWasHitSignal.AddLocalListener(new System.Action<uint, DamageableHitInfo>(delegate (uint netId, DamageableHitInfo dmgInfo){PlayerWasHit(netId, dmgInfo);}));            
            FirearmPrimeCompleteSignal.AddLocalListener(new System.Action<uint, int, int>(delegate (uint netId, int prime, int index){FirearmPrimeComplete(netId, prime, index);}));

            var playerDominantHandChangedSignal = SceneContext.Instance.InstanceBinder.Cast<InstanceBinder>().bindings[UnhollowerRuntimeLib.Il2CppType.Of<PlayerDominantHandChangedSignal>()].GetInstance().Cast<PlayerDominantHandChangedSignal>();
            playerDominantHandChangedSignal.AddListener(new System.Action<uint, Handedness>(delegate (uint netId, Handedness handedness) { ChangeHand(netId, handedness); }));

            var localFirearmFiredSignal = SceneContext.Instance.InstanceBinder.Cast<InstanceBinder>().bindings[UnhollowerRuntimeLib.Il2CppType.Of<LocalFirearmFiredSignal>()].GetInstance().Cast<LocalFirearmFiredSignal>();
            localFirearmFiredSignal.AddListener(new System.Action<uint, FirearmInfo, bool>(delegate (uint netId, FirearmInfo info, bool dominant) { FirearmFired(netId, info, dominant); }));

            var buildingBlockTriggerDown = SceneContext.Instance.InstanceBinder.Cast<InstanceBinder>().bindings[UnhollowerRuntimeLib.Il2CppType.Of<BuildingBlockTriggerDown>()].GetInstance().Cast<BuildingBlockTriggerDown>();
            buildingBlockTriggerDown.AddListener(new System.Action<uint>(delegate (uint netId) { BulidBlock(netId); }));

            var playerInventoryItemChangedSignal = SceneContext.Instance.InstanceBinder.Cast<InstanceBinder>().bindings[UnhollowerRuntimeLib.Il2CppType.Of<PlayerInventoryItemChangedSignal>()].GetInstance().Cast<PlayerInventoryItemChangedSignal>();
            playerInventoryItemChangedSignal.AddListener(new System.Action<uint, int, InventorySlot>(delegate (uint netId, int i, InventorySlot inventorySlot) { DropItem(netId,i, inventorySlot); }));

        }

        public static KeyValuePair<float, float> GetAngle2(Transform transform, DamageableHitInfo dmgInfo)
        {

            Vector3 playerToEnemy = dmgInfo.Position - transform.position;
            float angle = Vector3.SignedAngle(transform.forward, playerToEnemy, Vector3.up);
            angle = (angle + 360) % 360; // 确保角度在0到360之间

            float num2 = dmgInfo.ImpactPosition.y - transform.position.y;

            
            return new KeyValuePair<float, float>(angle, num2);
        }
        public static KeyValuePair<float, float> GetAngle1(Transform transform, DamageableHitInfo dmgInfo)
        {

            Vector3 playerToEnemy = dmgInfo.ImpactPosition - transform.position;
            float angle = Vector3.SignedAngle(transform.forward, playerToEnemy, Vector3.up);
            angle = (angle + 360) % 360; // 确保角度在0到360之间

            float num2 = dmgInfo.ImpactPosition.y - transform.position.y;

            
            return new KeyValuePair<float, float>(angle, num2);
        }
        public static KeyValuePair<float, float> GetAngle(Transform transform, DamageableHitInfo dmgInfo)
        {

            Vector3 playerToEnemy = dmgInfo.Forward - transform.forward;
            float angle = Vector3.SignedAngle(transform.forward, playerToEnemy, Vector3.up);

            if (angle < 0f)
            {
                angle = 360f + angle;
                if ((dmgInfo.Forward.x > 0f && transform.forward.x < 0f) || (dmgInfo.Forward.x < 0f && transform.forward.x > 0f))
                {
                    angle = angle - 180f;
                }
            }
            else
            {
                if (angle > 150f)
                {
                    angle = 180f - angle;
                }
            }

            float num2 = dmgInfo.ImpactPosition.y - transform.position.y;

            
            return new KeyValuePair<float, float>(angle, num2);
        }

        

        private static void PlayerWasHit(uint netId, DamageableHitInfo dmgInfo)
        {
            if (netId != Main.myNetId)
            {
                return;
            }
            if (!canDamage)
            {
                return;
            }
            canDamage = false;
            Timer damageTimer = new Timer(DamageTimerCallBack,null,80,Timeout.Infinite);
            Transform transform = GameObject.FindObjectOfType<PlayerCamera>().transform;
            var angle = GetAngle(transform,dmgInfo);
            
            switch (dmgInfo.Source)
            {
                case HitSourceCategory.Firearm:
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg($"PlayerBulletDamage,{angle.Key},{angle.Value}");
                    Main._TrueGear.PlayAngle("PlayerBulletDamage", angle.Key,angle.Value);
                    break;
                case HitSourceCategory.Melee:
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg($"MeleeDamage,{angle.Key},{angle.Value}");
                    Main._TrueGear.PlayAngle("DefaultDamage", angle.Key,angle.Value);
                    break;
                case HitSourceCategory.Falling:
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("FallDamage");
                    Main._TrueGear.Play("FallDamage");
                    break;
                case HitSourceCategory.BattleZone:
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg("BattleZoneDamage");
                    Main._TrueGear.Play("PoisonDamage");
                    break;
                case HitSourceCategory.Explosive:
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg($"ExplosiveDamage,{angle.Key},{angle.Value}");
                    Main._TrueGear.PlayAngle("DefaultDamage", angle.Key,angle.Value);
                    break;
                case HitSourceCategory.Unknown:
                    MelonLogger.Msg("------------------------------------------");
                    MelonLogger.Msg($"Damage,{angle.Key},{angle.Value}");
                    Main._TrueGear.PlayAngle("DefaultDamage", angle.Key,angle.Value);
                    break;
            }
            MelonLogger.Msg($"PlayerForward :{transform.forward.x},{transform.forward.y},{transform.forward.z}");
            MelonLogger.Msg($"dmginfoForward :{dmgInfo.Forward.x},{dmgInfo.Forward.y},{dmgInfo.Forward.z}");
            if (dmgInfo.ArmorBroke)    
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg("ShieldBroke");
                Main._TrueGear.Play("ShieldBroke");
            }
        }

        private static void DamageTimerCallBack(object o)
        {
            canDamage = true;
        }

        public static void FirearmPrimeComplete(uint netId, int prime, int index)
        {
            if (netId != Main.myNetId)
            {
                return;
            }
            
            //if (!canFirearmPrimeCompleteTimer)
            //{
            //    return;
            //}
            //canFirearmPrimeCompleteTimer = false;
            if (prime == 0)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg($"{myDominantHand}DownReload");
                Main._TrueGear.Play($"{myDominantHand}DownReload");
            }
            else
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg($"{mySecondlyHand}DownReload");
                Main._TrueGear.Play($"{mySecondlyHand}DownReload");
            }

            MelonLogger.Msg($"prime :{prime}");
            //Timer firearmPrimeCompleteTimer = new Timer(FirearmPrimeCompleteTimerCallBack,null,150,Timeout.Infinite);
        }

        private static void FirearmPrimeCompleteTimerCallBack(System.Object o)
        {
            canFirearmPrimeCompleteTimer = true;
        }

        private static void FirearmInsertAmmoComplete(uint netId)
        {
            if (netId != Main.myNetId)
            {
                return;
            }
            MelonLogger.Msg("------------------------------------------");
            MelonLogger.Msg($"{myDominantHand}ReloadAmmo");            
            Main._TrueGear.Play($"{myDominantHand}ReloadAmmo");
        }

        private static void FirearmFired(uint netId, FirearmInfo firearmInfo, bool dominant)
        {
            if (netId != Main.myNetId)
            {
                return;
            }
            if (Main.isTwoHand)
            {
                switch (firearmInfo.Class)
                {
                    case FirearmClass.Pistol:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg("TwoHandPistolShoot");
                        Main._TrueGear.Play("LeftHandPistolShoot");
                        Main._TrueGear.Play("RightHandPistolShoot");
                        break;
                    case FirearmClass.Shotgun:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg("TwoHandShotgunShoot");
                        Main._TrueGear.Play("LeftHandShotgunShoot");
                        Main._TrueGear.Play("RightHandShotgunShoot");
                        break;
                    case FirearmClass.SMG:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg("TwoHandSMGShoot");
                        Main._TrueGear.Play("LeftHandRifleShoot");
                        Main._TrueGear.Play("RightHandRifleShoot");
                        break;
                    case FirearmClass.AR:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg("TwoHandARShoot");
                        Main._TrueGear.Play("LeftHandRifleShoot");
                        Main._TrueGear.Play("RightHandRifleShoot");
                        break;
                    case FirearmClass.Sniper:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg("TwoHandSniperShoot");
                        Main._TrueGear.Play("LeftHandShotgunShoot");
                        Main._TrueGear.Play("RightHandShotgunShoot");
                        break;
                    case FirearmClass.Heavy:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg("TwoHandHeavyShoot");
                        Main._TrueGear.Play("LeftHandShotgunShoot");
                        Main._TrueGear.Play("RightHandShotgunShoot");
                        break;
                }
            }
            else if(dominant)
            {
                switch (firearmInfo.Class)
                {
                    case FirearmClass.Pistol:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{myDominantHand}HandPistolShoot");
                        Main._TrueGear.Play($"{myDominantHand}HandPistolShoot");
                        break;
                    case FirearmClass.Shotgun:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{myDominantHand}HandShotgunShoot");
                        Main._TrueGear.Play($"{myDominantHand}HandShotgunShoot");
                        break;
                    case FirearmClass.SMG:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{myDominantHand}HandSMGShoot");
                        Main._TrueGear.Play($"{myDominantHand}HandRifleShoot");
                        break;
                    case FirearmClass.AR:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{myDominantHand}HandARShoot");
                        Main._TrueGear.Play($"{myDominantHand}HandRifleShoot");
                        break;
                    case FirearmClass.Sniper:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{myDominantHand}HandSniperShoot");
                        Main._TrueGear.Play($"{myDominantHand}HandShotgunShoot");
                        break;
                    case FirearmClass.Heavy:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{myDominantHand}HandHeavyShoot");
                        Main._TrueGear.Play($"{myDominantHand}HandShotgunShoot");
                        break;
                }
            }
            else
            {
                switch (firearmInfo.Class)
                {
                    case FirearmClass.Pistol:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{mySecondlyHand}HandPistolShoot");
                        Main._TrueGear.Play($"{mySecondlyHand}HandPistolShoot");
                        break;
                    case FirearmClass.Shotgun:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{mySecondlyHand}HandShotgunShoot");
                        Main._TrueGear.Play($"{mySecondlyHand}HandShotgunShoot");
                        break;
                    case FirearmClass.SMG:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{mySecondlyHand}HandSMGShoot");
                        Main._TrueGear.Play($"{mySecondlyHand}HandRifleShoot");
                        break;
                    case FirearmClass.AR:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{mySecondlyHand}HandARShoot");
                        Main._TrueGear.Play($"{mySecondlyHand}HandRifleShoot");
                        break;
                    case FirearmClass.Sniper:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{mySecondlyHand}HandSniperShoot");
                        Main._TrueGear.Play($"{mySecondlyHand}HandShotgunShoot");
                        break;
                    case FirearmClass.Heavy:
                        MelonLogger.Msg("------------------------------------------");
                        MelonLogger.Msg($"{mySecondlyHand}HandHeavyShoot");
                        Main._TrueGear.Play($"{mySecondlyHand}HandShotgunShoot");
                        break;
                }
            }
            MelonLogger.Msg($"class :{firearmInfo.Class}");
            MelonLogger.Msg($"dominant :{dominant}");
        }

        private static void ChangeHand(uint netId, Handedness handedness)
        {
            if (netId != Main.myNetId)
            {
                return;
            }

            if (handedness == Handedness.Left)
            {
                myDominantHand = handedness;
                mySecondlyHand = Handedness.Right;
            }
            else
            {
                myDominantHand = handedness;
                mySecondlyHand = Handedness.Left;
            }
        }

        private static void BulidBlock(uint netId)
        {
            if (netId == Main.myNetId)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg($"{myDominantHand}HandBulidBlock");
                Main._TrueGear.Play($"{myDominantHand}HandPickupItem");
            }
        }

        private static void DropItem(uint netId, int index, InventorySlot inventorySlot)
        {
            if (netId != Main.myNetId)
            {
                return;
            }
            if (items[index] > inventorySlot.Quantity)
            {
                MelonLogger.Msg("------------------------------------------");
                MelonLogger.Msg($"{myDominantHand}HandDropItem");
                Main._TrueGear.Play($"{myDominantHand}HandPickupItem");
            }
            items[index] = inventorySlot.Quantity;
        }
    }
}
