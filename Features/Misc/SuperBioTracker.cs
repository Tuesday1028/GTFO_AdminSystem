using Agents;
using AK;
using Enemies;
using Gear;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Misc;

[EnableFeatureByDefault]
[DisallowInGameToggle]
internal class SuperBioTracker : Feature
{
    public override string Name => "超级生物扫描仪";

    public override FeatureGroup Group => EntryPoint.Groups.Misc;

    [FeatureConfig]
    public static SuperBioTrackerSetting Settings { get; set; }

    public class SuperBioTrackerSetting
    {
        [FSDisplayName("使用机器人扫描")]
        public bool UseBotTag { get; set; }
        [FSDisplayName("忽略单次标记上限")]
        public bool IgnoreMaxTags { get; set; }
    }

    [ArchivePatch(typeof(EnemyScanner), nameof(EnemyScanner.UpdateTagProgress))]
    private static class EnemyScanner__UpdateTagProgress__Patch
    {
        private static bool Prefix(EnemyScanner __instance, ref int maxTags)
        {
            if (!__instance.Owner.IsLocallyOwned)
                return true;
            maxTags = Settings.IgnoreMaxTags ? int.MaxValue : maxTags;
            if (Settings.UseBotTag)
            {
                UpdateTagProgress(__instance, maxTags);
                return false;
            }
            return true;
        }

        public static bool AllowBotTag { get; private set; }

        public static bool AllowAgentModePatch { get; private set; }

        private static bool TryGetTaggableEnemies(EnemyScanner __instance)
        {
            AllowBotTag = false;
            AllowAgentModePatch = true;
            __instance.m_taggableEnemies = new();
            EnemyScanner.BotTag(__instance.Owner.CourseNode, __instance.Owner.Position, __instance.m_taggableEnemies);
            AllowAgentModePatch = false;
            AllowBotTag = true;
            return __instance.m_taggableEnemies.Count > 0;
        }

        private static void UpdateTagProgress(EnemyScanner __instance, int maxTags)
        {
            __instance.m_lastTagging = __instance.m_tagging;
            __instance.m_lastRecharging = __instance.m_recharging;
            if (__instance.m_showingNoTargetsTimer > 0f && Clock.Time > __instance.m_showingNoTargetsTimer)
            {
                __instance.m_screen.SetNoTargetsText("");
                __instance.m_showingNoTargetsTimer = -1f;
            }
            if ((!__instance.m_recharging && !__instance.m_tagging && __instance.FireButtonPressed) || (__instance.m_tagging && __instance.FireButton))
            {
                if (!TryGetTaggableEnemies(__instance))
                {
                    if (__instance.m_showingNoTargetsTimer <= 0f)
                    {
                        __instance.m_screen.SetNoTargetsText("NO MOVING TARGETS FOUND");
                        __instance.m_showingNoTargetsTimer = Clock.Time + 1f;
                        __instance.Sound.Post(EVENTS.BIOTRACKER_NO_MOVING_TARGET_FOUND, true);
                        return;
                    }
                }
                else
                {
                    if (!__instance.m_tagging)
                    {
                        __instance.m_tagStartTime = Clock.Time;
                        __instance.Sound.Post(EVENTS.BIOTRACKER_TAGGING_CHARGE_LOOP, true);
                        __instance.m_screen.SetStatusText("Tagging..");
                        __instance.m_tagging = true;
                        __instance.m_recharging = false;
                        return;
                    }
                    if (Clock.Time < __instance.m_tagStartTime + EnemyScanner.TagDuration)
                    {
                        __instance.m_progressBar.SetProgress((Clock.Time - __instance.m_tagStartTime) / EnemyScanner.TagDuration);
                        return;
                    }
                    __instance.Sound.Post(EVENTS.BIOTRACKER_TAGGING_CHARGE_FINISHED, true);
                    if (TryGetTaggableEnemies(__instance))
                    {
                        AllowAgentModePatch = true;
                        EnemyScanner.BotTag(__instance.Owner.CourseNode, __instance.Owner.Position, __instance.m_taggableEnemies);
                        AllowAgentModePatch = false;
                        if (__instance.m_enemiesDetected.Count > 1)
                        {
                            PlayerDialogManager.WantToStartDialog(185U, __instance.Owner);
                        }
                    }
                    __instance.m_screen.SetGuixColor(Color.red);
                    __instance.m_screen.SetStatusText("Recharging..");
                    __instance.m_tagging = false;
                    __instance.m_recharging = true;
                    __instance.m_rechargeStartTime = Clock.Time;
                    return;
                }
            }
            else if (__instance.m_recharging)
            {
                float num = AgentModifierManager.ApplyModifier(__instance.Owner, AgentModifier.ScannerRechargeSpeed, __instance.m_rechargeDuration);
                if (Clock.Time < __instance.m_rechargeStartTime + num)
                {
                    float num2 = 1f - (Clock.Time - __instance.m_tagStartTime) / num;
                    __instance.m_tagging = false;
                    __instance.m_progressBar.SetProgress(num2);
                    return;
                }
                __instance.m_recharging = false;
                __instance.Sound.Post(EVENTS.BIOTRACKER_RECHARGED, true);
                __instance.m_screen.ResetGuixColor();
                __instance.m_screen.SetStatusText("Ready to tag");
                return;
            }
            else if (__instance.m_progressBar.Progress > 0f)
            {
                __instance.m_progressBar.SetProgress(__instance.m_progressBar.Progress - Clock.Delta * 3f);
                if (__instance.m_tagging)
                {
                    __instance.Sound.Post(EVENTS.BIOTRACKER_TAGGING_CHARGE_FINISHED, true);
                }
                __instance.m_tagging = false;
            }
        }
    }

    [ArchivePatch(typeof(EnemyScanner), nameof(EnemyScanner.UpdateDetectedEnemies))]
    private static class EnemyScanner__UpdateDetectedEnemies__Patch
    {
        private static void Prefix(EnemyScanner __instance, ref int stepMax)
        {
            if (!__instance.Owner.IsLocallyOwned)
                return;
            stepMax = Settings.IgnoreMaxTags ? int.MaxValue : stepMax;
        }
    }

    [ArchivePatch(typeof(EnemyScanner), nameof(EnemyScanner.IterateNodeList))]
    private static class EnemyScanner__IterateNodeList__Patch
    {
        private static void Prefix(EnemyScanner __instance, ref int stepMax)
        {
            if (!__instance.Owner.IsLocallyOwned)
                return;
            stepMax = Settings.IgnoreMaxTags ? int.MaxValue : stepMax;
        }
    }

    [ArchivePatch(typeof(ToolSyncManager), nameof(ToolSyncManager.WantToTagEnemy))]
    private static class ToolSyncManager__WantToTagEnemy__Patch
    {
        private static bool Prefix()
        {
            if (!EnemyScanner__UpdateTagProgress__Patch.AllowBotTag)
                return false;
            return true;
        }
    }

    [ArchivePatch(typeof(AgentAI), nameof(AgentAI.Mode), null, ArchivePatch.PatchMethodType.Getter)]
    private static class AgentAI__get_Mode__Patch
    {
        private static void Postfix(AgentAI __instance, ref AgentMode __result)
        {
            if (EnemyScanner__UpdateTagProgress__Patch.AllowAgentModePatch && (__instance.TryCast<EnemyAI>()?.m_enemyAgent?.IsScout ?? false))
                __result = AgentMode.Scout;
        }
    }
}
