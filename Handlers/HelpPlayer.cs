#if false
using Agents;
using BepInEx.Unity.IL2CPP.Utils;
using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using Player;
using SNetwork;
using System;
using System.Collections;
using UnityEngine;

namespace Hikaria.AdminSystem.Handlers
{
    internal sealed class HelpPlayer : MonoBehaviour
    {
        private void Start()
        {
            this.StartCoroutine(StartUpdater());
        }

        private IEnumerator StartUpdater()
        {
            var yielder = new WaitForFixedUpdate();
            while (true)
            {
                this.StartCoroutine(UpdateHelper());
                yield return yielder;
            }
        }

        private static IEnumerator UpdateHelper()
        {
            if (helpInterval > 0.5f)
            {
                for (int slot = 0; slot < 4; slot++)
                {
                    if (needHelp[slot])
                    {
                        if (PlayerManager.TryGetPlayerAgent(ref slot, out PlayerAgent player))
                        {
                            if (player.NeedHealth())
                            {
                                player.GiveHealth(AdminUtils.LocalPlayerAgent, 1f);
                            }
                            if (player.NeedDisinfection())
                            {
                                player.GiveDisinfection(AdminUtils.LocalPlayerAgent, 1f);
                            }
                            if (player.NeedWeaponAmmo())
                            {
                                player.GiveAmmoRel(AdminUtils.LocalPlayerAgent, 1f, 1f, 0f);
                            }
                            if (player.NeedToolAmmo())
                            {
                                player.GiveAmmoRel(AdminUtils.LocalPlayerAgent, 0f, 0f, 1f);
                            }
                        }
                        else
                        {
                            needHelp[slot] = false;
                        }
                    }
                }
                helpInterval = 0f;
            }
            helpInterval += Time.fixedDeltaTime;

            if (glueInterval > 0.25f)
            {
                for (int slot = 0; slot < 4; slot++)
                {
                    if (needGlue[slot])
                    {
                        if (PlayerManager.TryGetPlayerAgent(ref slot, out PlayerAgent player))
                        {
                            float stepLength = 1f;
                            Vector3 planeNormal = player.Forward;
                            Vector3 planeRight = Vector3.Cross(Vector3.up, planeNormal).normalized;
                            Vector3 planeOrigin = player.EyePosition + player.Forward + Vector3.up * 0.5f;
                            for (int i = -1; i <= 1; i++)
                            {
                                for (int j = -1; j <= 1; j++)
                                {
                                    // 计算当前格子的坐标
                                    Vector3 currentPos = planeOrigin + i * stepLength * planeRight + j * stepLength * Vector3.up;
                                    ProjectileManager.WantToFireGlue(player, currentPos, player.TargetLookDir * 25f, 500f, true);
                                }
                            }
                        }
                        else
                        {
                            needGlue[slot] = false;
                        }
                    }
                }
                glueInterval = 0f;
            }
            glueInterval += Time.fixedDeltaTime;

            if (mineInterval > 0.5f)
            {
                for (int slot = 0; slot < 4; slot++)
                {
                    if (needMine[slot])
                    {
                        if (PlayerManager.TryGetPlayerAgent(ref slot, out PlayerAgent player))
                        {

                            ItemReplicationManager.SpawnItem(explosiveMine, null, ItemMode.Instance, player.Position + Vector3.up * 0.1f, Quaternion.LookRotation(Vector3.down), player.CourseNode, player);
                        }
                        else
                        {
                            needMine[slot] = false;
                        }
                    }
                }
                mineInterval = 0f;
            }
            mineInterval += Time.fixedDeltaTime;

            yield break;
        }

        private static void ToggleHelpPlayer(int slot)
        {
            if (AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent playerAgent))
            {
                needHelp[slot - 1] = !needHelp[slot - 1];
                DevConsole.Log($"<color={(needHelp[slot - 1] ? "green" : "red")}>{playerAgent.PlayerName} 已{(needHelp[slot - 1] ? "开始" : "停止")} Help</color>");
                return;
            }
            needHelp[slot - 1] = false;
            DevConsole.LogError($"不存在slot为 {slot} 的玩家");
        }

        private static void HelpAllPlayers(bool enable)
        {
            Array.Fill(needHelp, enable);
            DevConsole.Log($"<color={(enable ? "green" : "red")}>已{(enable ? "开始" : "停止")} Help 所有玩家</color>");
        }

        private static void ToggleGluePlayer(int slot)
        {
            if (AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent playerAgent))
            {
                needGlue[slot - 1] = !needGlue[slot - 1];
                DevConsole.Log($"<color={(needGlue[slot - 1] ? "green" : "red")}>{playerAgent.PlayerName} 已{(needGlue[slot - 1] ? "开始" : "停止")} FireGlue</color>");
                return;
            }
            needGlue[slot - 1] = false;
            DevConsole.LogError($"不存在Slot为 {slot} 的玩家");
        }

        private static void ToggleMinePlayer(int slot)
        {
            if (AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent playerAgent))
            {
                needMine[slot - 1] = !needMine[slot - 1];
                DevConsole.Log($"<color={(needMine[slot - 1] ? "green" : "red")}>{playerAgent.PlayerName} 已{(needMine[slot - 1] ? "开始" : "停止")} DeployMine</color>");
                return;
            }
            needMine[slot - 1] = false;
            DevConsole.LogError($"不存在slot为 {slot} 的玩家");
        }

        internal static void AddCommands()
        {
            if (isCommandAdded)
            {
                return;
            }
            isCommandAdded = true;

            DevConsole.AddCommand(Command.Create<int>("HelpPlayer", "帮助玩家", "帮助玩家", Parameter.Create("Slot", "槽位, 1-4"), ToggleHelpPlayer));
            DevConsole.AddCommand(Command.Create<bool>("HelpAllPlayer", "帮助所有玩家", "帮助所有玩家", Parameter.Create("Enable", "开启或关闭, True或False"), HelpAllPlayers));

            DevConsole.AddCommand(Command.Create<int>("GluePlayer", "Glue玩家", "玩家无限喷胶", Parameter.Create("Slot", "槽位, 1-4"), ToggleGluePlayer));
            DevConsole.AddCommand(Command.Create<int>("MinePlayer", "Mine玩家", "玩家无限布雷", Parameter.Create("Slot", "槽位, 1-4"), ToggleMinePlayer));
        }

        private static bool isCommandAdded;

        internal static void DoClear(eGameStateName pre, eGameStateName next)
        {
            if (next >= eGameStateName.ExpeditionSuccess && next <= eGameStateName.ExpeditionAbort || next == eGameStateName.AfterLevel)
            {
                Logs.LogMessage($"HelpPlayer: DoClear ({next})");
                Array.Fill(needHelp, false);
                Array.Fill(needGlue, false);
                Array.Fill(needMine, false);
            }
        }

        private static float helpInterval = 0.5f;

        private static float glueInterval = 0.25f;

        private static float mineInterval = 0.25f;

        private static bool[] needHelp = new bool[4];

        private static bool[] needGlue = new bool[4];

        private static bool[] needMine = new bool[4];

        private static pItemData explosiveMine = new()
        {
            itemID_gearCRC = 125U
        };
    }
}
#endif