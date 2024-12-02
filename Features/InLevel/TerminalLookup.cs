using Hikaria.AdminSystem.Utilities;
using Hikaria.AdminSystem.Utility;
using Hikaria.QC;
using Hikaria.QC.Actions;
using LevelGeneration;
using System.Collections.Generic;
using System.Text;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;


namespace Hikaria.AdminSystem.Features.InLevel
{
    [DoNotSaveToConfig]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [CommandPrefix("Terminal")]
    public class TerminalLookup : Feature
    {
        public override string Name => "终端";

        public override string Description => base.Description;

        public override FeatureGroup Group => EntryPoint.Groups.InLevel;

        private static Dictionary<int, LG_ComputerTerminal> TerminalsInLevel = new();

        [ArchivePatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.Setup))]
        private class LG_ComputerTerminal__Setup__Patch
        {
            private static void Postfix(LG_ComputerTerminal __instance)
            {
                TerminalsInLevel[__instance.m_serialNumber] = __instance;
            }
        }

        [ArchivePatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.OnDestroy))]
        private class LG_ComputerTerminal__OnDestroy__Patch
        {
            private static void Prefix(LG_ComputerTerminal __instance)
            {
                TerminalsInLevel.Remove(__instance.m_serialNumber);
            }
        }

        [Command("Password")]
        private static void GetTerminalPassword([TerminalInLevel] int id)
        {
            if (!TerminalsInLevel.TryGetValue(id, out var terminal))
            {
                ConsoleLogs.LogToConsole($"不存在 TERMINAL_{id}", LogLevel.Error);
                return;
            }
            if (string.IsNullOrEmpty(terminal.m_password))
            {
                ConsoleLogs.LogToConsole($"TERMINAL_{id}没有密码", LogLevel.Error);
                return;
            }
            ConsoleLogs.LogToConsole($"<color=orange>TERMINAL_{id} </color><color=green> 解锁密码:{terminal.m_password}</color>");
        }

        [Command("Uplinkcodes")]
        private static void GetTerminalUplinkCodes([TerminalInLevel] int id)
        {
            if (!TerminalsInLevel.TryGetValue(id, out var terminal) || terminal.UplinkPuzzle?.m_rounds?.Count == 0)
            {
                ConsoleLogs.LogToConsole($"TERMINAL_{id} 不需要上行链路秘钥");
                return;
            }
            ConsoleLogs.LogToConsole($"<color=orange>TERMINAL_{id} 上行验证秘钥：</color>");
            for (int i = 0; i < terminal.UplinkPuzzle.m_rounds.Count; i++)
            {
                ConsoleLogs.LogToConsole($"<color=orange>{i + 1}: {terminal.UplinkPuzzle.m_rounds[i].CorrectCode}</color>");
            }
        }

        [Command("UplinkStart")]
        private static void StartTerminalUplink([TerminalInLevel] int id)
        {
            if (!TerminalsInLevel.TryGetValue(id, out var terminal) || terminal.UplinkPuzzle?.m_rounds?.Count == 0)
            {
                ConsoleLogs.LogToConsole($"TERMINAL_{id} 没有上行链路");
                return;
            }

            if (!terminal.m_command.TryGetCommand("UPLINK_CONNECT", out var command, out var text1, out var text2))
            {
                ConsoleLogs.LogToConsole("非法指令, 请重新输入", LogLevel.Error);
                return;
            }
            terminal.m_command.TryGetCommandName(TERM_Command.TerminalUplinkConnect, out var commandName);
            LG_ComputerTerminalManager.WantToSendTerminalCommand(terminal.SyncID, TERM_Command.TerminalUplinkConnect, $"{commandName} {terminal.UplinkPuzzle.TerminalUplinkIP}", terminal.UplinkPuzzle.TerminalUplinkIP, string.Empty);
        }

        [Command("ListLogs")]
        private static void ListTerminalLogs([TerminalInLevel] int id)
        {
            if (!TerminalsInLevel.TryGetValue(id, out var terminal) || terminal.m_localLogs?.Count == 0)
            {
                ConsoleLogs.LogToConsole($"TERMINAL_{id} 不存在可读取的Log", LogLevel.Error);
                return;
            }
            ConsoleLogs.LogToConsole("----------------------------------------------------------------");
            ConsoleLogs.LogToConsole($"<color=orange>TERMINAL_{id} </color>存在以下可读取的日志:");
            int count = 1;
            foreach (var kvp in terminal.m_localLogs)
            {
                var fileName = kvp.Value.FileName?.ToUpperInvariant() ?? kvp.Key.ToUpperInvariant();
                ConsoleLogs.LogToConsole($"{count}. {fileName}");
                count++;
            }
            ConsoleLogs.LogToConsole("----------------------------------------------------------------");
        }

        [Command("ReadLog")]
        private static IEnumerator<ICommandAction> ReadTerminalLogContentText([TerminalInLevel] int id)
        {
            if (!TerminalsInLevel.TryGetValue(id, out var terminal) || terminal.m_localLogs?.Count == 0)
            {
                yield return new Value($"<color=red>TERMINAL_{id} 不存在可读取的日志</color>");
                yield break;
            }
            List<string> avaliableLogs = new List<string>();
            foreach (var kvp in terminal.m_localLogs)
            {
                if (kvp.Key.ToUpperInvariant() != "AUTO_GEN_STATUS.LOG")
                    avaliableLogs.Add(kvp.Value.FileName?.ToUpperInvariant() ?? kvp.Key.ToUpperInvariant());
            }
            if (avaliableLogs.Count == 0)
            {
                yield return new Value($"<color=red>TERMINAL_{id} 不存在可读取的日志</color>");
                yield break;
            }
            yield return new Value("选择一个日志");
            string selectedLogFileName = string.Empty;
            yield return new Choice<string>(avaliableLogs, s => selectedLogFileName = s);
            StringBuilder sb = new(200);
            sb.AppendLine("----------------------------------------------------------------");
            sb.AppendLine($"<color=orange>日志: {selectedLogFileName}</color>");
            sb.AppendLine(terminal.m_localLogs[selectedLogFileName].FileContent.ToString());
            sb.AppendLine("----------------------------------------------------------------");
            yield return new Value(sb.ToString());
        }

        [Command("SendCommand")]
        private static IEnumerator<ICommandAction> SendCommandToTerminal([TerminalInLevel] int id)
        {
            if (!TerminalsInLevel.TryGetValue(id, out var terminal))
            {
                yield return new Value($"<color=red>不存在 TERMINAL_{id}</color>");
                yield break;
            }

            yield return new Value("选择一个指令");
            List<string> avaliableCommands = new();
            foreach (var kvp in terminal.m_command.m_commandsPerString)
                avaliableCommands.Add(kvp.Key.ToUpperInvariant());
            string selectedCommand = string.Empty;

            yield return new Choice<string>(avaliableCommands, s => selectedCommand = s);
        reinput:
            var payload = string.Empty;
            yield return new ReadLine(s => payload = s, config: new() { LogInput = false, InputPrompt = "" });
            var input = $"{selectedCommand} {payload}".ToUpperInvariant();
            if (!TerminalsInLevel[id].m_command.TryGetCommand(input, out var cmdEnum, out string text, out string text2))
            {
                yield return new Value("<color=red>非法指令, 请重新输入</color>");
                goto reinput;
            }
            LG_ComputerTerminalManager.WantToChangeTerminalState(terminal.SyncID, TERM_State.Awake, AdminUtils.LocalPlayerAgent);
            LG_ComputerTerminalManager.WantToSendTerminalCommand(terminal.SyncID, cmdEnum, input, text, text2);
            yield return new Value($"<color=orange>已发送指令到 TERMINAL_{id}:</color>\n<color=orange>{input}</color>");
        }

        [Command("ListCommands")]
        private static void ListTerminalCommands([TerminalInLevel] int id)
        {
            ConsoleLogs.LogToConsole("----------------------------------------------------------------");
            ConsoleLogs.LogToConsole($"<color=orange>TERMINAL_{id} 存在以下可用命令:</color>");
            int num = 1;
            foreach (var command in TerminalsInLevel[id].m_command.m_commandsPerString)
            {
                ConsoleLogs.LogToConsole($"{num}. {command.key.ToUpperInvariant()}, {TerminalsInLevel[id].m_command.m_commandHelpStrings[command.Value].ToString().ToUpperInvariant()}");
                num++;
            }
            ConsoleLogs.LogToConsole("----------------------------------------------------------------");
        }

        public struct TerminalInLevelTag : IQcSuggestorTag
        {

        }

        public sealed class TerminalInLevelAttribute : SuggestorTagAttribute
        {
            private readonly IQcSuggestorTag[] _tags = { new TerminalInLevelTag() };

            public override IQcSuggestorTag[] GetSuggestorTags()
            {
                return _tags;
            }
        }

        public class TerminalInLevelSuggestor : BasicCachedQcSuggestor<int>
        {
            protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options)
            {
                return context.HasTag<TerminalInLevelTag>();
            }

            protected override IQcSuggestion ItemToSuggestion(int item)
            {
                return new RawSuggestion(item.ToString());
            }

            protected override IEnumerable<int> GetItems(SuggestionContext context, SuggestorOptions options)
            {
                return TerminalsInLevel.Keys;
            }
        }
    }
}
