using Agents;
using AIGraph;
using AirNavigation;
using Enemies;
using Hikaria.AdminSystem.Utilities;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using Hikaria.QC;
using Player;
using SNetwork;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using TheArchive.Loader;
using UnityEngine;
using UnityEngine.AI;

namespace Hikaria.AdminSystem.Features.Visual
{
    [DisallowInGameToggle]
    [EnableFeatureByDefault]
    [DoNotSaveToConfig]
    public class EnemyPathVisualizer : Feature, IOnMasterChanged
    {
        public override string Name => "敌人寻迹可视化";

        public override FeatureGroup Group => EntryPoint.Groups.Visual;

        [FeatureConfig]
        public static EnemyPathVisualizerSettings Settings { get; set; }

        public class EnemyPathVisualizerSettings
        {
            [FSDisplayName("敌人寻迹可视化")]
            public bool ShowEnemyPath { get => _enemyPathVisualizer; set => _enemyPathVisualizer = value; }

            [FSDisplayName("寻迹可视化最大区域间隔")]
            public int MaxVisualizerNodeRange { get => _maxVisualizeNodeRange; set => _maxVisualizeNodeRange = value; }

            [FSDisplayName("只显示目标为自身的敌人")]
            public bool OnlyShowTargetingSelf { get; set; } = true;

            [FSDisplayName("显示触手路径")]
            public bool ShowScoutPath { get; set; } = false;

            [FSDisplayName("可视化渲染模式")]
            public VisualizerModeType VisualizerMode { get => EnemyPathVisualizer.VisualizerMode; set => EnemyPathVisualizer.VisualizerMode = value; }
        }

        [Localized]
        public enum VisualizerModeType
        {
            World,
            Overlay
        }

        [Command("EnemyPathVisualizer")]
        private static bool ShowEnemyPath
        {
            get
            {
                return _enemyPathVisualizer;
            }
            set
            {
                _enemyPathVisualizer = value;
            }
        }
        private static bool _enemyPathVisualizer;

        private static int _maxVisualizeNodeRange = 3;

        [Command("EnemyPathVisualizerMode")]
        private static VisualizerModeType VisualizerMode
        {
            get
            {
                return _visualizerMode;
            }
            set
            {
                _visualizerMode = value;
                EnemyPathVisualizerHandler.SetMaterial(value);
            }
        }
        private static VisualizerModeType _visualizerMode = VisualizerModeType.World;
        public override void Init()
        {
            GameEventAPI.RegisterSelf(this);
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<EnemyPathVisualizerHandler>();
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<AroundEnemyUpdater>();
        }

        public void OnMasterChanged()
        {
            if (SNet.IsMaster)
            {
                foreach (var enemy in UnityEngine.Object.FindObjectsOfType<EnemyAgent>())
                {
                    if (enemy.GetComponent<EnemyPathVisualizerHandler>() == null)
                        enemy.gameObject.AddComponent<EnemyPathVisualizerHandler>();
                }
            }
        }

        [ArchivePatch(typeof(EnemyAgent), nameof(EnemyAgent.Setup))]
        private class EnemyAgent__Setup__Patch
        {
            private static void Postfix(EnemyAgent __instance)
            {
                if (!SNet.IsMaster)
                    return;
                if (__instance.GetComponent<EnemyPathVisualizerHandler>() == null)
                    __instance.gameObject.AddComponent<EnemyPathVisualizerHandler>();
            }
        }

