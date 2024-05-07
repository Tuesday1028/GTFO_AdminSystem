using Hikaria.AdminSystem.Utilities;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using Hikaria.DevConsoleLite;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Misc
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class JoinLobbyFlash : Feature, IOnPlayerEvent
    {
        public override string Name => "进场特效";

        public override string Description => "启用后进入大厅会发出对应位置玩家颜色的闪光";

        public override FeatureGroup Group => EntryPoint.Groups.Misc;

        public override bool InlineSettingsIntoParentMenu => true;

        [FeatureConfig]
        public static JoinLobbyFlashSettings Settings { get; set; }

        public class JoinLobbyFlashSettings
        {
            [FSDisplayName("进场特效")]
            public bool EnableJoinLobbyFlash { get; set; } = false;
        }

        public override void Init()
        {
            GameEventAPI.RegisterSelf(this);
            DevConsole.AddCommand(Command.Create<bool?>("JoinLobbyFlash", "进场特效", "启用后进入大厅会发出对应位置玩家颜色的闪光", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.EnableJoinLobbyFlash;
                }

                Settings.EnableJoinLobbyFlash = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 进场特效");
            }, () =>
            {
                DevConsole.LogVariable("进场特效", Settings.EnableJoinLobbyFlash);
            }));
        }

        public void OnPlayerEvent(SNet_Player player, SNet_PlayerEvent playerEvent, SNet_PlayerEventReason reason)
        {
            if (!player.IsLocal || !player.HasPlayerAgent || playerEvent != SNet_PlayerEvent.PlayerAgentSpawned || !Settings.EnableJoinLobbyFlash)
            {
                return;
            }
            Color color = player.PlayerColor.RGBMultiplied(75);
            Vector3 dir = AdminUtils.LocalPlayerAgent.FPSCamera.CameraRayDir;
            EnvironmentStateManager.AttemptLightningStrike(dir, color);
            EnvironmentStateManager.AttemptLightningStrike(dir, color);
            EnvironmentStateManager.AttemptLightningStrike(dir, color);
            EnvironmentStateManager.AttemptLightningStrike(dir, color);
            EnvironmentStateManager.AttemptLightningStrike(dir, color);
        }
    }
}
