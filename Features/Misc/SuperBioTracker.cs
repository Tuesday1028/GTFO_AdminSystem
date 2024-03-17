using Gear;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Misc;

internal class SuperBioTracker : Feature
{
    public override string Name => "超级生物扫描仪";

    public override FeatureGroup Group => EntryPoint.Groups.Misc;

    [ArchivePatch(typeof(EnemyScanner), nameof(EnemyScanner.RefreshNodeEnemyList))]
    private static class EnemyScanner__RefreshNodeEnemyList__Patch
    {
        private static bool Prefix(EnemyScanner __instance)
        {
            if (!__instance.Owner.IsLocallyOwned)
                return true;
            __instance.RefreshUpdateID();
            if (__instance.Owner.CourseNode != null)
            {
                EnemyScanner.BotGetScanResult(__instance.Owner.CourseNode, __instance.Owner.Position, __instance.m_enemiesInNodes);
                __instance.m_nodeListStepsLeft = __instance.m_enemiesInNodes.Count;
            }
            __instance.m_nodeListIndex = 0;
            return false;
        }
    }

    [ArchivePatch(typeof(EnemyScannerGraphics), nameof(EnemyScannerGraphics.IsDetected))]
    private static class EnemyScannerGraphics__IsDetected__Patch
    {
        private static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [ArchivePatch(typeof(EnemyScanner), nameof(EnemyScanner.UpdateDetectedEnemies))]
    private static class EnemyScanner__UpdateDetectedEnemies__Patch
    {
        private static void Prefix(EnemyScanner __instance, ref int stepMax)
        {
            if (!__instance.Owner.IsLocallyOwned)
                return;
            stepMax = int.MaxValue;
        }
    }

    [ArchivePatch(typeof(EnemyScanner), nameof(EnemyScanner.UpdateTagProgress))]
    private static class EnemyScanner__UpdateTagProgress__Patch
    {
        private static void Prefix(EnemyScanner __instance, ref int maxTags)
        {
            if (!__instance.Owner.IsLocallyOwned)
                return;
            maxTags = int.MaxValue;
        }
    }

    [ArchivePatch(typeof(EnemyScanner), nameof(EnemyScanner.IterateNodeList))]
    private static class EnemyScanner__IterateNodeList__Patch
    {
        private static void Prefix(EnemyScanner __instance, ref int stepMax)
        {
            if (!__instance.Owner.IsLocallyOwned)
                return;
            stepMax = int.MaxValue;
        }
    }
}
