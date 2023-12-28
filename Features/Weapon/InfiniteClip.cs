using Gear;
using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Weapon
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class InfiniteClip : Feature
    {
        public override string Name => "无限弹夹容量";

        public override string Description => "射击时枪械弹夹容量始终为满";

        public override string Group => EntryPoint.Groups.Weapon;

        [FeatureConfig]
        public static InfiniteClipSettings Settings { get; set; }

        public class InfiniteClipSettings
        {
            [FSDisplayName("无限弹夹容量")]
            public bool EnableInfiniteClip { get; set; }
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("InfClip", "无限弹夹容量", "无限弹夹容量", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.EnableInfiniteClip;
                }
                Settings.EnableInfiniteClip = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 无限弹夹容量");
            }, () =>
            {
                DevConsole.LogVariable("无限弹夹容量", Settings.EnableInfiniteClip);
            }));
        }

        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        private static class BulletWeapon_Fire_Patch
        {
            static void Postfix(BulletWeapon __instance)
            {
                if (!__instance.Owner == AdminUtils.LocalPlayerAgent || !Settings.EnableInfiniteClip)
                {
                    return;
                }
                __instance.m_clip = __instance.GetMaxClip();
                __instance.UpdateAmmoStatus();
            }
        }

        [ArchivePatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        private static class Shotgun_Fire_Patch
        {
            static void Postfix(Shotgun __instance)
            {
                if (!__instance.Owner == AdminUtils.LocalPlayerAgent || !Settings.EnableInfiniteClip)
                {
                    return;
                }
                __instance.m_clip = __instance.ClipSize;
                __instance.UpdateAmmoStatus();
            }
        }
    }
}
