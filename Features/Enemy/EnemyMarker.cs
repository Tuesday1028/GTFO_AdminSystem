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
using System.Text;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
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
            public bool EnableEnemyMarker { get => _enableEnemyMarker; set => _enableEnemyMarker = value; }

            [FSDisplayName("标记最大区域间隔")]
            public int MaxDetectionNodeRange { get => _maxDetectionNodeRange; set => _maxDetectionNodeRange = value; }

            [FSDisplayName("敌人信息")]
            public List<EnemyMarkerInfo> ShowEnemyInfo { get; set; } = new List<EnemyMarkerInfo>();

            [FSDisplayName("瞄准时透明")]
            public bool TransparentWhenAiming { get; set; }
        }

        [Localized]
        public enum EnemyMarkerInfo
        {
            Name,
            Health,
            State,
            Distance,
            Target
        }

        [Command("EnemyMarker")]
        private static bool _enableEnemyMarker
        {
            get
            {
                return _enemyMarker;
            }
            set
            {
                _enemyMarker = value;
                if (!value)
                {
                    EnemyMarkerHandler.DoClear();
                }
            }
        }

        private static bool _enemyMarker;

        private static int _maxDetectionNodeRange = 3;

        public override void Init()
        {
            GameEventAPI.RegisterListener(this);
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
                localPlayer = GetComponent<LocalPlayerAgent>();
            }

            private void Start()
            {
                StartCoroutine(UpdateMarkers().WrapToIl2Cpp());
            }

            private IEnumerator UpdateMarkers()
            {
                var yielder = new WaitForSecondsRealtime(0.25f);
                while (true)
                {
                    if (GameStateManager.CurrentStateName == eGameStateName.InLevel)
                    {
                        while (true)
                        {
                            try
                            {
                                if (_enemyMarker)
                                {
                                    if (localPlayer.CourseNode == null)
                                    {
                                        break;
                                    }
                                    foreach (var enemy in AIG_CourseGraph.GetReachableEnemiesInNodes(localPlayer.CourseNode, _maxDetectionNodeRange))
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
                StringBuilder sb = new(100);
                var yielder = new WaitForSeconds(0.1f);
                Color color;
                while (agent.Alive)
                {
                    if (AIG_CourseGraph.GetDistanceBetweenToNodes(localPlayer.CourseNode, agent.CourseNode) > _maxDetectionNodeRange)
                    {
                        MarkerLookup.Remove(agent.GlobalID);
                        GuiManager.NavMarkerLayer.RemoveMarker(marker);
                        yield break;
                    }
                    var distance = (agent.transform.position - localPlayer.Position).magnitude;

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
                    sb.Append("<color=#FFFFFF><b>");
                    if (Settings.ShowEnemyInfo.Contains(EnemyMarkerInfo.Name))
                        sb.AppendLine($"种类: {TranslateHelper.EnemyName(agent.EnemyDataID)}");
                    if (Settings.ShowEnemyInfo.Contains(EnemyMarkerInfo.Health))
                        sb.AppendLine($"血量: {agent.Damage.Health:F2}");
                    if (Settings.ShowEnemyInfo.Contains(EnemyMarkerInfo.State))
                        sb.AppendLine($"状态: <sprite=0 color=#{ColorExt.ToHex(color)}>");
                    if (Settings.ShowEnemyInfo.Contains(EnemyMarkerInfo.Distance))
                        sb.AppendLine($"距离: {distance:F2}");
                    if (Settings.ShowEnemyInfo.Contains(EnemyMarkerInfo.Target))
                        if (agent.HasValidTarget())
                            sb.AppendLine($"目标: {agent.AI.Target.m_agent.Cast<PlayerAgent>().GetColoredName()}");
                    sb.Append($"</b></color>");
                    marker.SetTitle(sb.ToString());
                    if (localPlayer.Inventory.WieldedSlot >= InventorySlot.GearStandard && localPlayer.Inventory.WieldedSlot <= InventorySlot.GearClass)
                    {
                        marker.SetAlpha(Settings.TransparentWhenAiming && localPlayer.Inventory.WieldedItem.AimButtonHeld ? 0.25f : 1f);
                    }
                    else
                    {
                        marker.SetAlpha(1f);
                    }
                    sb.Clear();
                    yield return yielder;
                }
                MarkerLookup.Remove(agent.GlobalID);
                GuiManager.NavMarkerLayer.RemoveMarker(marker);
            }

            public static void DoClear()
            {
                foreach (var marker in MarkerLookup.Values)
                {
                    GuiManager.NavMarkerLayer.RemoveMarker(marker);
                }
                MarkerLookup.Clear();
            }

            private PlayerAgent localPlayer;

            private readonly static Dictionary<ushort, NavMarker> MarkerLookup = new();
        }
    }
}
