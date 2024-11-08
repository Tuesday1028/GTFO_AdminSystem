using Enemies;
using Gear;
using Hikaria.AdminSystem.Features.Player;
using Hikaria.AdminSystem.Managers;
using Hikaria.AdminSystem.Utility;
using Hikaria.DevConsoleLite;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Weapon
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class WeaponAutoTrigger : Feature
    {
        public override string Name => "自动扳机";

        public override string Description => "使用枪械时启用自动扳机";

        public override FeatureGroup Group => EntryPoint.Groups.Weapon;

        [FeatureConfig]
        public static WeaponAutoTriggerSettings Settings { get; set; }

        public class WeaponAutoTriggerSettings
        {

            [FSDisplayName("状态")]
            [FSDescription("枪械处于瞄准状态并瞄准敌人时自动开火")]
            public bool Enabled { get; set; }

            [FSDisplayName("暂停自动扳机按键")]
            [FSDescription("按下后可暂停自动扳机，松开后恢复")]
            public KeyCode PauseAutoFireKey { get; set; } = KeyCode.LeftShift;

            [FSDisplayName("反转暂停自动扳机")]
            public bool ReversePauseAutoFire { get; set; }

            [FSDisplayName("蓄力保持时间")]
            [FSDescription("默认值为0.5")]
            [FSSlider(0f, 1f, FSSlider.SliderStyle.FloatTwoDecimal)]
            public float ChargeupTimer { get; set; } = 0.5f;

            [FSDisplayName("衰减距离判定阈值")]
            [FSDescription("默认值为15")]
            [FSSlider(5f, 15f, FSSlider.SliderStyle.FloatOneDecimal)]
            public float FalloffThreshold { get; set; } = 15f;

            [FSDisplayName("伤害衰减阈值")]
            [FSDescription("默认值为0.25")]
            [FSSlider(0f, 1f, FSSlider.SliderStyle.FloatTwoDecimal)]
            public float DamageFalloffThreshold { get; set; } = 0.25f;

            [FSDisplayName("霰弹枪单次伤害阈值")]
            [FSDescription("默认值为0.75")]
            [FSSlider(0f, 1f, FSSlider.SliderStyle.FloatTwoDecimal)]
            public float ShotgunDamagePerFireThreshold { get; set; } = 0.75f;

            [FSDisplayName("装甲部位检测阈值")]
            [FSDescription("默认值为0.1")]
            [FSSlider(0f, 1f, FSSlider.SliderStyle.FloatTwoDecimal)]
            public float ArmorLimbDamageMultiThreshold
            {
                get
                {
                    return EnemyDataManager.ArmorMultiThreshold;
                }
                set
                {
                    EnemyDataManager.ArmorMultiThreshold = value;
                    EnemyDataManager.ClearGeneratedEnemyDamageData();
                }
            }
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("AutoTrigger", "自动扳机", "自动扳机", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.Enabled;
                }
                Settings.Enabled = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 自动扳机");
            }, () =>
            {
                DevConsole.LogVariable("自动扳机", Settings.Enabled);
            }));
        }

        private static bool IsWieldBulletWeapon;
        private static float StopChargeTimer;
        private static float FireDelayTimer;
        private static EnemyAgent CurrentTargetEnemy;

        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.OnWield))]
        private class BulletWeapon__OnWield__Patch
        {
            private static void Postfix(BulletWeapon __instance)
            {
                if (__instance.Owner?.IsLocallyOwned ?? false)
                {
                    IsWieldBulletWeapon = true;
                }
            }
        }

        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.OnUnWield))]
        private class BulletWeapon__OnUnWield__Patch
        {
            private static void Prefix(BulletWeapon __instance)
            {
                if (__instance.Owner?.IsLocallyOwned ?? false)
                {
                    IsWieldBulletWeapon = false;
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

                if (weapon.m_clip <= 0 && !weapon.m_inventory.CanReloadCurrent())
                    return;

                // 非瞄准、开火时忽略
                if (!weapon.AimButtonHeld || weapon.FireButton || weapon.FireButtonPressed)
                    return;

                var pauseKeyPressed = Input.GetKey(Settings.PauseAutoFireKey);
                if ((Settings.ReversePauseAutoFire && !pauseKeyPressed) || (!Settings.ReversePauseAutoFire && pauseKeyPressed))
                    return;

                var camera = owner.FPSCamera;
                if (!Physics.Raycast(camera.Position, camera.CameraRayDir, out var rayHit, weapon.MaxRayDist, LayerManager.MASK_BULLETWEAPON_RAY))
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

                if (OneShotKill.OneShotKillLookup.TryGetValue(SNet.LocalPlayer.Lookup, out var tentry) && tentry.EnableOneShotKill)
                {
                    goto fire;
                }

                var damageData = EnemyDataManager.GetOrGenerateEnemyDamageData(targetEnemy);
                if (damageData.IsImmortal)
                    return;

                var firePosition = camera.Position;
                var fireDir = camera.CameraRayDir;
                var fireDistance = rayHit.distance;
                var data = weapon.ArchetypeData;
                var shotgun = weapon.TryCast<Shotgun>();
                bool isShotgun = shotgun != null;
                float totalDamage = 0f;
                if (isShotgun)
                {
                    var id = targetEnemy.GlobalID;
                    Vector3 up = weapon.MuzzleAlign.up;
                    Vector3 right = weapon.MuzzleAlign.right;
                    float randomSpread = data.ShotgunBulletSpread;
                    float baseDamage = data.GetDamageWithBoosterEffect(owner, weapon.ItemDataBlock.inventorySlot);
                    baseDamage = AgentModifierManager.ApplyModifier(targetEnemy, AgentModifier.ProjectileResistance, baseDamage);
                    float expectTotalDamage = baseDamage * data.ShotgunBulletCount * Settings.ShotgunDamagePerFireThreshold;
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

                if (fireDistance > (data.DamageFalloff.y - data.DamageFalloff.x) * Settings.DamageFalloffThreshold + data.DamageFalloff.x)
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

                    if (fireDistance <= Mathf.Min(Settings.FalloffThreshold, data.DamageFalloff.x))
                    {
                        goto fire;
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
                if (owner == null || !owner.IsLocallyOwned)
                    return;

                OverrideFireButtonPressed = false;
                OverrideFireButton = false;
            }
        }
    }
}
