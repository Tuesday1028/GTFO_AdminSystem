using Clonesoft.Json;
using Enemies;
using GameData;
using Gear;
using Hikaria.AdminSystem.Managers;
using Hikaria.AdminSystem.Utilities;
using Hikaria.AdminSystem.Utility;
using Hikaria.QC;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using TheArchive.Core.ModulesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Weapon
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class WeaponAutoTrigger : Feature
    {
        public override string Name => "自动扳机";

        public override string Description => "使用枪械时启用自动扳机\n<color=red>本功能与自动瞄准冲突</color>";

        public override FeatureGroup Group => EntryPoint.Groups.Weapon;

        [FeatureConfig]
        public static WeaponAutoTriggerSettings Settings { get; set; }

        public class WeaponAutoTriggerSettings
        {
            [FSDisplayName("状态")]
            [FSDescription("枪械处于瞄准状态并瞄准敌人时自动开火")]
            [Command("AutoTrigger", MonoTargetType.Registry)]
            public bool Enabled { get; set; }

            [FSDisplayName("暂停自动扳机按键")]
            [FSDescription("按下后可暂停自动扳机，松开后恢复")]
            public KeyCode PauseAutoTriggerKey { get; set; } = KeyCode.LeftShift;

            [FSDisplayName("反转暂停自动扳机")]
            public bool ReversePauseAutoFire { get; set; }

            [FSDisplayName("装甲部位检测阈值")]
            [FSDescription("默认值为0.1")]
            [FSSlider(0f, 1f, FSSlider.SliderStyle.FloatTwoDecimal)]
            public float ArmorLimbDamageMultiThreshold
            {
                get
                {
                    return EnemyDataHelper.ArmorMultiThreshold;
                }
                set
                {
                    EnemyDataHelper.ArmorMultiThreshold = value;
                    EnemyDataHelper.ClearGeneratedEnemyDamageData();
                }
            }

            [FSInline]
            [JsonIgnore]
            [FSDisplayName("当前武器参数调节")]
            public List<WeaponAutoTriggerPreference> Preferences
            {
                get
                {
                    List<WeaponAutoTriggerPreference> result = new();
                    if (CurrentWeaponPref != null)
                    {
                        result.Add(CurrentWeaponPref);
                    }
                    return result;
                }
                set
                {
                }
            }
        }

        private static WeaponAutoTriggerPreference CurrentWeaponPref;

        [Localized]
        public enum AutoTriggerLogicType
        {
            Generic = 0,
            WeakspotOnly = 1,
            //NormalOnly = 2
        }

        private static CustomSetting<Dictionary<uint, WeaponAutoTriggerPreference>> PreferencesLookup = new("WeaponAutoTriggerPreferences.json", new());

        public class WeaponAutoTriggerPreference
        {
            public WeaponAutoTriggerPreference() { }
            public WeaponAutoTriggerPreference(ArchetypeDataBlock block)
            {
                ArchetypeDataID = block.persistentID;
                ArchetypeName = block.PublicName;
            }

            [FSReadOnly]
            [FSHeader("当前武器参数设置")]
            [FSDisplayName("武器ID")]
            public uint ArchetypeDataID { get; set; }
            [FSReadOnly]
            [FSDisplayName("武器名称")]
            public string ArchetypeName { get; set; }
            [FSDisplayName("自动开火逻辑")]
            public AutoTriggerLogicType AutoTriggerLogic { get; set; } = AutoTriggerLogicType.Generic;
            [FSDisplayName("可靠性")]
            [FSDescription("默认值为1")]
            [FSSlider(0, 1f, FSSlider.SliderStyle.FloatTwoDecimal)]
            public float Reliability { get; set; } = 1f;
            [FSDisplayName("衰减距离判定阈值")]
            [FSDescription("最大值为15")]
            [FSSlider(5f, 15f, FSSlider.SliderStyle.FloatOneDecimal)]
            public float FalloffThreshold { get; set; } = 12f;
            [FSDisplayName("伤害衰减阈值")]
            [FSDescription("默认值为0.25")]
            [FSSlider(0f, 1f, FSSlider.SliderStyle.FloatTwoDecimal)]
            public float DamageFalloffThreshold { get; set; } = 0.25f;
            [FSDisplayName("霰弹枪单次伤害阈值")]
            [FSDescription("默认值为0.75, 非霰弹类武器请忽略此项")]
            [FSSlider(0f, 1f, FSSlider.SliderStyle.FloatTwoDecimal)]
            public float ShotgunDamagePerFireThreshold { get; set; } = 0.75f;
        }

        private static bool IsWieldBulletWeapon;

        public override void Init()
        {
            QuantumRegistry.RegisterObject(Settings);
        }

        [ArchivePatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.OnWield))]
        private class BulletWeaponArchetype__OnWield__Patch
        {
            private static void Postfix(BulletWeaponArchetype __instance)
            {
                if (__instance.m_owner?.IsLocallyOwned ?? false)
                {
                    IsWieldBulletWeapon = true;

                    var data = __instance.m_archetypeData;
                    if (!PreferencesLookup.Value.TryGetValue(data.persistentID, out CurrentWeaponPref))
                    {
                        CurrentWeaponPref = new(data);
                        PreferencesLookup.Value.Add(data.persistentID, CurrentWeaponPref);
                    }
                }
            }
        }

        [ArchivePatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.OnUnWield))]
        private class BulletWeaponArchetype__OnUnWield__Patch
        {
            private static void Postfix(BulletWeaponArchetype __instance)
            {
                if (__instance.m_owner?.IsLocallyOwned ?? false)
                {
                    IsWieldBulletWeapon = false;
                    CurrentWeaponPref = null;
                }
            }
        }

        [ArchivePatch(typeof(ItemEquippable), nameof(ItemEquippable.FireButton), null, ArchivePatch.PatchMethodType.Getter)]
        private class ItemEquippable__get_FireButton__Patch
        {
            private static bool Prefix(ref bool __result)
            {
                if (!IsWieldBulletWeapon || !OverrideFireButton)
                    return ArchivePatch.RUN_OG;
                __result = true;
                return ArchivePatch.SKIP_OG;
            }
        }

        [ArchivePatch(typeof(ItemEquippable), nameof(ItemEquippable.FireButtonPressed), null, ArchivePatch.PatchMethodType.Getter)]
        private class ItemEquippable__get_FireButtonPressed__Patch
        {
            private static bool Prefix(ref bool __result)
            {
                if (!IsWieldBulletWeapon || !OverrideFireButtonPressed)
                    return ArchivePatch.RUN_OG;
                __result = true;
                return ArchivePatch.SKIP_OG;
            }
        }

        private static bool OverrideFireButton;
        private static bool OverrideFireButtonPressed;

        [ArchivePatch(typeof(BulletWeaponArchetype), nameof(BulletWeaponArchetype.Update))]
        private class BulletWeaponArchetype__Update__Patch
        {
            private static void Prefix(BulletWeaponArchetype __instance)
            {
                if (!Settings.Enabled)
                    return;

                var owner = __instance.m_owner;
                if (owner == null || !owner.IsLocallyOwned)
                    return;

                var weapon = __instance.m_weapon;
                if (weapon.m_clip <= 0)
                {
                    if (!weapon.m_inventory.CanReloadCurrent())
                        return;
                    else if (!weapon.IsReloading)
                        weapon.m_inventory.TriggerReload();
                    return;
                }

                if (!weapon.AimButtonHeld || weapon.FireButton || weapon.FireButtonPressed)
                    return;

                var pauseKeyPressed = Input.GetKey(Settings.PauseAutoTriggerKey);
                if ((Settings.ReversePauseAutoFire && !pauseKeyPressed) || (!Settings.ReversePauseAutoFire && pauseKeyPressed))
                    return;

                if (CurrentWeaponPref == null)
                    return;

                var camera = owner.FPSCamera;
                camera.UpdateCameraRay();

                float spread = 0f;
                var factor = Random.Range(0f, 1f);
                if (factor > CurrentWeaponPref.Reliability && Clock.Time - weapon.m_lastFireTime <= weapon.m_fireRecoilCooldown)
                {
                    spread = 0.025f;
                }

                if (!Physics.SphereCast(camera.Position, spread, camera.CameraRayDir, out var rayHit, weapon.MaxRayDist, LayerManager.MASK_BULLETWEAPON_RAY))
                    return;

                var rayObj = rayHit.collider.gameObject;
                if (rayObj == null)
                    return;

                var targetLimb = rayObj.GetComponent<Dam_EnemyDamageLimb>();
                if (targetLimb == null)
                    return;

                var dam = targetLimb.m_base;
                if (dam.IsImortal)
                    return;
                var isArmorLimb = targetLimb.m_type == eLimbDamageType.Armor && targetLimb.m_armorDamageMulti <= Settings.ArmorLimbDamageMultiThreshold;
                if (isArmorLimb)
                    return;

                var targetEnemy = targetLimb.m_base.Owner;
                if (!targetEnemy.Alive)
                    return;

                var damageData = EnemyDataHelper.GetOrGenerateEnemyDamageData(targetEnemy);
                if (damageData.IsImmortal)
                    return;

                var data = weapon.ArchetypeData;
                var fireDir = camera.CameraRayDir;
                var firePosition = camera.Position;
                var fireDistance = rayHit.distance;
                var shotgun = weapon.TryCast<Shotgun>();
                bool isShotgun = shotgun != null;
                float totalDamage = 0f;
                if (isShotgun)
                {
                    var up = weapon.MuzzleAlign.up;
                    var right = weapon.MuzzleAlign.right;
                    var id = targetEnemy.GlobalID;
                    float randomSpread = data.ShotgunBulletSpread;
                    float baseDamage = data.GetDamageWithBoosterEffect(owner, weapon.ItemDataBlock.inventorySlot);
                    baseDamage = AgentModifierManager.ApplyModifier(targetEnemy, AgentModifier.ProjectileResistance, baseDamage);
                    float expectTotalDamage = baseDamage * data.ShotgunBulletCount * CurrentWeaponPref.ShotgunDamagePerFireThreshold;
                    float realDamage = 0f;
                    float tempFireDistance = 0f;
                    for (int i = 0; i < data.ShotgunBulletCount; i++)
                    {
                        fireDir = camera.CameraRayDir;
                        float num = shotgun.m_segmentSize * i;
                        float angOffsetX = 0f;
                        float angOffsetY = 0f;
                        if (i > 0)
                        {
                            angOffsetX += data.ShotgunConeSize * Mathf.Cos(num);
                            angOffsetY += data.ShotgunConeSize * Mathf.Sin(num);
                        }
                        if (Mathf.Abs(angOffsetX) > 0f)
                        {
                            fireDir = Quaternion.AngleAxis(angOffsetX, up) * fireDir;
                        }
                        if (Mathf.Abs(angOffsetY) > 0f)
                        {
                            fireDir = Quaternion.AngleAxis(angOffsetY, right) * fireDir;
                        }
                        if (randomSpread > 0f)
                        {
                            Vector2 vector = UnityEngine.Random.insideUnitCircle * randomSpread;
                            fireDir = Quaternion.AngleAxis(vector.x, up) * fireDir;
                            fireDir = Quaternion.AngleAxis(vector.y, right) * fireDir;
                        }

                        if (!Physics.Raycast(firePosition, fireDir, out var tempRayHit, weapon.MaxRayDist, LayerManager.MASK_BULLETWEAPON_RAY))
                            continue;
                        var hitObj = tempRayHit.collider.gameObject;
                        if (hitObj == null)
                            continue;
                        var tempDam = hitObj.GetComponent<Dam_EnemyDamageLimb>();
                        if (tempDam == null || tempDam.m_base.Owner.GlobalID != id)
                            continue;

                        tempFireDistance = tempRayHit.distance;

                        realDamage = targetLimb.ApplyWeakspotAndArmorModifiers(baseDamage, data.PrecisionDamageMulti);
                        if (tempFireDistance > data.DamageFalloff.x)
                        {
                            realDamage *= Mathf.Max(1f - (tempFireDistance - data.DamageFalloff.x) / (data.DamageFalloff.y - data.DamageFalloff.x), BulletWeapon.s_falloffMin);
                        }
                        if (targetEnemy.EnemyBalancingData.AllowDamgeBonusFromBehind)
                        {
                            realDamage = targetLimb.ApplyDamageFromBehindBonus(realDamage, owner.Position, fireDir);
                        }

                        totalDamage += realDamage;
                    }

                    if (targetEnemy.IsScout)
                    {
                        var scream = targetEnemy.Locomotion.ScoutScream;
                        if (scream != null && scream.m_state != ES_ScoutScream.ScoutScreamState.Done)
                            return;
                    }

                    if (totalDamage >= expectTotalDamage)
                    {
                        goto fire;
                    }
                }
                else
                {
                    float realDamage = data.GetDamageWithBoosterEffect(owner, weapon.ItemDataBlock.inventorySlot);
                    realDamage = AgentModifierManager.ApplyModifier(targetEnemy, AgentModifier.ProjectileResistance, realDamage);
                    realDamage = targetLimb.ApplyWeakspotAndArmorModifiers(realDamage, data.PrecisionDamageMulti);
                    if (fireDistance > data.DamageFalloff.x)
                    {
                        realDamage *= Mathf.Max(1f - (fireDistance - data.DamageFalloff.x) / (data.DamageFalloff.y - data.DamageFalloff.x), BulletWeapon.s_falloffMin);
                    }
                    if (targetEnemy.EnemyBalancingData.AllowDamgeBonusFromBehind)
                    {
                        realDamage = targetLimb.ApplyDamageFromBehindBonus(realDamage, owner.Position, fireDir);
                    }
                    totalDamage = realDamage;
                }

                if (dam.WillDamageKill(totalDamage)) // 如果能一枪致死直接过
                {
                    goto fire;
                }

                if (fireDistance > (data.DamageFalloff.y - data.DamageFalloff.x) * CurrentWeaponPref.DamageFalloffThreshold + data.DamageFalloff.x)
                    return;

                if (!isShotgun)
                {
                    if (targetEnemy.IsScout)
                    {
                        var scream = targetEnemy.Locomotion.ScoutScream;
                        if (scream != null && scream.m_state != ES_ScoutScream.ScoutScreamState.Done)
                            return;
                    }

                    if (targetLimb.m_type == eLimbDamageType.Weakspot)
                    {
                        goto fire;
                    }

                    if (CurrentWeaponPref.AutoTriggerLogic != AutoTriggerLogicType.WeakspotOnly)
                    {
                        if (fireDistance <= Mathf.Min(CurrentWeaponPref.FalloffThreshold, data.DamageFalloff.x))
                        {
                            goto fire;
                        }
                    }

                    if (damageData.HasWeakSpot)
                    {
                        if (!__instance.HasChargeup)
                        {
                            foreach (var index in damageData.Weakspots.Keys)
                            {
                                var tempLimb = targetEnemy.Damage.DamageLimbs[index];
                                if (!tempLimb.IsDestroyed && AdminUtils.CanFireHitObject(firePosition, tempLimb.gameObject))
                                {
                                    return;
                                }
                            }
                        }
                    }
                    goto fire;
                }
                return;
            fire:
                OverrideFireButton = true;
                OverrideFireButtonPressed = true;
                return;
            }

            private static void Postfix(BulletWeaponArchetype __instance)
            {
                if (!Settings.Enabled)
                    return;

                var owner = __instance.m_owner;
                if (!owner?.IsLocallyOwned ?? true)
                    return;

                OverrideFireButtonPressed = false;
                OverrideFireButton = false;
            }
        }
    }
}