        [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
        private class LocalPlayerAgent__Setup__Patch
        {
            private static void Postfix(LocalPlayerAgent __instance)
            {
                if (__instance.GetComponent<AroundEnemyUpdater>() == null)
                    __instance.gameObject.AddComponent<AroundEnemyUpdater>();
            }
        }

        public class EnemyPathVisualizerHandler : MonoBehaviour
        {
            private EnemyAgent _enemyAgent;
            private EnemyAI _enemyAI;
            private PlayerAgent _localPlayerAgent;
            private Camera _camera;
            private NavMeshAgent _navMeshAgent;
            private FlyingAirGraphAgent _flyingAirGraphAgent;

            private ES_PathMove _pathMove;
            private ES_PathMoveFlyer _pathMoveFlyer;
            private bool _isFlyer;
            private ushort _id;

            private Color _color = Color.red;
            private Color _colorScoutToPoint = Color.yellow;
            private Vector2 _textSize = new Vector2(0.2f, 0.2f);

            private static Material s_MatNormal = MaterialHelper.DefaultOverlay;
            private static Material s_MatFaded = MaterialHelper.DefaultOverlayFaded;

            internal static void SetMaterial(VisualizerModeType type)
            {
                if (type == VisualizerModeType.Overlay)
                {
                    s_MatNormal = MaterialHelper.DefaultOverlay;
                    s_MatFaded = MaterialHelper.DefaultOverlayFaded;
                }
                else if (type == VisualizerModeType.World)
                {
                    s_MatNormal = MaterialHelper.DefaultInWorld;
                    s_MatFaded = MaterialHelper.DefaultInWorldFaded;
                }
            }

            private void Awake()
            {
                _enemyAgent = GetComponent<EnemyAgent>();
                _id = _enemyAgent.GlobalID;
                _localPlayerAgent = PlayerManager.GetLocalPlayerAgent();
                _camera = _localPlayerAgent.FPSCamera.m_camera;
                _navMeshAgent = _enemyAgent.AI.m_navMeshAgent.TryCast<NavMeshAgentExtention.NavMeshAgentProxy>()?.m_agent;
                _flyingAirGraphAgent = _enemyAgent.AI.m_navMeshAgent.TryCast<FlyingAirGraphAgent>();
                _pathMove = _enemyAgent.Locomotion.PathMove.TryCast<ES_PathMove>();
                _pathMoveFlyer = _enemyAgent.Locomotion.PathMove.TryCast<ES_PathMoveFlyer>();
                _isFlyer = _enemyAgent.EnemyBehaviorData.IsFlyer;

            }

            private void Update()
            {
                if (!Settings.ShowEnemyPath || !SNet.IsMaster || !_enemyAgent.Alive || !_pathVisualizers.Contains(_id))
                    return;

                if (AIG_CourseGraph.GetDistanceBetweenToNodes(_localPlayerAgent.CourseNode, _enemyAgent.CourseNode) > _maxVisualizeNodeRange)
                    return;

                var characterID = -1;
                if (_enemyAgent.HasValidTarget())
                {
                    if (Settings.OnlyShowTargetingSelf && !_enemyAgent.AI.Target.m_agent.Cast<PlayerAgent>().IsLocallyOwned)
                        return;
                    characterID = _enemyAgent.AI.Target.m_agent.Cast<PlayerAgent>().CharacterID;
                }
                if (_isFlyer)
                {
                    Vector3 last = _enemyAgent.transform.position;
                    int nodeCount = _flyingAirGraphAgent.CurrentPath.nodes.Count;
                    for (int i = 0; i < nodeCount; i++)
                    {
                        var point = _flyingAirGraphAgent.CurrentPath.nodes[i];
                        Fig.DrawDottedLine(last, point, characterID >= 0 ? PlayerManager.GetStaticPlayerColor(characterID) : Color.white, s_MatNormal, 1f);
                        last = point;
                    }
                }
                else
                {
                    Vector3 last = _enemyAgent.transform.position;
                    int cornersCount = _navMeshAgent.path.corners.Count;
                    for (int i = 0; i < cornersCount; i++)
                    {
                        var point = _navMeshAgent.path.corners[i];
                        Fig.DrawDottedLine(last, point, characterID >= 0 ? PlayerManager.GetStaticPlayerColor(characterID) : Color.white, s_MatNormal, 1f);
                        last = point;
                    }
                }
                if (Settings.ShowScoutPath && _enemyAgent.AI.Mode == AgentMode.Scout)
                {
                    var scoutPath = _enemyAgent.AI.m_scoutPath;
                    if (scoutPath != null)
                    {
                        for (int i = 0; i < scoutPath.m_pathData.pathSteps; i++)
                        {
                            FigExt.HighlightPoint(_camera, scoutPath.GetPosition(i), string.Empty, _textSize, _color, _color, _color, s_MatNormal, 0.2f, 0.2f, 0f);
                            if (_enemyAgent.Locomotion.CurrentStateEnum != ES_StateEnum.ScoutDetection && i + 1 < scoutPath.m_pathData.pathSteps)
                                Fig.DrawDottedLine(scoutPath.GetPosition(i), scoutPath.GetPosition(i + 1), _colorScoutToPoint, s_MatFaded, 0.8f);
                        }
                    }
                }
            }

            private void OnDestroy()
            {
                _pathVisualizers.Remove(_id);
            }
        }

        private class AroundEnemyUpdater : MonoBehaviour
        {
            private PlayerAgent _localPlayer;
            private float _timer;

            private void Awake()
            {
                _localPlayer = GetComponent<LocalPlayerAgent>();
                _timer = Time.time;
            }

            private void Update()
            {
                if (CurrentGameState != (int)eGameStateName.InLevel)
                    return;
                if (!Settings.ShowEnemyPath || _timer > Clock.Time)
                    return;

                if (_enemyPathVisualizer)
                {
                    if (_localPlayer.CourseNode != null)
                    {
                        foreach (var enemy in AIG_CourseGraph.GetReachableEnemiesInNodes(_localPlayer.CourseNode, _maxVisualizeNodeRange))
                        {
                            _pathVisualizers.Add(enemy.GlobalID);
                        }
                    }
                }
                else if (_pathVisualizers.Count > 0)
                {
                    _pathVisualizers.Clear();
                }

                _timer = Clock.Time + 0.25f;
            }
        }

        private readonly static HashSet<ushort> _pathVisualizers = new();
    }
}
