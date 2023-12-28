using Gear;
using Hikaria.DevConsoleLite;
using Player;
using System.Collections.Generic;
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

        public override string Group => EntryPoint.Groups.Weapon;

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

                //保存原始数值用于恢复
                falloff = __instance.ArchetypeData.DamageFalloff;
                AimSpread = __instance.ArchetypeData.AimSpread;
                HipFireSpread = __instance.ArchetypeData.HipFireSpread;
                ShotgunConeSize = __instance.ArchetypeData.ShotgunConeSize;
                ShotgunBulletSpread = __instance.ArchetypeData.ShotgunBulletSpread;
                isPiercingBullets = __instance.ArchetypeData.PiercingBullets;
                PiercingDamageCountLimit = __instance.ArchetypeData.PiercingDamageCountLimit;

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


        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.OnUnWield))]
        private class BulletWeapon__OnUnWield__Patch
        {
            static void Prefix(ref BulletWeapon __instance)
            {
                if (GameStateManager.Current.m_currentStateName != eGameStateName.InLevel)
                {
                    return;
                }
                if (!IsWeaponOwner(__instance))
                {
                    return;
                }
                __instance.ArchetypeData.PiercingBullets = isPiercingBullets;
                __instance.ArchetypeData.PiercingDamageCountLimit = PiercingDamageCountLimit;
                __instance.ArchetypeData.DamageFalloff = falloff;
                __instance.ArchetypeData.AimSpread = AimSpread;
                __instance.ArchetypeData.HipFireSpread = HipFireSpread;
                __instance.ArchetypeData.ShotgunBulletSpread = ShotgunBulletSpread;
                __instance.ArchetypeData.ShotgunConeSize = ShotgunConeSize;
            }
        }

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


        private static void SetupClearSight(GameObject baseGO, bool enable)
        {
            foreach (var item in baseGO.GetComponentsInChildren<Transform>())
            {
                string childName = item.name.ToLower(System.Globalization.CultureInfo.CurrentCulture);
                if (childName.Contains("thermal") || childName.Contains("glass"))
                {
                    MeshRenderer meshRenderer = item.gameObject.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        Material material = meshRenderer.material;
                        Shader shader = meshRenderer.material.shader;
                        for (int j = 0; j < shader.GetPropertyCount(); j++)
                        {
                            string propName = shader.GetPropertyName(j).ToLower(System.Globalization.CultureInfo.CurrentCulture);
                            var type = shader.GetPropertyType(j);
                            int nameId = shader.GetPropertyNameId(j);
                            foreach (var name in ClearSightShaderProps)
                            {
                                if (propName.Contains(name))
                                {
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
            }
        }


        private static bool isPiercingBullets;

        private static Vector2 falloff;

        private static float AimSpread;

        private static float HipFireSpread;

        private static int ShotgunConeSize;

        private static int ShotgunBulletSpread;

        private static Vector2 FalloffBlocker = new(1000f, 1001f);

        private static int EnemyDamagableLayerMask;

        private static int BulletPiercingPassMask;

        private static int BulletWeaponRayMask;

        private static int PiercingDamageCountLimit;
    }
}
