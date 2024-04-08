using Clonesoft.Json;
using Hikaria.DevConsoleLite;
using LevelGeneration;
using Player;
using SNetwork;
using System;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using static Hikaria.AdminSystem.Managers.PauseManager;

namespace Hikaria.AdminSystem.Features.InLevel;

[DisallowInGameToggle]
[EnableFeatureByDefault]
public class PauseGame : Feature
{
    public override string Name => "暂停游戏";

    public override FeatureGroup Group => EntryPoint.Groups.InLevel;

    [FeatureConfig]
    public static PauseGameSettings Settings { get; set; }

    public class PauseGameSettings
    {
        [JsonIgnore]
        [FSDisplayName("当前状态")]
        public PauseGameStatus CurrentStatus
        {
            get => _currentStatus;
            set
            {
                if (SNet.IsMaster && _currentStatus != value && CurrentGameState == (int)eGameStateName.InLevel)
                {
                    _currentStatus = value;
                    DoPauseGame(_currentStatus);
                }
            }
        }
        [JsonIgnore]
        private PauseGameStatus _currentStatus = PauseGameStatus.Unpaused;
    }

    [Localized]
    public enum PauseGameStatus
    {
        Unpaused,
        Paused
    }

    public override void Init()
    {
        DevConsole.AddCommand(Command.Create("PauseGame", "暂停游戏", "暂停游戏", () =>
        {
            if (CurrentGameState != (int)eGameStateName.InLevel)
            {
                DevConsole.LogError("不在游戏中");
                return;
            }
            if (!SNet.IsMaster)
            {
                DevConsole.LogError("主机才能暂停游戏");
                return;
            }
            Array enumValues = Enum.GetValues<PauseGameStatus>();
            int currentIndex = Array.IndexOf(enumValues, Settings.CurrentStatus);
            int nextIndex = (currentIndex + 1) % enumValues.Length;
            Settings.CurrentStatus = (PauseGameStatus)enumValues.GetValue(nextIndex);
            DevConsole.LogSuccess($"已{(Settings.CurrentStatus == PauseGameStatus.Paused ? "暂停" : "取消暂停")}游戏");
        }));
    }

    public override void OnGameDataInitialized()
    {
        Setup();
    }

    public override void OnGameStateChanged(int state)
    {
        Settings.CurrentStatus = PauseGameStatus.Unpaused;
    }

    [ArchivePatch(typeof(GS_InLevel), nameof(GS_InLevel.Update))]
    private class GS_InLevel__Update__Postfix
    {
        private static void Postfix()
        {
            if (IsPaused)
            {
                Clock.ExpeditionProgressionTime = ExpeditionProgressionTime;
            }
        }
    }

    [ArchivePatch(typeof(PlayerSync), nameof(PlayerSync.IncomingLocomotion))]
    private class PlayerSync__IncomongLocomotion__Patch
    {
        private static void Prefix(PlayerSync __instance, pPlayerLocomotion data)
        {
            if (!SNet.IsMaster || !IsPaused)
            {
                return;
            }
            var isReady = __instance.m_agent?.Owner?.Load<pReady>().isReady ?? false;
            if (!isReady)
            {
                return;
            }
            if (data.Pos == __instance.m_locomotionLast.Pos && __instance.m_agent.Inventory.WieldedSlot == InventorySlot.None)
            {
                return;
            }
            __instance.m_agent.RequestToggleControlsEnabled(false);
        }
    }

    private static float ExpeditionProgressionTime;

    private static void DoPauseGame(PauseGameStatus status)
    {
        var flag = status == PauseGameStatus.Paused;
        if (flag)
        {
            ExpeditionProgressionTime = Clock.ExpeditionProgressionTime;
        }
        else
        {
            Clock.ExpeditionProgressionTime = ExpeditionProgressionTime;
        }
        IsPaused = flag;
        SetPauseForWardenObjectiveItems(flag);
        SetPauseForAllPlayers(flag);
    }

    private static void SetPauseForAllPlayers(bool paused)
    {
        foreach (PlayerAgent player in PlayerManager.PlayerAgentsInLevel)
        {
            player.RequestToggleControlsEnabled(!paused);
        }
    }

    private static void SetPauseForWardenObjectiveItems(bool paused)
    {
        var reactors = UnityEngine.Object.FindObjectsOfType<LG_WardenObjective_Reactor>();
        foreach (var reactor in reactors)
        {
            if (reactor.m_isWardenObjective && !reactor.ObjectiveItemSolved)
            {
                switch (reactor.m_currentState.status)
                {
                    case eReactorStatus.Startup_intro:
                        reactor.m_progressUpdateEnabled = !paused;
                        break;
                    case eReactorStatus.Startup_intense:
                        reactor.m_progressUpdateEnabled = !paused;
                        break;
                    case eReactorStatus.Startup_waitForVerify:
                        reactor.m_progressUpdateEnabled = !paused;
                        break;
                    case eReactorStatus.Shutdown_intro:
                        reactor.m_progressUpdateEnabled = !paused;
                        break;
                }
            }
        }
    }
}
