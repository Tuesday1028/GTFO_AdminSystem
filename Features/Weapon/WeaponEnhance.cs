using Agents;
using AK;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Enemies;
using GameData;
using Gear;
using Hikaria.AdminSystem.Utility;
using Hikaria.DevConsoleLite;
using Il2CppInterop.Runtime.InteropTypes;
using Player;
using SNetwork;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Weapon
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    internal class WeaponEnhance : Feature
    {
        public override string Name => "武器加强";

        public override string Description => "增强武器属性";

        public override FeatureGroup Group => EntryPoint.Groups.Weapon;

        [FeatureConfig]
        public static WeaponEnhanceSettings Settings { get; set; }

        public class WeaponEnhanceSettings
        {
            [FSDisplayName("清晰瞄具")]
            [FSDescription("去除枪械瞄具污渍, 加强热成像瞄具")]
            public bool ClearSight { get; set; }

            [FSDisplayName("无伤害衰减")]
            [FSDescription("枪械伤害没有距离衰减")]
            public bool NoDamageFalloff { get; set; }

            [FSDisplayName("无弹道扩散")]
            [FSDescription("枪械弹道无扩散, 散弹子弹散步减小")]
            public bool NoSpread { get; set; }

            [FSDisplayName("无后座")]
            [FSDescription("枪械无后坐力")]
            public bool NoRecoil { get; set; }

            private bool _wallHack;

            [FSDisplayName("子弹穿墙")]
            [FSDescription("枪械子弹穿墙")]
            public bool WallHack
            {
                get
                {
                    return _wallHack;
                }
                set
                {
                    _wallHack = value;
                    LayerManager.MASK_BULLETWEAPON_PIERCING_PASS = _wallHack ? EnemyDamagableLayerMask : BulletPiercingPassMask;
                    LayerManager.MASK_BULLETWEAPON_RAY = _wallHack ? EnemyDamagableLayerMask : BulletWeaponRayMask;
                }
            }

            [FSDisplayName("无声枪")]
            [FSDescription("枪械无开火声音, 即不惊怪(仅客机可用)")]
            public bool SilentWeapon { get; set; }

            [FSDisplayName("自动上弹")]
            [FSDescription("优先消耗后备弹药")]
            public bool AutoReload { get; set; }

            [FSDisplayName("特殊部位伤害溢出")]
            [FSDescription("启用后可以在特殊部位单次打出超过最大生命值上限的伤害")]
            public bool IgnoreLimbMaxHealthClamp { get; set; }

            /*
            [FSHeader("特殊弹药")]
            [FSDisplayName("爆炸子弹")]
            public bool ExplosiveBullet { get; set; }
            [FSDisplayName("爆炸子弹范围")]
            [FSSlider(0f, 5f, FSSlider.SliderStyle.FloatTwoDecimal)]
            public float ExplosiveBulletRadius { get; set; } = 0.5f;
            [FSDisplayName("燃烧子弹")]
            public bool IncendiaryBullet { get; set; }
            */
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("ClearSight", "清晰瞄具", "清晰瞄具", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.ClearSight;
                }
                Settings.ClearSight = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 清晰瞄具");
            }, () =>
            {
                DevConsole.LogVariable("清晰瞄具", Settings.ClearSight);
            }));
            DevConsole.AddCommand(Command.Create<bool?>("NoDamageFalloff", "无伤害衰减", "无伤害衰减", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.NoDamageFalloff;
                }
                Settings.NoDamageFalloff = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 无伤害衰减");
            }, () =>
            {
                DevConsole.LogVariable("无伤害衰减", Settings.NoDamageFalloff);
            }));
            DevConsole.AddCommand(Command.Create<bool?>("NoSpread", "无弹道扩散", "无弹道扩散", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.NoSpread;
                }
                Settings.NoSpread = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 无弹道扩散");
            }, () =>
            {
                DevConsole.LogVariable("无弹道扩散", Settings.NoSpread);
            }));
            DevConsole.AddCommand(Command.Create<bool?>("NoRecoil", "无后座", "无后座", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.NoRecoil;
                }
                Settings.NoRecoil = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 无后座");
            }, () =>
            {
                DevConsole.LogVariable("无后座", Settings.NoRecoil);
            }));
            DevConsole.AddCommand(Command.Create<bool?>("WallHack", "子弹穿墙", "子弹穿墙", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.WallHack;
                }
                Settings.WallHack = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 子弹穿墙");
            }, () =>
            {
                DevConsole.LogVariable("子弹穿墙", Settings.WallHack);
            }));
            DevConsole.AddCommand(Command.Create<bool?>("SilentWeapon", "静音枪", "静音枪", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.SilentWeapon;
                }
                Settings.SilentWeapon = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 静音枪");
            }, () =>
            {
                DevConsole.LogVariable("静音枪", Settings.SilentWeapon);
            }));
            DevConsole.AddCommand(Command.Create<bool?>("AutoReload", "自动上弹", "优先消耗后备弹药", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.AutoReload;
                }
                Settings.AutoReload = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 自动上弹");
            }, () =>
            {
                DevConsole.LogVariable("静音枪", Settings.AutoReload);
            }));
        }

        public override void OnGameDataInitialized()
        {
            EnemyDamagableLayerMask = 1 << LayerManager.LAYER_ENEMY_DAMAGABLE; //只对敌人进行检测

            BulletPiercingPassMask = LayerManager.MASK_BULLETWEAPON_PIERCING_PASS;

            BulletWeaponRayMask = LayerManager.MASK_BULLETWEAPON_RAY;
        }

        /*
        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        private class BulletWeapon__BulletHit__Patch
        {
            private static bool Prefix(global::Weapon.WeaponHitData weaponRayData, bool doDamage, ref bool __result)
            {
                if (!Settings.ExplosiveBullet || !doDamage || !weaponRayData.owner.IsLocallyOwned)
                {
                    return true;
                }
                var position = weaponRayData.rayHit.point;
                CellSound.Post(EVENTS.STICKYMINEEXPLODE, position);
                DamageUtil.DoExplosionDamage(position, Settings.ExplosiveBulletRadius, weaponRayData.damage, LayerManager.MASK_EXPLOSION_TARGETS, LayerManager.MASK_EXPLOSION_BLOCKERS, true, weaponRayData.damage);
                __result = (weaponRayData.rayHit.collider?.gameObject?.GetComponent<IDamageable>() ?? null) != null;
                return false;
            }
        }

        [ArchivePatch(typeof(Dam_EnemyDamageLimb), nameof(Dam_EnemyDamageLimb.BulletDamage))]
        private class Dam_EnemyDamageLimb__BulletDamage__Patch
        {
            private static void Postfix(Dam_EnemyDamageLimb __instance, float dam, Agent sourceAgent)
            {
                if (!Settings.IncendiaryBullet || sourceAgent == null)
                {
                    return;
                }
                var player = sourceAgent.TryCast<PlayerAgent>();
                if (player == null || !player.IsLocallyOwned)
                {
                    return;
                }
                var targetLevel = __instance.ApplyWeakspotAndArmorModifiers(dam, 1f) + __instance.m_base.m_heatLevel;
                if (!SNet.IsMaster)
                {
                    pSetHeatLevel pSetHeatLevel = new();
                    pSetHeatLevel.heatLevel.Set(targetLevel, 100f);
                    __instance.m_base.m_setHeatLevelPacket.ReceiveAction.Invoke(pSetHeatLevel);
                    __instance.m_base.m_setHeatLevelPacket.Send(pSetHeatLevel, SNet_ChannelType.GameNonCritical);
                }
                else
                {
                    __instance.m_base.SetHeatLevel(targetLevel);
                }
                __instance.m_base.OnHeatLevelUpdated();
            }
        }

        [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.Setup))]
        private class EnemyAgent__Setup__Patch
        {
            private static void Postfix(EnemyAgent __instance)
            {
                __instance.StartCoroutineManaged2(UpdateHeat(__instance.Damage).WrapToIl2Cpp());
            }
        }

        private static IEnumerator UpdateHeat(Dam_EnemyDamageBase damageBase)
        {
            var yielder = new WaitForSeconds(damageBase.m_fireDamageDelay);
            damageBase.m_heatLevel = 0f;
            var agent = PlayerManager.GetLocalPlayerAgent();
            while (true)
            {
                if (damageBase.m_heatLevel > 0f)
                {
                    if (!damageBase.OnFire)
                    {
                        damageBase.m_heatLevel = Mathf.Clamp(damageBase.m_heatLevel - damageBase.m_heatFalloffSpeed, 0f, 100f);
                        damageBase.OnHeatLevelUpdated();
                    }
                    else
                    {
                        float num = damageBase.m_fireDamageMaxPerSecond * damageBase.m_fireDamageDelay * (damageBase.m_heatLevel / 100f);
                        damageBase.FireDamage(num, agent);
                        if (damageBase.Owner.Alive)
                        {
                            damageBase.Owner.Voice.PlayVoiceEvent(damageBase.Owner.EnemySFXData.SFX_ID_hurtSmall);
                        }
                    }
                }
                yield return yielder;
            }
        }
        */

        [ArchivePatch(typeof(Dam_EnemyDamageLimb_Custom), nameof(Dam_EnemyDamageLimb_Custom.ApplyWeakspotAndArmorModifiers))]
        private static class Dam_EnemyDamageLimb_Custom__ApplyWeakspotAndArmorModifiers__Patch
        {
            private static bool Prefix(Dam_EnemyDamageLimb_Custom __instance, ref float __result, float dam, float precisionMulti = 1f)
            {
                if (!Settings.IgnoreLimbMaxHealthClamp)
                    return true;
                __result = dam * Mathf.Max(__instance.m_weakspotDamageMulti * precisionMulti, 1f) * __instance.m_armorDamageMulti;
                return false;
            }
        }


        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        private class BulletWeapon__Fire__Patch
        {
            private static void Postfix(BulletWeapon __instance)
            {
                if (Settings.AutoReload && IsWeaponOwner(__instance))
                {
                    __instance.m_inventory.DoReload();
                }
            }
        }

        [ArchivePatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        private class Shotgun__Fire__Patch
        {
            private static void Postfix(Shotgun __instance)
            {
                if (Settings.AutoReload && IsWeaponOwner(__instance))
                {
                    __instance.m_inventory.DoReload();
                }
            }
        }

        [ArchivePatch(typeof(PlayerSync), nameof(PlayerSync.RegisterFiredBullets))]
        private class PlayerSync__RegisterFiredBullets__Patch
        {
            static bool Prefix(PlayerSync __instance)
            {
                if (!__instance.m_agent.Owner.IsLocal || !Settings.SilentWeapon)
                {
                    return true;
                }
                return false;
            }
        }

        [ArchivePatch(typeof(global::Weapon), nameof(global::Weapon.ApplyRecoil))]
        private class Weapon__ApplyRecoil__Patch
        {
            static bool Prefix()
            {
                return !Settings.NoRecoil;
            }
        }

        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.OnWield))]
        private class BulletWeapon__OnWield__Patch
        {
            static void Postfix(ref BulletWeapon __instance)
            {
                if (GameStateManager.Current.m_currentStateName != eGameStateName.InLevel)
                {
                    return;
                }
                if (!IsWeaponOwner(__instance))
                {
                    return;
                }

                if (!WeaponInstanceArchetypeDataLookup.TryGetValue(__instance.ArchetypeData.persistentID, out var originData))
                {
                    WeaponInstanceArchetypeDataLookup.Add(__instance.ArchetypeData.persistentID, AdminUtils.CopyProperties(__instance.ArchetypeData, new()));
                }
                else
                {
                    __instance.ArchetypeData = originData;
                }

                //无伤害衰减
                if (Settings.NoDamageFalloff)
                {
                    __instance.ArchetypeData.DamageFalloff = FalloffBlocker;
                }

                //无后坐力
                if (Settings.NoRecoil)
                {
                    __instance.ArchetypeData.RecoilDataID = 0U;
                }

                //无弹道扩散
                if (Settings.NoSpread)
                {
                    __instance.ArchetypeData.AimSpread = 0f;
                    __instance.ArchetypeData.HipFireSpread = 0f;
                    __instance.ArchetypeData.ShotgunBulletSpread = 0;
                    __instance.ArchetypeData.ShotgunConeSize = 1;
                }

                //清晰瞄具
                SetupClearSight(__instance.gameObject, Settings.ClearSight);
            }
        }

        private static Dictionary<uint, ArchetypeDataBlock> WeaponInstanceArchetypeDataLookup = new();

        private static bool IsWeaponOwner(BulletWeapon bulletWeapon)
        {
            if (bulletWeapon == null || bulletWeapon.Owner == null)
            {
                return false;
            }
            else
            {
                return bulletWeapon.Owner.Owner.IsLocal;
            }
        }


        private static List<string> ClearSightShaderProps = new()
        {
            "falloff", "distortion", "dirt"
        };

        private static List<string> ClearSightCompNames = new()
        {
            "thermal", "glass"
        };


        private static void SetupClearSight(GameObject baseGO, bool enable)
        {
            foreach (var item in baseGO.GetComponentsInChildren<Transform>())
            {
                string childName = item.name.ToLowerInvariant();
                foreach (var compName in ClearSightCompNames)
                {
                    if (!childName.Contains(compName))
                    {
                        continue;
                    }
                    MeshRenderer meshRenderer = item.gameObject.GetComponent<MeshRenderer>();
                    if (meshRenderer == null)
                    {
                        continue;
                    }
                    Material material = meshRenderer.material;
                    Shader shader = meshRenderer.material.shader;
                    for (int j = 0; j < shader.GetPropertyCount(); j++)
                    {
                        string propName = shader.GetPropertyName(j).ToLowerInvariant();
                        var type = shader.GetPropertyType(j);
                        int nameId = shader.GetPropertyNameId(j);
                        foreach (var name in ClearSightShaderProps)
                        {
                            if (!propName.Contains(name))
                            {
                                continue;
                            }
                            if (type == UnityEngine.Rendering.ShaderPropertyType.Float || type == UnityEngine.Rendering.ShaderPropertyType.Range)
                            {
                                var value = enable ? 0 : shader.GetPropertyDefaultFloatValue(j);
                                material.SetFloat(nameId, value);
                            }
                            else if (type == UnityEngine.Rendering.ShaderPropertyType.Vector)
                            {
                                var value = enable ? Vector4.zero : shader.GetPropertyDefaultVectorValue(j);
                                material.SetVector(nameId, value);
                            }
                        }
                    }
                }
            }
        }

        private static Vector2 FalloffBlocker = new(1000f, 1001f);

        private static int EnemyDamagableLayerMask;

        private static int BulletPiercingPassMask;

        private static int BulletWeaponRayMask;
    }
}
