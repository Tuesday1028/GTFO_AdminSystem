using Hikaria.ItemMarker.Managers;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Item
{
    [EnableFeatureByDefault]
    public class ItemMarker : Feature
    {
        public override string Name => "物品标记";

        public override FeatureGroup Group => EntryPoint.Groups.Item;

        public override void OnEnable()
        {
            ItemMarkerManager.DevMode = true;
        }

        public override void OnDisable()
        {
            ItemMarkerManager.DevMode = false;
        }
    }
}