using Agents;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Misc;

public class NoiseBlocker : Feature
{
    public override string Name => "噪声拦截";

    public override FeatureGroup Group => EntryPoint.Groups.Misc;

    [ArchivePatch(typeof(NoiseManager), nameof(NoiseManager.ReceiveNoise))]
    private class NoiseManager__ReceiveNoise__Prefix
    {
        private static bool Prefix()
        {
            return false;
        }
    }

    [ArchivePatch(typeof(NoiseManager), nameof(NoiseManager.MakeNoise))]
    private class NoiseManager__MakeNoise__Prefix
    {
        private static bool Prefix()
        {
            return false;
        }
    }

    [ArchivePatch(typeof(PlayerAgent), nameof(PlayerAgent.Noise), null, ArchivePatch.PatchMethodType.Setter)]
    private class PlayerAgent__set_Noise__Prefix
    {
        private static void Prefix(ref Agent.NoiseType value)
        {
            value = Agent.NoiseType.None;
        }
    }
}
