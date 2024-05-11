using CellMenu;
using Hikaria.DevConsoleLite;
using Player;
using SNetwork;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Player
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class MiniMapWarp : Feature
    {
        public override string Name => "小地图传送";

        public override string Description => "玩家通过点击小地图传送到点击位置";

        [FeatureConfig]
        public static MiniMapWarpSettings Settings { get; set; }

        public class MiniMapWarpSettings
        {
            [FSDisplayName("小地图传送")]
            public bool EnableMiniMapWarp { get; set; }
        }

        public override FeatureGroup Group => EntryPoint.Groups.Player;

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<bool?>("MiniMapWarp", "小地图传送", "小地图传送", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.EnableMiniMapWarp;
                }
                Settings.EnableMiniMapWarp = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 小地图传送");
            }, () =>
            {
                DevConsole.LogVariable("小地图传送", Settings.EnableMiniMapWarp);
            }));
        }

        [ArchivePatch(typeof(CM_PageMap), nameof(CM_PageMap.DrawWithPixels))]
        public class CM_LagePageMap__DrawWithPixels__Patch
        {
            private static void Postfix(SNet_Player player, Vector2 pos)
            {
                if (!Settings.EnableMiniMapWarp)
                    return;
                PlayerAgent playerAgent = player.PlayerAgent.Cast<PlayerAgent>();
                Vector3 vector;
                vector = new Vector3(pos.x / CM_PageMap.WorldToUIDisScale, playerAgent.Position.y, pos.y / CM_PageMap.WorldToUIDisScale);
                playerAgent.RequestWarpToSync(playerAgent.DimensionIndex, vector, playerAgent.TargetLookDir, PlayerAgent.WarpOptions.ShowScreenEffectForLocal);
                DevConsole.Log($"<color=orange>{playerAgent.PlayerName} 已传送至 ({(int)vector.x},{(int)vector.y},{(int)vector.z})</color>");
            }
        }

        public override void OnGameStateChanged(int state)
        {
            eGameStateName current = (eGameStateName)state;
            if (current == eGameStateName.AfterLevel)
            {
                Settings.EnableMiniMapWarp = false;
            }
        }
    }
}
