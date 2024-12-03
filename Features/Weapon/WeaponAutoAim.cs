using AIGraph;
using Enemies;
using GameData;
using Gear;
using Hikaria.AdminSystem.Features.Player;
using Hikaria.AdminSystem.Managers;
using Hikaria.AdminSystem.Utility;
using Hikaria.QC;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using TheArchive.Core.Models;
using TheArchive.Loader;
using TheArchive.Utilities;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Weapon
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class WeaponAutoAim : Feature
    {
        public override string Name => "自瞄";

        public override string Description => "使用枪械时启用自瞄";

        public override FeatureGroup Group => EntryPoint.Groups.Weapon;

        [FeatureConfig]
        public static WeaponAutoAimSettings Settings { get; set; }

        public class WeaponAutoAimSettings
        {
            [FSDisplayName("启用自瞄")]
            public bool EnableAutoAim { get => _enableAutoAim; set => _enableAutoAim = value; }

            [FSDisplayName("弹道修正")]
            public bool EnableTrajectoryRedirection { get; set; }

            [FSDisplayName("隔墙自瞄")]
            public bool WallHackAim { get; set; }

            [FSDisplayName("追踪子弹")]
            [FSDescription("仅适用于穿透子弹")]
            public bool MagicBullet { get; set; } = true;

            [FSDisplayName("追踪子弹忽略不可见")]
            public bool MagicBulletVisibleOnly { get; set; } = true;

            [FSDisplayName("追踪子弹最大修正角度")]
            public float MagicBulletMaxCorrectionAngle { get; set; } = 30f;

            [FSDisplayName("自瞄节点距离")]
            [FSDescription("默认为3个节点")]
            public int AutoAimNodeRange { get; set; } = 3;

            [FSDisplayName("自瞄模式")]
            [FSDescription("准心优先 或 近处优先")]
            public AutoAimMode AimMode { get; set; } = AutoAimMode.Crosshair;

            [FSDisplayName("暂停自瞄按键")]
            [FSDescription("按下后可暂停自瞄，松开后恢复")]
            public KeyCode PauseAutoAimKey { get; set; } = KeyCode.LeftShift;

            [FSDisplayName("反转暂停自瞄")]
            public bool ReversePauseAutoAim { get; set; }

            [FSDisplayName("自动开火模式")]
            public AutoFireMode AutoFire { get; set; } = AutoFireMode.Off;

            [FSDisplayName("自瞄范围半径")]
            [FSDescription("单位: 像素")]
            public float AimRadius { get; set; } = 540f;

            [FSDisplayName("全范围自瞄")]
            public bool IgnoreAimRadius { get; set; } = false;

            [FSDisplayName("装甲部位检测阈值")]
            [FSDescription("默认值为0.1")]
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

            [FSHeader("颜色设置")]
            [FSDisplayName("目标通用颜色")]
            public SColor TargetedColor { get; set; } = new(1.2f, 0.3f, 0.1f, 1f);
            [FSDisplayName("目标弱点颜色")]
            public SColor TargetedWeakspotColor { get; set; } = new(1.2f, 0.6f, 0.0f, 1f);
            [FSDisplayName("无目标颜色")]
            public SColor UnTargetedColor { get; set; } = new(0.3f, 0.1f, 0.1f, 1f);
            [FSDisplayName("非活跃颜色")]
            public SColor PassiveDetection { get; set; } = new(0.5f, 0.5f, 0.5f, 0.5f);

            [Localized]
            public enum AutoAimMode
            {
                Crosshair,
                Closest
            }

            [Localized]
            public enum AutoFireMode
            {
                Off,
                SemiAuto,
                FullyAuto
            }
        }

        [Command("AutoAim")]
        private static bool _enableAutoAim;

        public override void Init()
        {
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<WeaponAutoAimHandler>();
        }

        public override void OnGameStateChanged(int state)
        {
            var stateName = (eGameStateName)state;
            if (stateName == eGameStateName.AfterLevel || stateName == eGameStateName.NoLobby || stateName == eGameStateName.Lobby || stateName == eGameStateName.ExpeditionFail)
            {
                foreach (var autoaim in WeaponAutoAimHandler.AllAutoAimInstances)
                {
                    if (autoaim != null)
                        autoaim.DoAfterLevelClear();
                }
            }
        }

        private static bool IsWeaponOwner(BulletWeapon bulletWeapon)
        {
            if (bulletWeapon == null || bulletWeapon.Owner == null)
            {
                return false;
            }
            return bulletWeapon.Owner.IsLocallyOwned;
        }

        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.OnWield))]
        private class BulletWeapon__OnWield__Patch
        {
            private static void Postfix(BulletWeapon __instance)
            {
                if (!_enableAutoAim)
                    return;
                if (GameStateManager.Current.m_currentStateName != eGameStateName.InLevel)
                {
                    return;
                }
                if (!IsWeaponOwner(__instance))
                {
                    return;
                }
                WeaponAutoAimHandler weaponAutoAim = __instance.gameObject.GetComponent<WeaponAutoAimHandler>();
                if (weaponAutoAim == null)
                {
                    weaponAutoAim = __instance.gameObject.AddComponent<WeaponAutoAimHandler>();
                }
                weaponAutoAim.Setup(__instance, __instance.Owner, __instance.Owner.FPSCamera);
                weaponAutoAim.enabled = _enableAutoAim;
            }
        }

        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.OnUnWield))]
        public class BulletWeapon__OnUnWield__Patch
        {
            private static void Prefix(BulletWeapon __instance)
            {
                if (GameStateManager.Current.m_currentStateName != eGameStateName.InLevel)
                {
                    return;
                }
                if (!IsWeaponOwner(__instance))
                {
                    return;
                }
                WeaponAutoAimHandler weaponAutoAim = __instance.gameObject.GetComponent<WeaponAutoAimHandler>();
                if (weaponAutoAim == null)
                {
                    return;
                }
                weaponAutoAim.DoClear();
                weaponAutoAim.enabled = false;
            }
        }


        [ArchivePatch(typeof(PlayerEnemyCollision), nameof(PlayerEnemyCollision.FindNearbyEnemiesMovementReduction))]
        public class PlayerEnemyCollision__FindNearbyEnemiesMovementReduction__Patch
        {
            private static bool Prefix(PlayerEnemyCollision __instance, Vector3 pos, ref float __result)
            {
                if (!_enableAutoAim)
                    return true;
                float num = 1f;
                if (__instance.m_owner.CourseNode == null)
                {
                    __result = num;
                    return false;
                }
                __instance.m_enemies.Clear();
                AIG_CourseNode.GetEnemiesInNodes(__instance.m_owner.CourseNode, Settings.AutoAimNodeRange, __instance.m_enemies);
                for (int i = 0; i < __instance.m_enemies.Count; i++)
                {
                    EnemyAgent enemyAgent = __instance.m_enemies[i];
                    Vector3 vector = enemyAgent.Position - pos;
                    if (enemyAgent.Alive && vector.magnitude < enemyAgent.EnemyBalancingData.EnemyCollisionRadius)
                    {
                        float enemyCollisionPlayerMovementReduction = enemyAgent.EnemyBalancingData.EnemyCollisionPlayerMovementReduction;
                        float num2 = num - enemyCollisionPlayerMovementReduction;
                        float enemyCollisionMinimumMoveSpeedModifier = enemyAgent.EnemyBalancingData.EnemyCollisionMinimumMoveSpeedModifier;
                        num = Mathf.Min(num, Mathf.Max(num2, enemyCollisionMinimumMoveSpeedModifier));
                    }
                }
                __result = num;
                return false;
            }
        }

        static bool IsLocalShotgunFireShots;

        [ArchivePatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        private class Shotgun__Fire__Patch
        {
            private static void Prefix(Shotgun __instance)
            {
                if (!_enableAutoAim)
                    return;
                if (__instance.Owner.IsLocallyOwned)
                    IsLocalShotgunFireShots = true;
            }

            private static void Postfix(Shotgun __instance)
            {
                if (!_enableAutoAim)
                    return;
                if (__instance.Owner.IsLocallyOwned)
                    IsLocalShotgunFireShots = false;
            }
        }

        [ArchivePatch(typeof(global::Weapon), nameof(global::Weapon.CastWeaponRay))]
        public class Weapon__CastWeaponRay__Patch
        {
            public static Type[] ParameterTypes() => new Type[]
            {
                typeof(Transform),
                typeof(global::Weapon.WeaponHitData).MakeByRefType(),
                typeof(Vector3),
                typeof(int)
            };

            private static void Prefix(ref global::Weapon.WeaponHitData weaponRayData, Vector3 originPos)
            {
                if (!_enableAutoAim)
                    return;
                if (IsLocalShotgunFireShots) // 霰弹枪开火不会设置Owner
                {
                    weaponRayData.owner = AdminUtils.LocalPlayerAgent;
                }
                else if (!weaponRayData.owner?.IsLocallyOwned ?? true)
                {
                    return;
                }
                ArchetypeDataBlock archetypeData = weaponRayData.owner.Inventory.WieldedItem?.ArchetypeData ?? null;
                if (archetypeData == null)
                    return;
                if (!WeaponAutoAimHandler.TryGetInstance(archetypeData.persistentID, out WeaponAutoAimHandler weaponAutoAim))
                {
                    weaponAutoAim = weaponRayData.owner.Inventory.WieldedItem.GetComponent<WeaponAutoAimHandler>();
                    if (weaponAutoAim == null)
                        return;
                    WeaponAutoAimHandler.Register(archetypeData.persistentID, weaponAutoAim);
                }
                if (!Settings.EnableTrajectoryRedirection || weaponAutoAim.PauseAutoAim)
                    return;
                bool skipForceUpdate = false;
                if (!weaponAutoAim.HasTarget)
                {
                    if (!IsLocalShotgunFireShots && !(Settings.MagicBullet && weaponAutoAim.IsPiercingBullet))
                        return;
                    weaponAutoAim.ForceUpdate(originPos);
                    if (!weaponAutoAim.HasTarget)
                        return;
                    skipForceUpdate = true;
                }
                if (!skipForceUpdate && (IsLocalShotgunFireShots || (Settings.MagicBullet && weaponAutoAim.IsPiercingBullet)))
                    weaponAutoAim.ForceUpdate(originPos);
                if (!weaponAutoAim.HasTarget)
                    return;
                if (!InputMapper.GetButtonKeyMouse(InputAction.Aim, eFocusState.FPS) && Settings.AutoFire != WeaponAutoAimSettings.AutoFireMode.FullyAuto)
                    return;

                Vector3 originalFireDir = weaponRayData.fireDir;

                weaponRayData.randomSpread = 0;
                weaponRayData.angOffsetX = 0;
                weaponRayData.angOffsetY = 0;
                weaponRayData.maxRayDist = 2000f;
                if (Settings.EnableTrajectoryRedirection)
                {
                    weaponRayData.fireDir = (weaponAutoAim.AimTargetPos - originPos).normalized;
                }
                if (!IsLocalShotgunFireShots)
                {
                    weaponAutoAim.TempIgnoreTargetEnemy();
                }
                weaponAutoAim.SetLastFireDir(IsLocalShotgunFireShots ? originalFireDir : weaponRayData.fireDir);
            }

            //private static void Postfix(ref global::Weapon.WeaponHitData weaponRayData)
            //{
            //    // 用于修正子弹击发特效的方向，击发特效在命中判定完成之后
            //    if (fireDirModified)
            //    {
            //        weaponRayData.fireDir = weaponRayData.owner.FPSCamera.CameraRayDir;
            //        fireDirModified = false;
            //    }
            //}
        }

        public class WeaponAutoAimHandler : MonoBehaviour
        {
            public static void Register(uint persistentID, WeaponAutoAimHandler weaponAutoAim)
            {
                AutoAimInstances.Add(persistentID, weaponAutoAim);
            }

            public static void Unregister(uint persistentID)
            {
                AutoAimInstances.Remove(persistentID);
            }

            public static bool TryGetInstance(uint persistentID, out WeaponAutoAimHandler weaponAutoAim)
            {
                return AutoAimInstances.TryGetValue(persistentID, out weaponAutoAim);
            }

            public Vector3 AimTargetPos
            {
                get
                {
                    if (!_enableAutoAim)
                    {
                        return m_Owner.FPSCamera.CameraRayPos;
                    }
                    if (HasTarget)
                    {
                        return m_TargetLimb.DamageTargetPos;
                    }
                    //else if (m_Target != null)
                    //{
                    //    return m_Target.AimTarget.position;
                    //}
                    return m_Owner.FPSCamera.CameraRayPos;
                }
            }

            private void Awake()
            {
                Current = this;
                AllAutoAimInstances.Add(this);
            }

            private void OnDestroy()
            {
                AllAutoAimInstances.Remove(this);
                if (m_Reticle != null)
                    m_Reticle.SetVisible(false, false);
                m_HasTarget = false;
                m_Target = null;
                m_Reticle.SafeDestroy();
                m_ReticleHolder.SafeDestroy();
            }

            public void Setup(BulletWeapon weapon, PlayerAgent owner, FPSCamera camera)
            {
                if (m_Owner == null)
                {
                    m_BulletWeapon = weapon;
                    m_Owner = owner;
                    m_PlayerCamera = camera.gameObject.GetComponent<Camera>();
                    SetupReticle();
                }
                enabled = _enableAutoAim;
            }

            public void DoClear()
            {
                if (m_Reticle != null)
                    m_Reticle?.SetVisible(false, false);
                m_HasTarget = false;
                m_Target = null;
                Unregister(m_BulletWeapon.ArchetypeData.persistentID);
            }

            public void DoAfterLevelClear()
            {
                DoClear();
                m_Reticle.SafeDestroy();
                m_ReticleHolder.SafeDestroy();
                this.SafeDestroy();
            }

            private void SetupReticle()
            {
                m_ReticleHolder = new GameObject();
                m_ReticleHolder.transform.SetParent(GuiManager.CrosshairLayer.CanvasTrans);
                m_Reticle = Instantiate(GuiManager.CrosshairLayer.m_hitIndicatorFriendly, m_ReticleHolder.transform);
                m_Reticle.name = "AutoAimIndicator";
                m_Reticle.transform.localScale = Vector3.zero;
                SetVFX(Settings.TargetedColor.ToUnityColor(), m_TargetedEulerAngles);
                m_Reticle.transform.localEulerAngles = m_TargetedEulerAngles;
            }

            private void Update()
            {
                UpdateTargetEnemy(m_Owner.FPSCamera.Position);
                UpdateColor();
                UpdateAutoFire();

                IgnoredEnemies.Clear();
                LastFireDir = Vector3.zero;
            }


            public void ForceUpdate(Vector3 sourcePos)
            {
                if (PauseAutoAim)
                    return;
                UpdateTargetEnemy(sourcePos, true);
                UpdateColor();
            }

            private void UpdateTargetEnemy(Vector3 sourcePos, bool force = false)
            {
                if (PauseAutoAim)
                {
                    m_Target = null;
                    m_TargetLimb = null;
                    m_HasTarget = false;
                    m_Reticle.transform.localScale = Vector3.zero;
                    return;
                }
                if (m_Target != null && m_TargetLimb != null)
                {
                    m_ReticleHolder.transform.position = m_PlayerCamera.WorldToScreenPoint(m_TargetLimb.DamageTargetPos);
                    if (!m_HasTarget)
                    {
                        m_Reticle.transform.localScale = Vector3.one * 2f;
                        m_Reticle.transform.localEulerAngles = m_TargetedEulerAngles;
                        m_Reticle.AnimateScale(1.5f, 0.13f);
                        m_HasTarget = true;
                    }
                }
                else
                {
                    m_Reticle.transform.localEulerAngles += new Vector3(0f, 0f, 5f);
                }
                if (m_HasTarget && (m_Target == null || m_TargetLimb == null))
                {
                    m_Reticle.AnimateScale(0f, 0.5f);
                    m_HasTarget = false;
                }
                updateTick += Time.deltaTime;
                if (updateTick >= 0.04f || force)
                {
                    UpdateAroundEnemy(sourcePos);
                    UpdateBestEnemyTarget(sourcePos);
                    UpdateTargetEnemyLimb(sourcePos);
                    updateTick = 0f;
                }
            }

            private void UpdateColor()
            {
                if (PauseAutoAim)
                    return;
                if (m_HasTarget)
                {
                    m_Reticle.m_hitColor = m_TargetLimb == null ? Settings.UnTargetedColor.ToUnityColor() : (m_TargetLimb.m_type == eLimbDamageType.Weakspot ? Settings.TargetedWeakspotColor.ToUnityColor() : Settings.TargetedColor.ToUnityColor());
                }
                else
                {
                    m_Reticle.m_hitColor = Settings.UnTargetedColor.ToUnityColor();
                }
                if (!InputMapper.GetButtonKeyMouse(InputAction.Aim, eFocusState.FPS) || m_BulletWeapon.GetCurrentClip() <= 0 || PauseAutoAim)
                {
                    m_ReticleHolder.transform.localScale = Vector3.one * 0.5f;
                    m_ReticleHolder.transform.localEulerAngles += new Vector3(0f, 0f, 2f);
                    m_Reticle.m_hitColor = Settings.PassiveDetection.ToUnityColor();
                }
                else
                {
                    m_ReticleHolder.transform.localScale = Vector3.one;
                    m_ReticleHolder.transform.localEulerAngles = Vector3.zero;
                }
                m_Reticle.UpdateColorsWithAlphaMul(m_Reticle.m_hitColor.a);
            }

            private void SetVFX(Color color, Vector3 euler)
            {
                m_Reticle.transform.localEulerAngles = euler;
                m_Reticle.m_hitColor = color;
                m_Reticle.UpdateColorsWithAlphaMul(1f);
            }

            private void UpdateTargetEnemyLimb(Vector3 sourcePos)
            {
                if (m_Target == null || PauseAutoAim)
                {
                    m_TargetLimb = null;
                    return;
                }

                EnemyDataHelper.EnemyDamageData data = EnemyDataHelper.GetOrGenerateEnemyDamageData(m_Target);

                if (data.IsImmortal)
                {
                    if (OneShotKill.OneShotKillLookup.TryGetValue(SNet.LocalPlayer.Lookup, out var enable) && enable)
                    {
                        foreach (var index in data.Armorspots)
                        {
                            Dam_EnemyDamageLimb limb = m_Target.Damage.DamageLimbs[index.Key];
                            if (!limb.IsDestroyed && AdminUtils.CanFireHitObject(sourcePos, limb.gameObject))
                            {
                                m_TargetLimb = limb;
                                return;
                            }
                        }
                    }
                    m_TargetLimb = null;
                    return;
                }
                if (data.HasWeakSpot)
                {
                    foreach (var index in data.Weakspots)
                    {
                        Dam_EnemyDamageLimb limb = m_Target.Damage.DamageLimbs[index.Key];
                        if (!limb.IsDestroyed && AdminUtils.CanFireHitObject(sourcePos, limb.gameObject))
                        {
                            m_TargetLimb = limb;
                            return;
                        }
                    }
                }
                if (data.HasNormalSpot)
                {
                    foreach (var index in data.Normalspots)
                    {
                        Dam_EnemyDamageLimb limb = m_Target.Damage.DamageLimbs[index.Key];
                        if (!limb.IsDestroyed && AdminUtils.CanFireHitObject(sourcePos, limb.gameObject))
                        {
                            m_TargetLimb = limb;
                            return;
                        }
                    }
                }
                if (data.HasArmorSpot)
                {
                    foreach (var index in data.Armorspots)
                    {
                        Dam_EnemyDamageLimb limb = m_Target.Damage.DamageLimbs[index.Key];
                        if (!limb.IsDestroyed && AdminUtils.CanFireHitObject(sourcePos, limb.gameObject))
                        {
                            m_TargetLimb = limb;
                            return;
                        }
                    }
                }
                if (data.HasRealArmorSpot && OneShotKill.OneShotKillLookup.TryGetValue(SNet.LocalPlayer.Lookup, out var enable2) && enable2)
                {
                    foreach (var index in data.RealArmorSpots)
                    {
                        Dam_EnemyDamageLimb limb = m_Target.Damage.DamageLimbs[index.Key];
                        if (!limb.IsDestroyed && AdminUtils.CanFireHitObject(sourcePos, limb.gameObject))
                        {
                            m_TargetLimb = limb;
                            return;
                        }
                    }
                }
                m_TargetLimb = null;
            }

            private void UpdateAutoFire(bool force = false)
            {
                if (PauseAutoAim)
                    return;
                if (HasTarget && ((Settings.AutoFire == WeaponAutoAimSettings.AutoFireMode.SemiAuto && InputMapper.GetButtonKeyMouse(InputAction.Aim, eFocusState.FPS)) || Settings.AutoFire == WeaponAutoAimSettings.AutoFireMode.FullyAuto) && m_BulletWeapon.GetCurrentClip() > 0)
                {
                    fireTimer -= Time.deltaTime;
                    if (fireTimer <= 0 || force)
                    {
                        m_BulletWeapon.Fire(true);
                        m_BulletWeapon.TriggerSingleFireAudio();
                        fireTimer = m_BulletWeapon.ArchetypeData.ShotDelay;
                    }
                    return;
                }
                fireTimer = 0;
            }

            private void UpdateBestEnemyTarget(Vector3 sourcePos)
            {
                if (AroundEnemies == null || AroundEnemies.Count == 0 || PauseAutoAim)
                {
                    m_Target = null;
                    return;
                }
                EnemyAgent target = null;
                float tempRange = 100000f;
                foreach (EnemyAgent enemy in AroundEnemies)
                {
                    if (IsPiercingBullet && Settings.MagicBullet && LastFireDir != Vector3.zero)
                    {
                        float angle = Math.Abs(Vector3.Angle(enemy.AimTarget.position - sourcePos, LastFireDir));
                        if (angle > Settings.MagicBulletMaxCorrectionAngle)
                        {
                            continue;
                        }
                    }
                    switch (Settings.AimMode)
                    {
                        default:
                        case WeaponAutoAimSettings.AutoAimMode.Crosshair:
                            Vector3 scrPos = m_Owner.FPSCamera.m_camera.WorldToScreenPoint(enemy.AimTarget.position);
                            Vector2 center = new(Screen.width / 2, Screen.height / 2);
                            float distance = Vector3.Distance(scrPos, center);
                            Vector3 vec = enemy.AimTarget.position - sourcePos;
                            if (Settings.IgnoreAimRadius || (scrPos.z > 0 && distance <= Settings.AimRadius))
                            {
                                if (distance < tempRange)
                                {
                                    tempRange = distance;
                                    target = enemy;
                                }
                            }
                            break;
                        case WeaponAutoAimSettings.AutoAimMode.Closest:
                            float distance2 = Vector3.Distance(enemy.AimTarget.position, m_Owner.Position);
                            Vector3 vec2 = enemy.AimTarget.position - m_Owner.FPSCamera.Position;
                            Vector3 scrPos2 = m_Owner.FPSCamera.m_camera.WorldToScreenPoint(enemy.AimTarget.position);
                            Vector2 center2 = new(Screen.width / 2, Screen.height / 2);
                            float distance3 = Vector3.Distance(scrPos2, center2);
                            if (Settings.IgnoreAimRadius || (scrPos2.z > 0 && distance3 <= Settings.AimRadius))
                            {
                                if (distance2 < tempRange)
                                {
                                    tempRange = distance2;
                                    target = enemy;
                                }
                            }
                            break;
                    }
                }
                m_Target = target;
            }

            private void UpdateAroundEnemy(Vector3 sourcePos)
            {
                AroundEnemies.Clear();
                if (PauseAutoAim)
                    return;

                if (Settings.MagicBulletVisibleOnly)
                    sourcePos = m_Owner.FPSCamera.Position;

                foreach (EnemyAgent enemy in m_Owner.EnemyCollision.m_enemies)
                {
                    if (IgnoredEnemies.Contains(enemy))
                        continue;
                    if (Settings.WallHackAim || AdminUtils.CanSeeEnemyPlus(sourcePos, enemy))
                    {
                        if (enemy.Alive && enemy.Damage.Health > 0f)
                        {
                            if (enemy.Damage.IsImortal)
                            {
                                continue;
                            }
                            AroundEnemies.Add(enemy);
                        }
                    }
                }
            }

            public void TempIgnoreTargetEnemy()
            {
                if (m_Target == null)
                    return;

                IgnoredEnemies.Add(m_Target);
            }

            public void SetLastFireDir(Vector3 dir)
            {
                LastFireDir = dir;
            }

            public static WeaponAutoAimHandler Current { get; private set; }

            public List<EnemyAgent> AroundEnemies { get; private set; } = new();

            public bool IsPiercingBullet => m_BulletWeapon.ArchetypeData?.PiercingBullets ?? false;

            private HashSet<EnemyAgent> IgnoredEnemies = new();

            private GameObject m_ReticleHolder;

            private CrosshairHitIndicator m_Reticle;

            private EnemyAgent m_Target;

            private Dam_EnemyDamageLimb m_TargetLimb;

            private Camera m_PlayerCamera;

            private bool m_HasTarget;

            private BulletWeapon m_BulletWeapon;

            private PlayerAgent m_Owner;

            private Vector3 m_TargetedEulerAngles = new(0f, 0f, 45f);

            private float fireTimer;

            private Vector3 LastFireDir = Vector3.zero;

            public static Dictionary<uint, WeaponAutoAimHandler> AutoAimInstances { get; private set; } = new();
            public static HashSet<WeaponAutoAimHandler> AllAutoAimInstances { get; private set; } = new();

            private float updateTick = 0.05f;

            public bool HasTarget => m_Target != null && m_Target.Alive && m_TargetLimb != null;

            public bool PauseAutoAim => ((!Settings.ReversePauseAutoAim && Input.GetKey(Settings.PauseAutoAimKey)) || (Settings.ReversePauseAutoAim && !Input.GetKey(Settings.PauseAutoAimKey)))
                //&& m_BulletWeapon.AimButtonHeld 
                && Settings.AutoFire == WeaponAutoAimSettings.AutoFireMode.Off;
        }
    }

}