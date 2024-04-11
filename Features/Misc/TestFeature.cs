#if DEBUG

using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Misc;

[AutomatedFeature]
[HideInModSettings]
[EnableFeatureByDefault]
internal class TestFeature : Feature
{
    public override string Name => "Testing";

    public override FeatureGroup Group => EntryPoint.Groups.Misc;
}
#endif
