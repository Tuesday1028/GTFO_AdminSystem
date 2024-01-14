using Globals;
using Hikaria.DevConsoleLite;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Enemy
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    internal class EnemyBehavior : Feature
    {
        public override string Name => "敌人行为";

        public override FeatureGroup Group => EntryPoint.Groups.Enemy;

        [FeatureConfig]
        public static EnemyBehaviorSettings Settings { get; set; }

        public class EnemyBehaviorSettings
        {
            [FSDisplayName("禁用敌人检测")]
            [FSDescription("启用后敌人不会攻击和惊醒(仅房主时有效)")]
            public bool DisableEnemyPlayerDetection
            {
                get
                {
                    return !Global.EnemyPlayerDetectionEnabled;
                }
                set
                {
                    Global.EnemyPlayerDetectionEnabled = !value;
                }
            }
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("DisableEnemyPlayerDetection", "禁用敌人检测", "启用后敌人不会攻击和惊醒(仅房主时有效)", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.DisableEnemyPlayerDetection;
                }

                Settings.DisableEnemyPlayerDetection = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 禁用敌人检测");
            }, () =>
            {
                DevConsole.LogVariable("禁用敌人检测", Settings.DisableEnemyPlayerDetection);
            }));
        }
    }
}
