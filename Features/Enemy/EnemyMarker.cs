using AIGraph;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Enemies;
using Hikaria.AdminSystem.Managers;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using Hikaria.QC;
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
    [CommandPrefix("Enemy")]
    internal class EnemyMarker : Feature, IOnSessionMemberChanged, IOnRecallComplete
    {
        public override string Name => "敌人标记";

        public override string Description => "实时显示敌人类别、位置、血量、状态和距离信息";

        public override bool InlineSettingsIntoParentMenu => true;

        public override FeatureGroup Group => EntryPoint.Groups.Enemy;

        [FeatureConfig]
        public static EnemyMarkerSettings Settings { get; set; }

        public class EnemyMarkerSettings
        {
            [FSDisplayName("敌人标记")]
            [Command("EnemyMarker", MonoTargetType.Registry)]
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

            [FSDisplayName("标记最大区域间隔")]
            [Command("MarkerRange", "敌人标记最大区域间隔")]
            public int MaxDetectionNodeRange { get; set; } = 3;
        }

        private static bool _enableEnemyMarker = true;


        public override void Init()
        {
            GameEventAPI.RegisterSelf(this);
            QuantumRegistry.RegisterObject(Settings);
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<EnemyMarkerHandler>();
        }

        [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
        private class LocalPlayerAgent__Setup__Patch
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
                player = GetComponent<LocalPlayerAgent>();
            }

            private void Start()
            {
                StartCoroutine(UpdateMarkers().WrapToIl2Cpp());
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
                                if (_enableEnemyMarker)
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
                    marker.SetTitle($"<color=#FFFFFF><b>敌人: {TranslateHelper.EnemyName(agent.EnemyDataID)}\nHP: {agent.Damage.Health:F2}\n状态: <sprite=0 color=#{ColorExt.ToHex(color)}>\n距离: {distance:F2}</b></color>");
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
