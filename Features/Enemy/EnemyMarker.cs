using AIGraph;
using BepInEx.Unity.IL2CPP.Utils;
using Enemies;
using Hikaria.AdminSystem.Interfaces;
using Hikaria.AdminSystem.Managers;
using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using Player;
using SNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Enemy
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    internal class EnemyMarker : Feature, IOnSessionMemberChanged, IOnRecallComplete
    {
        public override string Name => "敌人标记";

        public override string Description => "实时显示敌人类别、位置、血量、状态和距离信息";

        public override FeatureGroup Group => EntryPoint.Groups.Enemy;

        [FeatureConfig]
        public static EnemyMarkerSettings Settings { get; set; }

        public class EnemyMarkerSettings
        {
            [FSDisplayName("敌人标记")]
            public bool EnableEnemyMarker
            {
                get
                {
                    return _enableEnemyMarker;
                }
                set
                {
                    _enableEnemyMarker = value;
                    if (!value)
                    {
                        EnemyMarkerHandler.DoClear();
                    }
                }
            }

            private bool _enableEnemyMarker;

            [FSDisplayName("标记最大区域间隔")]
            public int MaxDetectionNodeRange { get; set; } = 3;
        }

        public override void Init()
        {
            GameEventManager.RegisterSelfInGameEventManager(this);
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<EnemyMarkerHandler>();
            DevConsole.AddCommand(Command.Create<bool?>("EnemyMarker", "敌人标记", "实时显示敌人类别、位置、血量、状态和距离信息", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.EnableEnemyMarker;
                }

                Settings.EnableEnemyMarker = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 敌人标记");
            }, () =>
            {
                DevConsole.LogVariable("敌人标记", Settings.EnableEnemyMarker);
            }));
        }

        [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
        private class LocalPlayerAgent_Setup_Patch
        {
            private static void Postfix(LocalPlayerAgent __instance)
            {
                if (__instance.GetComponent<EnemyMarkerHandler>() == null)
                {
                    __instance.gameObject.AddComponent<EnemyMarkerHandler>();
                }
            }
        }

        public void OnRecallComplete(eBufferType bufferType)
        {
            EnemyMarkerHandler.DoClear();
        }

        public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
        {
            if (player.IsLocal && playerEvent == SessionMemberEvent.LeftSessionHub)
            {
                EnemyMarkerHandler.DoClear();
            }
        }

        private class EnemyMarkerHandler : MonoBehaviour
        {
            private void Awake()
            {
                player = AdminUtils.LocalPlayerAgent;
            }

            private void Start()
            {
                this.StartCoroutine(UpdateMarkers());
            }

            private IEnumerator UpdateMarkers()
            {
                var yielder = new WaitForSeconds(0.25f);
                while (true)
                {
                    if (GameStateManager.CurrentStateName == eGameStateName.InLevel)
                    {
                        while (true)
                        {
                            try
                            {
                                if (Settings.EnableEnemyMarker)
                                {
                                    if (player.CourseNode == null)
                                    {
                                        break;
                                    }
                                    foreach (var enemy in AIG_CourseGraph.GetReachableEnemiesInNodes(player.CourseNode, Settings.MaxDetectionNodeRange))
                                    {
                                        if (MarkerLookup.ContainsKey(enemy.GlobalID))
                                        {
                                            continue;
                                        }

                                        GameObject markerAlign = enemy.ModelRef.m_markerTagAlign;
                                        NavMarker marker = GuiManager.NavMarkerLayer.PlaceCustomMarker(NavMarkerOption.Title, markerAlign, string.Empty, 0, false);
                                        marker.SetVisible(true);
                                        marker.SetPinEnabled(true);
                                        marker.m_titleSubObj.transform.localScale *= 2.0f;
                                        MarkerLookup[enemy.GlobalID] = marker;

                                        Coroutine coroutine = this.StartCoroutine(UpdateMarker(enemy, marker));
                                        enemy.add_OnDeadCallback((Action)(() =>
                                        {
                                            MarkerLookup.Remove(enemy.GlobalID);
                                            GuiManager.NavMarkerLayer.RemoveMarker(marker);
                                        }));
                                    }
                                }
                                else
                                {
                                    if (MarkerLookup.Count <= 0)
                                        break;

                                    DoClear();
                                }
                            }
                            catch
                            {
                            }
                            break;
                        }
                    }
                    yield return yielder;
                }
            }

            private IEnumerator UpdateMarker(EnemyAgent agent, NavMarker marker)
            {
                var yielder = new WaitForSeconds(0.1f);
                while (agent.Alive)
                {
                    if (AIG_CourseGraph.GetDistanceBetweenToNodes(player.CourseNode, agent.CourseNode) > Settings.MaxDetectionNodeRange)
                    {
                        MarkerLookup.Remove(agent.GlobalID);
                        GuiManager.NavMarkerLayer.RemoveMarker(marker);
                        yield break;
                    }
                    var distance = (agent.Position - player.Position).magnitude;
                    Color color;
                    if (agent.Locomotion.CurrentStateEnum == ES_StateEnum.Hibernate)
                    {
                        if (agent.IsHibernationDetecting)
                        {
                            if (agent.Locomotion.Hibernate.m_heartbeatActive)
                            {
                                color = Color.yellow;
                            }
                            else
                            {
                                color = Color.yellow * 0.5f;
                            }
                        }
                        else
                        {
                            color = Color.white;
                        }
                    }
                    else if (agent.Locomotion.CurrentStateEnum == ES_StateEnum.ScoutScream)
                    {
                        color = Color.cyan * 2.0f;
                    }
                    else
                    {
                        color = Color.red;
                    }
                    string name = TranslateManager.EnemyName(agent.EnemyDataID);
                    name = name == "未知" ? $"ID: {agent.EnemyDataID}" : name;
                    marker.SetTitle($"<color=#FFFFFF><b>敌人: {name}\nHP: {agent.Damage.Health:F2}\n状态: <sprite=0 color=#{ColorExt.ToHex(color)}>\n距离: {distance:F2}</b></color>");
                    yield return yielder;
                }
            }

            public static void DoClear()
            {
                foreach (var marker in MarkerLookup.Values)
                {
                    GuiManager.NavMarkerLayer.RemoveMarker(marker);
                }
                MarkerLookup.Clear();
            }

            private PlayerAgent player;

            private readonly static Dictionary<ushort, NavMarker> MarkerLookup = new();
        }
    }
}
