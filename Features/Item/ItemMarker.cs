using Hikaria.ItemMarker.Managers;
using Hikaria.QC;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Item
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class ItemMarker : Feature
    {
        public override string Name => "物品标记";

        public override FeatureGroup Group => EntryPoint.Groups.Item;

        [FeatureConfig]
        public static ItemMarkerSettings Settings { get; set; }

        public class ItemMarkerSettings
        {
            [FSDisplayName("状态")]
            public bool EnableItemMarker { get => ItemMarkerManager.DevMode; set => ItemMarkerManager.DevMode = value; }
        }

        [Command("ItemMarker")]
        private static bool EnableItemMarker { get => ItemMarkerManager.DevMode; set => ItemMarkerManager.DevMode = value; }
    }
}