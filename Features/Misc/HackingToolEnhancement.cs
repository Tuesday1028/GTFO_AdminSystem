using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.Misc;

[EnableFeatureByDefault]
public class HackingToolEnhancement : Feature
{
    public override string Name => "入侵工具增强";

    public override FeatureGroup Group => EntryPoint.Groups.Misc;

    [FeatureConfig]
    public static HackingMinigameAutoCompleteSettings Settings { get; set; }

    public class HackingMinigameAutoCompleteSettings
    {
        [FSDisplayName("入侵时间间隔")]
        [FSSlider(0f, 1f, FSSlider.SliderStyle.FloatTwoDecimal)]
        public float HackingPauseTime { get; set; } = 0.25f;
        [FSDisplayName("立即完成")]
        public bool InstantHacking { get; set; }
        [FSDisplayName("自动入侵")]
        public bool AutoClick { get; set; }
        [FSDisplayName("禁用失败检测")]
        public bool DisableMissCheck { get; set; }
    }

    [ArchivePatch(typeof(HackingMinigame_TimingGrid), nameof(HackingMinigame_TimingGrid.UpdateGame))]
    private class HackingMinigame_TimingGrid__UpdateGame__Patch
    {
        private static void Prefix(HackingMinigame_TimingGrid __instance)
        {
            if (Settings.InstantHacking)
            {
                __instance.m_puzzleDone = true;
            }
        }

        private static void Postfix(HackingMinigame_TimingGrid __instance, ref bool __result)
        {
            if (Settings.AutoClick)
            {
                if (!__instance.m_puzzleDone
                    && __instance.m_movingRow >= __instance.m_selectorRowStart
                    && __instance.m_movingRow < __instance.m_selectorRowEnd)
                {
                    __instance.OnHit();
                    __result = false;
                }
            }
        }
    }

    [ArchivePatch(typeof(HackingMinigame_TimingGrid), nameof(HackingMinigame_TimingGrid.SetSelectorRows))]
    private class HackingMinigame_TimingGrid__SetSelectorRows__Patch
    {
        private static void Prefix(HackingMinigame_TimingGrid __instance, ref int width)
        {
            if (Settings.DisableMissCheck)
            {
                width = __instance.m_gridSizeX;
            }
        }
    }

    [ArchivePatch(typeof(HackingMinigame_TimingGrid), nameof(HackingMinigame_TimingGrid.SetPuzzleLevel))]
    private class HackingMinigame_TimingGrid__SetPuzzleLevel__Patch
    {
        private static void Prefix(ref float pauseDelay)
        {
            pauseDelay = Settings.HackingPauseTime;
        }
    }

    [ArchivePatch(typeof(HackingMinigame_TimingGrid), nameof(HackingMinigame_TimingGrid.OnHit))]
    private class HackingMinigame_TimingGrid__OnHit__Patch
    {
        private static void Postfix(HackingMinigame_TimingGrid __instance)
        {
            __instance.m_gamePauseTimer = Settings.HackingPauseTime;
        }
    }

    [ArchivePatch(typeof(HackingMinigame_TimingGrid), nameof(HackingMinigame_TimingGrid.OnMiss))]
    private class HackingMinigame_TimingGrid__OnMiss__Patch
    {
        private static void Postfix(HackingMinigame_TimingGrid __instance)
        {
            __instance.m_gamePauseTimer = Settings.HackingPauseTime;
        }
    }
}
