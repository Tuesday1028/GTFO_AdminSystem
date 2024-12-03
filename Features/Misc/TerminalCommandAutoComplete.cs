using AK;
using BepInEx.Unity.IL2CPP.Utils;
using LevelGeneration;
using System;
using System.Collections;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Utilities;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Misc
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class TerminalCommandAutoComplete : Feature
    {
        public override string Name => "自动完成指令";

        public override FeatureGroup Group => EntryPoint.Groups.Misc;

        [FeatureConfig]
        public static TerminalCommandAutoCompleteSettings Settings { get; set; }

        public class TerminalCommandAutoCompleteSettings
        {
            [FSDisplayName("自动指令")]
            public bool EnableAutoCommand { get; set; }
            [FSDisplayName("禁用指令验证")]
            public bool DisableCodeValiation { get; set; }
        }

        [ArchivePatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.EvaluateInput))]
        private class LG_ComputerTerminalCommandInterpreter__EvaluateInput__Patch
        {
            private static bool Prefix(LG_ComputerTerminalCommandInterpreter __instance, ref string inputString)
            {
                return !EvaluateInput(__instance, inputString, out inputString);
            }
        }

        [ArchivePatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.TryUnlockingTerminal))]
        private class LG_ComputerTerminalCommandInterpreter__TryUnlockingTerminal__Patch
        {
            private static void Prefix(LG_ComputerTerminalCommandInterpreter __instance, ref string param)
            {
                if (Settings.DisableCodeValiation)
                {
                    param = __instance.m_terminal.m_password.ToUpperInvariant();
                    return;
                }

                if (Settings.EnableAutoCommand && !string.IsNullOrEmpty(param) && param.Equals("UNLOCK", StringComparison.OrdinalIgnoreCase))
                {
                    param = __instance.m_terminal.m_password.ToUpperInvariant();
                    return;
                }
            }
        }

        private static bool EvaluateInput(LG_ComputerTerminalCommandInterpreter command, string inputString, out string result)
        {
            result = inputString;
            if (string.IsNullOrEmpty(inputString))
                return false;
            result = inputString.ToUpperInvariant();
            command.m_terminal.m_sound.Post(EVENTS.BUTTONGENERICPRESS, true);
            command.m_inputBuffer.Add(inputString);
            command.m_terminal.m_caretBlinkOffsetFromEnd = 0;
            command.m_inputBufferStep = 0;
            if (command.m_inputBuffer.Count > 10)
            {
                command.m_inputBuffer.RemoveAt(0);
            }
            while (Settings.EnableAutoCommand)
            {
                if (!command.TryGetCommand(inputString, out var term_Command, out _, out _) || term_Command == TERM_Command.None)
                {
                    var input = inputString.ToUpperInvariant().Split(' ').ToList();
                    if (input.Count <= 1 || input[0] != "AUTO")
                        break;
                    var terminal = command.m_terminal;
                    if (terminal == null)
                        break;
                    switch (input[1])
                    {
                        case "REACTOR_VERIFY":
                            var reactor = terminal.ConnectedReactor;
                            var state = reactor?.m_currentState.status ?? eReactorStatus.Inactive_Idle;
                            if (reactor == null || state != eReactorStatus.Startup_waitForVerify && state != eReactorStatus.Shutdown_waitForVerify)
                                break;
                            var code = reactor.GetOverrideCodes()[reactor.m_currentWaveCount - 1].ToUpperInvariant();
                            LG_ComputerTerminalManager.WantToSendTerminalCommand(terminal.SyncID, TERM_Command.ReactorVerify, $"{command.m_commandsPerEnum[TERM_Command.ReactorVerify].ToUpperInvariant()} {code}",
                                code, string.Empty);
                            return true;
                        case "UPLINK_VERIFY":
                            var puzzle = terminal.UplinkPuzzle;
                            if (puzzle == null || puzzle.Solved)
                                break;
                            LG_ComputerTerminalManager.WantToSendTerminalCommand(terminal.SyncID, TERM_Command.ReactorVerify,
                                $"{command.m_commandsPerEnum[TERM_Command.ReactorVerify].ToUpperInvariant()} {puzzle.CurrentRound.CorrectCode.ToUpperInvariant()}",
                               puzzle.CurrentRound.CorrectCode.ToUpperInvariant(), string.Empty);
                            return true;
                        case "UPLINK_COMPLETE":
                            var puzzle1 = terminal.UplinkPuzzle;
                            if (puzzle1 == null || puzzle1.Solved)
                                break;
                            terminal.StartCoroutine(AutoCompleteUplink(terminal));
                            return true;
                    }
                }
                break;
            }
            while (Settings.DisableCodeValiation)
            {
                if (!command.TryGetCommand(inputString, out var term_Command, out _, out _))
                    break;
                var input = inputString.ToUpperInvariant().Split(' ').ToList();
                if (input.Count == 0)
                    break;
                if (input.Count == 1)
                    input.Add(string.Empty);
                var terminal = command.m_terminal;
                if (terminal == null)
                    break;
                switch (term_Command)
                {
                    case TERM_Command.ReactorVerify:
                        var reactor = terminal.ConnectedReactor;
                        if (reactor == null)
                            break;
                        input[1] = reactor.GetOverrideCodes()[reactor.m_currentWaveCount - 1].ToUpperInvariant();
                        result = string.Join(' ', input);
                        return false;
                    case TERM_Command.TerminalCorruptedUplinkVerify:
                    case TERM_Command.TerminalUplinkVerify:
                        var puzzle = terminal.UplinkPuzzle;
                        if (puzzle == null || puzzle.Solved)
                            break;
                        input[1] = puzzle.CurrentRound.CorrectCode.ToUpperInvariant();
                        result = string.Join(' ', input);
                        return false;
                    case TERM_Command.TerminalCorruptedUplinkConnect:
                    case TERM_Command.TerminalUplinkConnect:
                        var puzzle1 = terminal.UplinkPuzzle;
                        if (puzzle1 == null || puzzle1.Solved)
                            break;
                        input[1] = puzzle1.TerminalUplinkIP;
                        result = string.Join(' ', input);
                        return false;
                    case TERM_Command.TryUnlockingTerminal:
                        input[1] = terminal.m_password.ToUpperInvariant();
                        result = string.Join(' ', input);
                        return false;
                }
                break;
            }
            return false;
        }

        private static IEnumerator AutoCompleteUplink(LG_ComputerTerminal terminal)
        {
            var yielder = new WaitForSecondsRealtime(1f);
            var puzzle = terminal.UplinkPuzzle;
            if (puzzle == null || puzzle.Solved)
                yield break;
            var command = terminal.m_command;
            if (command == null)
                yield break;
            if (!puzzle.Connected)
            {
                var connect_Command = puzzle.IsCorrupted ? TERM_Command.TerminalCorruptedUplinkConnect : TERM_Command.TerminalUplinkConnect;
                LG_ComputerTerminalManager.WantToSendTerminalCommand(terminal.SyncID, connect_Command, $"{command.m_commandsPerEnum[connect_Command].ToUpperInvariant()} {puzzle.TerminalUplinkIP}",
                    $"{puzzle.TerminalUplinkIP}", string.Empty);
            }
            while (!puzzle.Connected)
                yield return yielder;
            var term_Command = puzzle.IsCorrupted ? TERM_Command.TerminalCorruptedUplinkVerify : TERM_Command.TerminalUplinkVerify;
            var index = 0;
            var rounds = puzzle.m_rounds.ToSystemList().ToDictionary(r => index++, r => r.CorrectCode.ToUpperInvariant());
            foreach (var round in rounds)
            {
                if (round.Key < puzzle.m_roundIndex)
                    continue;
                while (round.Key > puzzle.m_roundIndex)
                {
                    yield return yielder;
                }
                var code = rounds[puzzle.m_roundIndex];
                LG_ComputerTerminalManager.WantToSendTerminalCommand(terminal.SyncID, term_Command, $"{command.m_commandsPerEnum[term_Command].ToUpperInvariant()} {code}", $"{code}", string.Empty);
            }
        }
    }
}
