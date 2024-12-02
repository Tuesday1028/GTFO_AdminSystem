using CellMenu;
using Hikaria.AdminSystem.Utilities;
using Hikaria.QC;
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
    public class MapClickWarp : Feature
    {
        public override string Name => "地图点击传送";

        public override string Description => "玩家通过点击地图传送到点击位置";

        public override bool InlineSettingsIntoParentMenu => true;

        [FeatureConfig]
        public static MapClickWarpSettings Settings { get; set; }

        public class MapClickWarpSettings
        {
            [FSDisplayName("地图点击传送")]
            [Command("MapClickWarp", MonoTargetType.Registry)]
            public bool EnableMapClickWarp { get; set; }
        }


        public override FeatureGroup Group => EntryPoint.Groups.Player;

        public override void Init()
        {
            QuantumRegistry.RegisterObject(Settings);
        }

        [ArchivePatch(typeof(CM_PageMap), nameof(CM_PageMap.DrawWithPixels))]
        public class CM_LagePageMap__DrawWithPixels__Patch
        {
            private static void Postfix(SNet_Player player, Vector2 pos)
            {
                if (!Settings.EnableMapClickWarp)
                    return;
                PlayerAgent playerAgent = player.PlayerAgent.Cast<PlayerAgent>();
                Vector3 vector;
                vector = new Vector3(pos.x / CM_PageMap.WorldToUIDisScale, playerAgent.Position.y, pos.y / CM_PageMap.WorldToUIDisScale);
                playerAgent.RequestWarpToSync(playerAgent.DimensionIndex, vector, playerAgent.TargetLookDir, PlayerAgent.WarpOptions.ShowScreenEffectForLocal);
                ConsoleLogs.LogToConsole($"<color=orange>{playerAgent.PlayerName} 已传送至 ({(int)vector.x},{(int)vector.y},{(int)vector.z})</color>");
            }
        }

        public override void OnGameStateChanged(int state)
        {
            eGameStateName current = (eGameStateName)state;
            if (current == eGameStateName.AfterLevel)
            {
                Settings.EnableMapClickWarp = false;
            }
        }
    }
}
