using Clonesoft.Json;
using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using LevelGeneration;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;

namespace Hikaria.AdminSystem.Features.WardenObjective
{
    [DisallowInGameToggle]
    [EnableFeatureByDefault]
    [DoNotSaveToConfig]
    public class ReactorLookup : Feature
    {
        public override string Name => "反应堆";

        public override FeatureGroup Group => EntryPoint.Groups.InLevel;

        public static Dictionary<int, LG_WardenObjective_Reactor> ReactorsInLevel { get; set; } = new();

        [FeatureConfig]
        [JsonIgnore]
        public static ReactorLookupSettings Settings { get; set; }

        public class ReactorLookupSettings
        {
            [FSDisplayName("反应堆列表")]
            [FSReadOnly]
            public List<ReactorEntry> RegisteredReactors
            {
                get
                {
                    List<ReactorEntry> list = new();
                    foreach (var reactor in ReactorsInLevel.Values)
                    {
                        list.Add(new(reactor.m_serialNumber));
                    }
                    return list;
                }
                set
                {
                }
            }
        }

        public class ReactorEntry
        {
            public ReactorEntry() { }

            public ReactorEntry(int id)
            {
                ID = id;
            }

            [FSSeparator]
            [FSDisplayName("名称")]
            [FSReadOnly]
            public string Name
            {
                get
                {
                    return $"REACTOR_{ID}";
                }
                set
                {
                }
            }

            [FSIgnore]
            public int ID { get; set; }

            [FSDisplayName("当前状态")]
            [FSReadOnly]
            public eReactorStatus CurrentStatus
            {
                get
                {
                    if (!ReactorsInLevel.TryGetValue(ID, out _))
                    {
                        return eReactorStatus.Inactive_Idle;
                    }
                    return ReactorsInLevel[ID].m_currentState.status;
                }
                set
                {
                }
            }

            [FSDisplayName("操作类别")]
            public eReactorInteraction Interaction
            {
                get
                {
                    return _interaction;
                }
                set
                {
                    AttemptInteraction(ReactorsInLevel[ID].m_serialNumber, value);
                    _interaction = value;
                }
            }

            private eReactorInteraction _interaction;

            [FSDisplayName("当前序列")]
            [FSReadOnly]
            public int CurrentWaveCount
            {
                get
                {
                    if (!ReactorsInLevel.TryGetValue(ID, out var reactor))
                    {
                        return -1;
                    }
                    return reactor.m_currentWaveCount;
                }
                set
                {
                }
            }

            [FSDisplayName("查看所有秘钥")]
            [FSIdentifier("验证秘钥")]
            public bool ShowAllOverrideCodes { get; set; }

            [FSDisplayName("验证秘钥")]
            [FSIdentifier("验证秘钥")]
            [FSReadOnly]
            public List<ReactorCodesEntry> OverrideCodes
            {
                get
                {
                    if (!ReactorsInLevel.TryGetValue(ID, out var Reactor))
                    {
                        return new();
                    }
                    List<string> codes = Reactor.GetOverrideCodes().ToList();
                    List<ReactorCodesEntry> list = new();
                    int count = ShowAllOverrideCodes ? codes.Count : Reactor.m_waveCountMax;
                    for (int i = 0; i < count; i++)
                    {
                        list.Add(new(i + 1, codes[i]));
                    }
                    return list;
                }
                set
                {
                }
            }

            [FSDisplayName("终端")]
            [FSReadOnly]
            public string LinkedTerminal
            {
                get
                {
                    if (!ReactorsInLevel.TryGetValue(ID, out var reactor) || reactor.m_terminal == null)
                    {
                        return "无";
                    }
                    return $"TERMINAL_{reactor.m_terminal.m_serialNumber}";
                }
                set
                {
                }
            }
        }

        public class ReactorCodesEntry
        {
            public ReactorCodesEntry(int round, string code)
            {
                Round = round;
                Code = code;
            }

            [FSSeparator]
            [FSDisplayName("波次")]
            [FSReadOnly]
            public int Round { get; set; }

            [FSDisplayName("验证秘钥")]
            [FSReadOnly]
            public string Code { get; set; }
        }

        private static void AttemptInteraction(int id, eReactorInteraction interaction)
        {
            ReactorsInLevel[id].AttemptInteract(interaction, 0f);
        }

        [ArchivePatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
        private class LG_WardenObjective_Reactor_OnBuildDone_Patch
        {
            private static void Postfix(LG_WardenObjective_Reactor __instance)
            {
                if (!ReactorsInLevel.TryAdd(__instance.m_serialNumber, __instance))
                {
                    ReactorsInLevel[__instance.m_serialNumber] = __instance;
                }
            }
        }

        [ArchivePatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnDestroy))]
        private class LG_WardenObjective_Reactor_OnDestroy_Patch
        {
            private static void Prefix(LG_WardenObjective_Reactor __instance)
            {
                ReactorsInLevel.Remove(__instance.m_serialNumber);
            }
        }

        public override void Init()
        {
            DevConsole.AddCommand(Command.Create<int>("ReactorGetCodes", "反应堆秘钥", "查询反应堆秘钥", Parameter.Create("ID", "反应堆编号"), ShowReactorCodes));
        }

        private static void ShowReactorCodes(int id)
        {
            if (!ReactorsInLevel.TryGetValue(id, out var reactor))
            {
                DevConsole.LogError($"不存在 REACTOR_{id}");
                return;
            }
            var codes = reactor.GetOverrideCodes();
            StringBuilder sb = new(200);
            sb.AppendLine($"<color=orange>REACTOR_{id} 验证秘钥：</color>");
            for (int i = 0; i < reactor.m_waveCountMax; i++)
            {
                sb.AppendLine($"{i + 1}. {codes[i]}");
            }
            DevConsole.Log(sb.ToString());
        }
    }
}
