using AIGraph;
using BepInEx;
using Clonesoft.Json;
using GameData;
using Hikaria.AdminSystem.Extensions;
using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using LevelGeneration;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using TheArchive.Core.FeaturesAPI.Settings;
using static TheArchive.Features.Dev.ModSettings;


namespace Hikaria.AdminSystem.Features.InLevel
{
    [DoNotSaveToConfig]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class TerminalLookup : Feature
    {
        public override string Name => "终端";

        public override string Description => base.Description;

        public override FeatureGroup Group => EntryPoint.Groups.InLevel;

        public static TerminalLookup Instance { get; private set; }

        [FeatureConfig]
        [JsonIgnore]
        public static TerminalSettings Settings { get; set; }

        private static Il2CppSystem.Collections.Generic.Dictionary<int, Il2CppSystem.Collections.Generic.List<TerminalUplinkPuzzleRound>> TerminalCodePuzzle = new();

        private static Dictionary<int, Dictionary<string, TerminalLogFileData>> TerminalLogs = new();

        private static Il2CppSystem.Collections.Generic.Dictionary<int, LG_ComputerTerminal> TerminalsInLevel = new();

        public class TerminalSettings
        {
            [FSDisplayName("目标终端序列号")]
            public int TerminalID { get; set; }

            [FSDisplayName("查询终端信息")]
            public FButton SearchTerminal { get; set; } = new FButton("查询", "查询终端信息");
            [FSDisplayName("终端信息")]
            [FSReadOnly]
            public List<TerminalSettingsEntry> TargetTerminal { get; set; }
        }

        public class TerminalSettingsEntry
        {
            public TerminalSettingsEntry(int id)
            {
                ID = id;
            }

            [FSDisplayName("名称")]
            [FSReadOnly]
            public string Name
            {
                get
                {
                    return $"TERMINAL_{ID}";
                }
                set
                {
                }
            }

            [FSIgnore]
            public int ID { get; set; }


            [FSDisplayName("区域")]
            [FSReadOnly]
            public string Zone
            {
                get
                {
                    return $"ZONE_{TerminalsInLevel[ID].SpawnNode.m_zone.Alias} {TerminalsInLevel[ID].SpawnNode.m_area.m_navInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore)}";
                }
                set
                {
                }
            }

            [FSDisplayName("状态")]
            public TERM_State CurrentState
            {
                get
                {
                    return TerminalsInLevel[ID].CurrentStateName;
                }
                set
                {
                    LG_ComputerTerminalManager.WantToChangeTerminalState(TerminalsInLevel[ID].SyncID, value, AdminUtils.LocalPlayerAgent);
                }
            }

            [FSDisplayName("解锁密码")]
            [FSReadOnly]
            public List<TerminalPasswordEntry> Password
            {
                get
                {
                    List<TerminalPasswordEntry> entry = new();
                    List<TerminalEntry> fromTerminals = new();
                    string password = TerminalsInLevel[ID].m_password;
                    if (!password.IsNullOrWhiteSpace())
                    {
                        var passwordLinkJob = TerminalsInLevel[ID].m_passwordLinkerJob;
                        if (passwordLinkJob != null)
                        {
                            foreach (var terminal in passwordLinkJob.m_terminalsWithPasswordParts)
                            {
                                fromTerminals.Add(new(terminal.m_serialNumber));
                            }
                        }
                    }
                    else
                    {
                        password = "无";
                    }
                    entry.Add(new(fromTerminals, password));
                    return entry;
                }
                set
                {
                }
            }

            [FSHeader("上行链路")]
            [FSDisplayName("上行链路IP")]
            [FSIdentifier("上行链路")]
            [FSReadOnly]
            public string UplinkIP
            {
                get
                {
                    if (TerminalsInLevel[ID].UplinkPuzzle == null)
                    {
                        return "无";
                    }
                    return TerminalsInLevel[ID].UplinkPuzzle.TerminalUplinkIP;
                }
                set
                {
                }
            }

            [FSDisplayName("上行链路第二终端")]
            [FSIdentifier("上行链路")]
            [FSReadOnly]
            public string CorruptedUplinkReceiver
            {
                get
                {
                    if (TerminalsInLevel[ID].CorruptedUplinkReceiver == null)
                    {
                        return "无";
                    }
                    return $"TERMINAL_{TerminalsInLevel[ID].CorruptedUplinkReceiver.m_serialNumber}";
                }
                set
                {
                }
            }


            [FSDisplayName("上行链接验证秘钥列表")]
            [FSIdentifier("上行链路")]
            [FSReadOnly]
            public List<TerminalUplinkCodeEntry> UplinkCodes
            {
                get
                {
                    List<TerminalUplinkCodeEntry> list = new();
                    int index = 1;
                    if (TerminalsInLevel[ID].UplinkPuzzle != null)
                    {
                        foreach (var round in TerminalsInLevel[ID].UplinkPuzzle.m_rounds)
                        {
                            list.Add(new(index, round.CorrectCode));
                            index++;
                        }
                    }
                    return list;
                }
                set
                {
                }
            }

            [FSHeader("终端指令")]
            [FSDisplayName("指令列表")]
            [FSIdentifier("终端指令")]
            [FSReadOnly]
            public List<TerminalCommandEntry> Commands
            {
                get
                {
                    List<TerminalCommandEntry> list = new();
                    int num = 1;
                    foreach (var command in TerminalsInLevel[ID].m_command.m_commandsPerString)
                    {
                        List<TerminalCommandHelpTextEntry> sublist = new();
                        string[] helps = TerminalsInLevel[ID].m_command.m_commandHelpStrings[command.Value].ToString().Split("\\n");
                        foreach (var str in helps)
                        {
                            sublist.Add(new(str));
                        }
                        list.Add(new(num, command.key, sublist));
                        num++;
                    }
                    return list;
                }
                set
                {
                }
            }

            [FSDisplayName("指令参数")]
            [FSIdentifier("终端指令")]
            public string CommandParams { get; set; }

            [FSDisplayName("执行指令")]
            [FSIdentifier("终端指令")]
            public FButton ExcuteCommand { get; set; } = new FButton("执行", "执行指令");


            [FSHeader("终端日志")]
            [FSDisplayName("查看日志")]
            [FSIdentifier("终端日志")]
            [FSReadOnly]
            public List<TerminalLogEntry> Logs
            {
                get
                {
                    if (TerminalLogEntry.LogContentPanel != null)
                    {
                        TerminalLogEntry.LogContentPanel.Hide();
                    }
                    var list = new List<TerminalLogEntry>();
                    foreach (var pair in TerminalsInLevel[ID].GetLocalLogs())
                    {
                        if (!pair.Key.IsNullOrEmptyOrWhiteSpace() && TerminalsInLevel[ID].m_logVisibleMap.ContainsKey(pair.Key) && TerminalsInLevel[ID].m_logVisibleMap[pair.Key])
                        {
                            list.Add(new(pair.Value));
                        }
                    }
                    return list;
                }
                set
                {
                }
            }
        }

        public class TerminalLogEntry
        {
            public TerminalLogEntry(TerminalLogFileData logFileData)
            {
                LogFileData = logFileData;
                if (LogContentPanel == null)
                {
                    LogContentPanel = new();
                }
                LogContentPanelData.Title = LogFileData.FileName;
                LogContentPanelData.Description = LogFileData.FileContent.ToString();
                ShowContent = new("查看", $"READ_{LogFileData.FileName}", new(() =>
                {
                    if (!PanelIsShow)
                    {
                        PanelIsShow = true;
                        LogContentPanel.Hide();
                        LogContentPanel.Show(LogContentPanelData);
                    }
                    else
                    {
                        PanelIsShow = false;
                        LogContentPanel.Hide();
                    }
                }));
            }

            ~TerminalLogEntry()
            {
                PanelIsShow = false;
                LogContentPanel.Hide();
                LogContentPanel.Dispose();
            }

            private static bool PanelIsShow;
            private TerminalLogFileData LogFileData;
            public static DescriptionPanel LogContentPanel = new();
            private static DescriptionPanel.DescriptionPanelData LogContentPanelData = new();

            [FSSeparator]
            [FSDisplayName("文件名")]
            [FSReadOnly]
            public string FileName
            {
                get
                {
                    return LogFileData.FileName;
                }
                set
                {
                }
            }

            [FSDisplayName("内容")]
            public FButton ShowContent { get; set; }

        }

        public class TerminalEntry
        {
            public TerminalEntry(int id)
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
                    return $"TERMINAL_{ID}";
                }
                set
                {
                }
            }

