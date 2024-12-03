using GameData;
using Gear;
using Hikaria.AdminSystem.Utility;
using Hikaria.QC;
using Player;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Weapon
{
    [HideInModSettings]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    internal class WeaponEnhancement : Feature
    {
        public override string Name => "武器增强";

        public override string Description => "增强武器属性";

        public override FeatureGroup Group => EntryPoint.Groups.Weapon;

        [FeatureConfig]
        public static WeaponEnhanceSettings Settings { get; set; }

        public class WeaponEnhanceSettings
        {
            [FSDisplayName("无限弹夹容量")]
            public bool InfiniteClip { get => _infiniteClip; set => _infiniteClip = value; }

            [FSDisplayName("清晰瞄具")]
            [FSDescription("去除枪械瞄具污渍, 加强热成像瞄具")]
            public bool ClearSight { get => _clearSight; set => _clearSight = value; }

            [FSDisplayName("无伤害衰减")]
            [FSDescription("枪械伤害没有距离衰减")]
            public bool NoDamageFalloff { get => _noDamageFalloff; set => _noDamageFalloff = value; }

            [FSDisplayName("无弹道扩散")]
            [FSDescription("枪械弹道无扩散, 散弹子弹散步减小")]
            public bool NoSpread { get => _noSpread; set => _noSpread = value; }

            [FSDisplayName("无后座")]
            [FSDescription("枪械无后坐力")]
            public bool NoRecoil { get => _noRecoil; set => _noRecoil = value; }


            [FSDisplayName("子弹穿墙")]
            [FSDescription("枪械子弹穿墙")]
            public bool WallHack { get => EnableWallHack; set => EnableWallHack = value; }

            [FSDisplayName("无声枪")]
            [FSDescription("枪械无开火声音, 即不惊怪(仅客机可用)")]
            public bool SilentWeapon { get => _silentWeapon; set => _silentWeapon = value; }

            [FSDisplayName("自动上弹")]
            [FSDescription("优先消耗后备弹药")]
            public bool AutoReload { get => _autoReload; set => _autoReload = value; }

            [FSDisplayName("特殊部位伤害溢出")]
            [FSDescription("启用后可以在特殊部位单次打出超过最大生命值上限的伤害")]
            public bool IgnoreLimbMaxHealthClamp { get => _ignoreLimbMaxHealthClamp; set => _ignoreLimbMaxHealthClamp = value; }

            [FSDisplayName("多部位穿透")]
            [FSDescription("启用后可以穿透同一敌人的多个部位")]
            public bool MultiLimbPierce { get => _multiLimbPierce; set => _multiLimbPierce = value; }
        }

        [Command("InfClip")]
        private static bool _infiniteClip;

        [Command("ClearSight")]
        private static bool _clearSight;

        [Command("NoDamageFalloff")]
        private static bool _noDamageFalloff;

        [Command("NoSpread")]
        private static bool _noSpread;

        [Command("NoRecoil")]
        private static bool _noRecoil;

        private static bool _enableWallHack;

        [Command("WallHack")]
        private static bool EnableWallHack
        {
            get
            {
                return _enableWallHack;
            }
            set
            {
                _enableWallHack = value;
                LayerManager.MASK_BULLETWEAPON_PIERCING_PASS = _enableWallHack ? EnemyDamagableLayerMask : BulletPiercingPassMask;
                LayerManager.MASK_BULLETWEAPON_RAY = _enableWallHack ? EnemyDamagableLayerMask : BulletWeaponRayMask;
            }
        }

        [Command("SilentWeapon")]
        private static bool _silentWeapon;

        [Command("AutoReload")]
        private static bool _autoReload;

        [Command("IgnoreLimbMaxHealthClamp")]
        private static bool _ignoreLimbMaxHealthClamp;

        [Command("MultiLimbPierce")]
        private static bool _multiLimbPierce;

        public override void OnGameDataInitialized()
        {
            EnemyDamagableLayerMask = 1 << LayerManager.LAYER_ENEMY_DAMAGABLE; //只对敌人进行检测

            BulletPiercingPassMask = LayerManager.MASK_BULLETWEAPON_PIERCING_PASS;

            BulletWeaponRayMask = LayerManager.MASK_BULLETWEAPON_RAY;
        }


        [ArchivePatch(typeof(Dam_EnemyDamageLimb_Custom), nameof(Dam_EnemyDamageLimb_Custom.ApplyWeakspotAndArmorModifiers))]
        private static class Dam_EnemyDamageLimb_Custom__ApplyWeakspotAndArmorModifiers__Patch
        {
            private static bool Prefix(Dam_EnemyDamageLimb_Custom __instance, ref float __result, float dam, float precisionMulti = 1f)
            {
                if (!_ignoreLimbMaxHealthClamp)
                    return true;
                __result = dam * Mathf.Max(__instance.m_weakspotDamageMulti * precisionMulti, 1f) * __instance.m_armorDamageMulti;
                return false;
            }
        }

        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
        private class BulletWeapon__BulletHit__Patch
        {
            private static void Prefix(ref uint damageSearchID)
            {
                if (_multiLimbPierce)
                {
                    damageSearchID = 0;
                }
            }
        }

        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        private class BulletWeapon__Fire__Patch
        {
            private static void Postfix(BulletWeapon __instance)
            {
                if (!IsWeaponOwner(__instance))
                    return;
                if (_autoReload)
                {
                    __instance.m_inventory.DoReload();
                }
                if (_infiniteClip)
                {
                    __instance.m_clip = __instance.ClipSize;
                    __instance.UpdateAmmoStatus();
                }
            }
        }

        [ArchivePatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        private class Shotgun__Fire__Patch
        {
            private static void Postfix(Shotgun __instance)
            {
                if (!IsWeaponOwner(__instance))
                    return;
                if (_autoReload)
                {
                    __instance.m_inventory.DoReload();
                }
                if (_infiniteClip)
                {
                    __instance.m_clip = __instance.ClipSize;
                    __instance.UpdateAmmoStatus();
                }
            }
        }

        [ArchivePatch(typeof(PlayerSync), nameof(PlayerSync.RegisterFiredBullets))]
        private class PlayerSync__RegisterFiredBullets__Patch
        {
            static bool Prefix(PlayerSync __instance)
            {
                if (!__instance.m_agent.Owner.IsLocal || !_silentWeapon)
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
                return !_noRecoil;
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
                if (_noDamageFalloff)
                {
                    __instance.ArchetypeData.DamageFalloff = FalloffBlocker;
                }

                //无后坐力
                if (_noRecoil)
                {
                    __instance.ArchetypeData.RecoilDataID = 0U;
                }

                //无弹道扩散
                if (_noSpread)
                {
                    __instance.ArchetypeData.AimSpread = 0f;
                    __instance.ArchetypeData.HipFireSpread = 0f;
                    __instance.ArchetypeData.ShotgunBulletSpread = 0;
                    __instance.ArchetypeData.ShotgunConeSize = 1;
                }

                //清晰瞄具
                SetupClearSight(__instance.gameObject, _clearSight);
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
            foreach (var renderer in baseGO.GetComponentsInChildren<MeshRenderer>())
            {
                string childName = renderer.name.ToLowerInvariant();
                foreach (var compName in ClearSightCompNames)
                {
                    if (!childName.Contains(compName))
                    {
                        continue;
                    }
                    Material material = renderer.material;
                    Shader shader = renderer.material.shader;
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