            [FSDisplayName("区域")]
            [FSReadOnly]
            public string Zone
            {
                get
                {
                    return $"ZONE_{TerminalsInLevel[ID].SpawnNode.m_zone.Alias} {TerminalsInLevel[ID].SpawnNode.m_area.m_navInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore)}";
                }
                set
                {
                }
            }

            [FSIgnore]
            public int ID { get; set; }
        }

        public class TerminalPasswordEntry
        {
            public TerminalPasswordEntry(List<TerminalEntry> fromTerminal, string password)
            {
                PasswordPartTerminals = fromTerminal;
                Password = password;
            }

            [FSDisplayName("解锁密码")]
            [FSReadOnly]
            public string Password { get; set; }

            [FSDisplayName("密码来源")]
            [FSReadOnly]
            public List<TerminalEntry> PasswordPartTerminals { get; set; }
        }

        public class TerminalUplinkCodeEntry
        {
            public TerminalUplinkCodeEntry(int index, string code)
            {
                Index = index;
                Code = code;
            }

            [FSSeparator]
            [FSDisplayName("轮次")]
            [FSReadOnly]
            public int Index { get; set; }
            [FSDisplayName("验证秘钥")]
            [FSReadOnly]
            public string Code { get; set; }
        }

        public class TerminalCommandEntry
        {
            public TerminalCommandEntry(int index, string commandName, List<TerminalCommandHelpTextEntry> helpText)
            {
                Index = index;
                CommandName = commandName;
                HelpText = helpText;
            }

            [FSSeparator]
            [FSDisplayName("序号")]
            [FSReadOnly]
            public int Index { get; set; }
            [FSDisplayName("命令名称")]
            [FSReadOnly]
            public string CommandName { get; set; }

            [FSDisplayName("帮助信息")]
            [FSReadOnly]
            [FSInline]
            public List<TerminalCommandHelpTextEntry> HelpText { get; set; }
        }

        public class TerminalCommandHelpTextEntry
        {
            public TerminalCommandHelpTextEntry(string helpText)
            {
                HelpText = helpText;
            }

            [FSDisplayName("帮助信息")]
            [FSReadOnly]
            public string HelpText { get; set; }
        }

        public override void Init()
        {
            Instance = this;

            DevConsole.AddCommand(Command.Create<int>("TerminalGetUplinkcodes", "获取终端上行验证密码", "获取终端上行验证密码", Parameter.Create("ID", "终端编号"), GetTerminalUplinkCodes));
            DevConsole.AddCommand(Command.Create<int>("TerminalGetPassword", "获取终端解锁密码", "获取终端解锁密码", Parameter.Create("ID", "终端编号"), GetTerminalPassword));
            DevConsole.AddCommand(Command.Create<int>("TerminalListCommands", "获取终端指令", "获取终端可用指令", Parameter.Create("ID", "终端编号"), ListTerminalCommands));
            DevConsole.AddCommand(Command.Create<int>("TerminalListLogs", "列出终端日志", "列出终端日志", Parameter.Create("ID", "终端编号"), ListTerminalLogs));
            DevConsole.AddCommand(Command.Create<int, string>("TerminalReadLog", "获取终端日志", "获取终端日志", Parameter.Create("ID", "终端编号"), Parameter.Create("LogFileName", "日志文件名"), ReadTerminalLogContentText));
            DevConsole.AddCommand(Command.Create<int, string>("TerminalSendCommand", "发送终端指令", "发送指令到终端", Parameter.Create("ID", "终端编号"), Parameter.Create("Command", "终端指令"), SendCommandToTerminal));
            DevConsole.AddCommand(Command.Create<int>("TerminalsInZone", "获取区域内所有终端", "获取区域内所有终端", Parameter.Create("ZoneID", "区域编号"), GetTerminalInZone));

            DevConsole.AddCommand(Command.Create<string>("ItemQuery", "查询物品", "查询物品", Parameter.Create("itemName", "物品名称"), QueryItem));
            DevConsole.AddCommand(Command.Create<string>("ItemPing", "标记物品", "标记物品", Parameter.Create("itemName", "物品名称"), PingItem));
            DevConsole.AddCommand(Command.Create<string>("ItemList", "列出物品", "列出物品", Parameter.Create("Param", "参数, 用','隔开"), p =>
            {
                var input = p.Split(',');
                ListItem(input[0], input.Length > 1 ? input[1] : string.Empty);
            }));
        }

        public override void OnButtonPressed(ButtonSetting setting)
        {
            if (setting.ButtonID == "查询终端信息")
            {
                if (Settings.TargetTerminal == null)
                {
                    Settings.TargetTerminal = new();
                }
                Settings.TargetTerminal.Clear();
                Settings.TargetTerminal.Add(new(Settings.TerminalID));
            }
            if (setting.ButtonID == "执行指令")
            {
                if (Settings.TargetTerminal == null)
                {
                    return;
                }
                var terminalEntry = Settings.TargetTerminal[0];
                uint syncID = TerminalsInLevel[terminalEntry.ID].m_syncID;
                string inputstring = terminalEntry.CommandParams.ToUpperInvariant();
                LG_ComputerTerminalManager.WantToChangeTerminalState(syncID, TERM_State.Awake, AdminUtils.LocalPlayerAgent);
                if (!TerminalsInLevel[terminalEntry.ID].m_command.TryGetCommand(inputstring, out TERM_Command term_Command, out string text, out string text2))
                {
                    DevConsole.LogError("非法指令, 请重新输入");
                    return;
                }
                LG_ComputerTerminalManager.WantToSendTerminalCommand(syncID, term_Command, inputstring, text, text2);
            }
        }

        [ArchivePatch(typeof(TerminalUplinkPuzzle), nameof(TerminalUplinkPuzzle.Setup))]
        private class TerminalUplinkPuzzle__Setup__Patch
        {
            private static void Postfix(TerminalUplinkPuzzle __instance, LG_ComputerTerminal terminal, int roundIndex = -1)
            {
                if (!TerminalCodePuzzle.TryAdd(terminal.m_serialNumber, __instance.m_rounds))
                {
                    TerminalCodePuzzle[terminal.m_serialNumber] = __instance.m_rounds;
                }
            }
        }

        [ArchivePatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.AddLocalLog))]
        private class LG_ComputerTerminal__AddLocalLog__Patch
        {
            private static void Postfix(LG_ComputerTerminal __instance, TerminalLogFileData data, bool visible)
            {
                if (visible)
                {
                    if (!TerminalLogs.TryGetValue(__instance.m_serialNumber, out Dictionary<string, TerminalLogFileData> value))
                    {
                        Dictionary<string, TerminalLogFileData> fileName2DataDict = new()
                        {
                            { data.FileName, data }
                        };
                        value = fileName2DataDict;
                        TerminalLogs.Add(__instance.m_serialNumber, value);
                        return;
                    }

                    value.AutoAdd(data.FileName, data);
                }
            }
        }


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
                TerminalLogs.Remove(__instance.m_serialNumber);
                TerminalCodePuzzle.Remove(__instance.m_serialNumber);
            }
        }

        private static void GetTerminalPassword(int id)
        {
            if (!TerminalsInLevel.ContainsKey(id))
            {
                DevConsole.LogError($"不存在 TERMINAL_{id}");
                return;
            }
            var terminal = TerminalsInLevel[id];
            if (string.IsNullOrEmpty(terminal.m_password))
            {
                DevConsole.LogError($"TERMINAL_{id}没有密码");
                return;
            }
            DevConsole.Log($"<color=orange>TERMINAL_{id} </color><color=green> 解锁密码:{terminal.m_password}</color>");
        }

        private static void GetTerminalUplinkCodes(int id)
        {
            if (!TerminalCodePuzzle.ContainsKey(id))
            {
                DevConsole.LogError($"TERMINAL_{id} 不需要上行链路秘钥");
                return;
            }
            DevConsole.Log($"<color=orange>TERMINAL_{id} 上行验证秘钥：</color>");
            for (int i = 0; i < TerminalCodePuzzle[id].Count; i++)
            {
                DevConsole.Log($"<color=orange>{i + 1}: {TerminalCodePuzzle[id][i].CorrectCode}</color>");
            }
        }

        private static void ListTerminalLogs(int id)
        {
            if (!TerminalLogs.TryGetValue(id, out Dictionary<string, TerminalLogFileData> value) || value.Count == 0)
            {
                DevConsole.LogError($"TERMINAL_{id} 不存在可读取的Log");
                return;
            }
            DevConsole.Log("----------------------------------------------------------------");
            DevConsole.Log($"<color=orange>TERMINAL_{id} </color><color=green>存在以下可读取的Log:</color>");
            int count = 1;
            foreach (string key in value.Keys)
            {
                DevConsole.Log($"{count}. {key}");
                count++;
            }
            DevConsole.Log("----------------------------------------------------------------");
        }

        private static void ReadTerminalLogContentText(int id, string logFileName)
        {
            if (!TerminalLogs.TryGetValue(id, out Dictionary<string, TerminalLogFileData> value) || value.Count == 0)
            {
                DevConsole.LogError(("TERMINAL_{0} 不存在可读取的Log", id));
                return;
            }
            DevConsole.Log("----------------------------------------------------------------");
            DevConsole.Log($"<color=orange>Log File: {value[logFileName].FileName}</color>");
            string[] array = value[logFileName].FileContent.UntranslatedText.Split('\n');
            for (int i = 0; i < array.Length; i++)
            {
                DevConsole.Log(array[i]);
            }
            DevConsole.Log("----------------------------------------------------------------");
        }

        private static void SendCommandToTerminal(int id, string inputstring)
        {
            uint syncID = TerminalsInLevel[id].m_syncID;
            inputstring = inputstring.ToUpperInvariant();
            LG_ComputerTerminalManager.WantToChangeTerminalState(syncID, TERM_State.Awake, AdminUtils.LocalPlayerAgent);
            if (!TerminalsInLevel[id].m_command.TryGetCommand(inputstring, out TERM_Command term_Command, out string text, out string text2))
            {
                DevConsole.LogError("非法指令, 请重新输入");
                return;
            }
            LG_ComputerTerminalManager.WantToSendTerminalCommand(syncID, term_Command, inputstring, text, text2);
            DevConsole.Log($"<color=orange>已发送以下指令到 TERMINAL_{id}:</color>");
            DevConsole.Log($"<color=orange>{inputstring}</color>");
        }

        private static void ListTerminalCommands(int id)
        {
            DevConsole.Log("----------------------------------------------------------------");
            DevConsole.Log($"<color=orange>TERMINAL_{id} 存在以下可用命令:</color>");
            int num = 1;
            foreach (var command in TerminalsInLevel[id].m_command.m_commandsPerString)
            {
                DevConsole.Log($"{num}. {command.key}, {TerminalsInLevel[id].m_command.m_commandHelpStrings[command.Value].ToString()}");
                num++;
            }
            DevConsole.Log("----------------------------------------------------------------");
        }

        private static void GetTerminalInZone(int zoneNum)
        {
            bool flag = false;
            foreach (LG_ComputerTerminal terminal in TerminalsInLevel.Values)
            {
                if (terminal.SpawnNode.m_zone.Alias == zoneNum)
                {
                    DevConsole.Log($"<color=green>ZONE_{zoneNum}: TERMINAL_{terminal.m_serialNumber}</color>");
                    flag = true;
                }
            }
            if (!flag)
            {
                DevConsole.LogError($"不存在ZONE_{zoneNum}, 或该区域没有终端");
            }
        }

        private static void PingItem(string itemName)
        {
            itemName = itemName.ToUpperInvariant();
            if (!LG_LevelInteractionManager.TryGetTerminalInterface(itemName, AdminUtils.LocalPlayerAgent.DimensionIndex, out iTerminalItem iTerminalItem))
            {
                DevConsole.LogError($"不存在名为 {itemName} 的物品");
                return;
            }
            iTerminalItem.Cast<LG_GenericTerminalItem>().PlayPing();
        }

        private static void QueryItem(string itemName)
        {
            itemName = itemName.ToUpperInvariant();
            eDimensionIndex dimensionIndex = AdminUtils.LocalPlayerAgent.DimensionIndex;
            if (!LG_LevelInteractionManager.TryGetTerminalInterface(itemName, dimensionIndex, out iTerminalItem iTerminalItem))
            {
                DevConsole.LogError($"不存在名为 {itemName} 的物品");
                return;
            }
            Il2CppSystem.Collections.Generic.List<string> itemDetails = new();
            itemDetails.Add("ID: " + iTerminalItem.TerminalItemKey);
            itemDetails.Add("物品状态: " + iTerminalItem.FloorItemStatus);
            string locationText = iTerminalItem.FloorItemLocation;
            if (AIG_CourseNode.TryGetCourseNode(dimensionIndex, iTerminalItem.LocatorBeaconPosition, 1f, out var node))
            {
                locationText += $" Area_{node.m_area.m_navInfo.Suffix}";
            }
            itemDetails.Add("位置: " + locationText);
            itemDetails.Add("----------------------------------------------------------------");
            Il2CppSystem.Collections.Generic.List<string> fullDetails = iTerminalItem.GetDetailedInfo(itemDetails);
            foreach (string s in fullDetails)
            {
                DevConsole.Log(s);
            }
        }

        private static void ListItem(string param1, string param2 = "")
        {
            List<string> list = new();
            bool flag2 = param1 == string.Empty;
            bool flag3 = param1 != string.Empty;
            bool flag4 = param2 != string.Empty;
            if (flag2)
            {
                DevConsole.LogError("参数1不可为空");
                return;
            }
            list.Add("-----------------------------------------------------------------------------------");
            list.Add("ID                       物品类型             物品状态              位置");
            foreach (var keyValuePair in LG_LevelInteractionManager.Current.m_terminalItemsByKeyString)
            {
                if (keyValuePair.Value.ShowInFloorInventory)
                {
                    var terminalItem = keyValuePair.Value;
                    string locationInfo = terminalItem.FloorItemLocation;
                    if (AIG_CourseNode.TryGetCourseNode(terminalItem.LocatorBeaconPosition.GetDimension().DimensionIndex, terminalItem.LocatorBeaconPosition, 1f, out var node))
                    {
                        locationInfo += $" Area_{node.m_area.m_navInfo.Suffix}";
                    }
                    string text2 = string.Concat(new object[]
                    {
                        terminalItem.TerminalItemKey,
                        " ",
                        terminalItem.FloorItemType,
                        " ",
                        terminalItem.FloorItemStatus,
                        " ",
                        terminalItem.FloorItemLocation,
                        " ",
                        eFloorInventoryObjectBeaconStatus.NoBeacon.ToString()
                    });
                    bool flag5 = flag3 && text2.ToUpperInvariant().Contains(param1.ToUpperInvariant());
                    bool flag6 = flag4 && text2.ToUpperInvariant().Contains(param2.ToUpperInvariant());
                    bool flag7 = !flag3 && !flag4;
                    bool flag8 = (!flag3 || flag5) && (!flag4 || flag6);
                    if (flag7 || flag8)
                    {
                        list.Add(terminalItem.TerminalItemKey.FormatInLength(25) + terminalItem.FloorItemType.ToString().FormatInLength(20) + terminalItem.FloorItemStatus.ToString().FormatInLength(20) + locationInfo);
                    }
                }
            }
            list.Add("-----------------------------------------------------------------------------------");

            foreach (string s in list)
            {
                DevConsole.Log(s);
            }
        }
    }
}
