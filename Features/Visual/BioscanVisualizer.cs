using ChainedPuzzles;
using Hikaria.AdminSystem.Utilities;
using Player;
using SNetwork;
using System.Collections;
using System.Collections.Generic;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TMPro;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Visual
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    public class BioscanVisualizer : Feature
    {
        public override string Name => "生物扫描点位可视化";

        public override FeatureGroup Group => EntryPoint.Groups.Visual;

        [FeatureConfig]
        public static BioScanVisualizerSettings Settings { get; set; }

        public class BioScanVisualizerSettings
        {
            [FSDisplayName("快速引导")]
            public bool FastSplineReveal { get; set; }
            [FSDisplayName("预知点位")]
            public bool ForeseeBioscanPosition { get; set; }
        }

        private const float CircleWidth = 2f;
        private const float LineWidth = 2f;

        [ArchivePatch(typeof(CP_Holopath_Spline._DoRevealSpline_d__38), nameof(CP_Holopath_Spline._DoRevealSpline_d__38.MoveNext))]
        public class CP_Holopath_Spline__DoRevealSpline_d__38__MoveNext__Patch
        {
            private static void Prefix(CP_Holopath_Spline._DoRevealSpline_d__38 __instance)
            {
                if (!SNet.IsMaster)
                    return;
                if (Settings.FastSplineReveal)
                    __instance._timeToReveal_5__2 = 0f;
            }
        }

        private static Dictionary<int, TextMeshPro> _textLookup = new();
        private static Camera _camera;
        private static Vector2 _textSize = new Vector2(0.2f, 0.2f);
        private static Quaternion _rotation = Quaternion.LookRotation(Vector3.up);
        private static Color _splineCol = Color.gray;

        [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
        private class LocalPlayerAgent__Setup__Patch
        {
            private static void Postfix(LocalPlayerAgent __instance)
            {
                _camera = __instance.FPSCamera.m_camera;
            }
        }

        [ArchivePatch(typeof(CP_Bioscan_Core), nameof(CP_Bioscan_Core.OnSyncStateChange))]
        private class CP_Bioscan_Core__OnSyncStateChange__Patch
        {
            private static Color _color = Color.magenta;
            private static Color _colorTScan = Color.gray;

            private static void Postfix(CP_Bioscan_Core __instance, eBioscanStatus status)
            {
                if (__instance.IsMovable && (status == eBioscanStatus.Waiting || status == eBioscanStatus.Scanning))
                    UnityMainThreadDispatcher.Enqueue(DrawTScan(__instance));
                if (status == eBioscanStatus.SplineReveal)
                    UnityMainThreadDispatcher.Enqueue(DrawBioscan(__instance));
            }

            private static IEnumerator DrawBioscan(CP_Bioscan_Core core)
            {
                var spline = core.m_spline?.Cast<CP_Holopath_Spline>().CurvySpline;
                var points = spline?.GetApproximation(Space.World);
                var count = points?.Count;
                var graphics = core.m_graphics.Cast<CP_Bioscan_Graphics>();
                var scanCol = graphics.m_currentCol;
                while (CurrentGameState == (int)eGameStateName.InLevel && core.State.status == eBioscanStatus.SplineReveal)
                {
                    if (Settings.ForeseeBioscanPosition)
                    {
                        if (spline != null)
                        {
                            for (int i = 0; i < count - 1; i++)
                                Fig.DrawLine(points[i], points[i + 1], _splineCol, MaterialHelper.DefaultOverlayFaded, LineWidth);
                        }
                        scanCol = graphics.m_currentCol;
                        if (core.m_hasAlarm)
                        {
                            if (graphics.m_colorsByMode.ContainsKey(eChainedPuzzleGraphicsColorMode.Alarm_Waiting))
                                scanCol = graphics.m_colorsByMode[eChainedPuzzleGraphicsColorMode.Alarm_Waiting];
                        }
                        else if (graphics.m_colorsByMode.ContainsKey(eChainedPuzzleGraphicsColorMode.Waiting))
                            scanCol = graphics.m_colorsByMode[eChainedPuzzleGraphicsColorMode.Waiting];
                        Fig.DrawCircle(graphics.transform.position, _rotation, graphics.m_radius, scanCol, MaterialHelper.DefaultOverlay, CircleWidth, 96);
                    }
                    yield return null;
                }
            }

            private static IEnumerator DrawTScan(CP_Bioscan_Core core)
            {
                int count = core.m_movingComp.ScanPositions.Count;
                var positions = core.m_movingComp.ScanPositions;
                while (CurrentGameState == (int)eGameStateName.InLevel && (core.State.status == eBioscanStatus.Waiting || core.State.status == eBioscanStatus.Scanning))
                {
                    if (Settings.ForeseeBioscanPosition)
                    {
                        for (int i = 0; i < count - 1; i++)
                        {
                            FigExt.HighlightPoint(_camera, positions[i], string.Empty, _textSize, _color, _color, _color, MaterialHelper.DefaultOverlay, 0.4f, 0.2f, 0f);
                            Fig.DrawLine(positions[i], positions[i + 1], _colorTScan, MaterialHelper.DefaultOverlayFaded, 1f);
                        }
                        FigExt.HighlightPoint(_camera, positions[count - 1], string.Empty, _textSize, _color, _color, _color, MaterialHelper.DefaultOverlay, 0.4f, 0.2f, 0f);
                    }
                    yield return null;
                }
            }
        }

        [ArchivePatch(typeof(CP_Cluster_Core), nameof(CP_Cluster_Core.OnSyncStateChange))]
        private class CP_Cluster_Core__OnSyncStateChange__Patch
        {
            private static void Postfix(CP_Cluster_Core __instance, eClusterStatus newStatus)
            {
                if (newStatus == eClusterStatus.SplineReveal)
                    UnityMainThreadDispatcher.Enqueue(DrawClusterScan(__instance));
            }

            private static IEnumerator DrawClusterScan(CP_Cluster_Core core)
            {
                var spline = core.m_spline.Cast<CP_Holopath_Spline>().CurvySpline;
                var points = spline.GetApproximation(Space.World);
                var count = points.Count;
                while (core.m_sync.GetCurrentState().status == eClusterStatus.SplineReveal)
                {
                    if (Settings.ForeseeBioscanPosition)
                    {
                        for (int i = 0; i < count - 1; i++)
                            Fig.DrawLine(points[i], points[i + 1], _splineCol, MaterialHelper.DefaultOverlayFaded, LineWidth);
                        foreach (var icore in core.m_childCores)
                        {
                            var ccore = icore.Cast<CP_Bioscan_Core>();
                            var graphics = ccore.m_graphics.Cast<CP_Bioscan_Graphics>();
                            var cspline = ccore.m_spline.Cast<CP_Holopath_Spline>().CurvySpline;
                            var cpoints = cspline.GetApproximation(Space.World);
                            var ccount = cpoints.Count;
                            for (int i = 0; i < ccount - 1; i++)
                                Fig.DrawLine(cpoints[i], cpoints[i + 1], _splineCol, MaterialHelper.DefaultOverlayFaded, LineWidth);
                            var scanCol = graphics.m_currentCol;
                            if (ccore.m_hasAlarm)
                            {
                                if (graphics.m_colorsByMode.ContainsKey(eChainedPuzzleGraphicsColorMode.Alarm_Waiting))
                                    scanCol = graphics.m_colorsByMode[eChainedPuzzleGraphicsColorMode.Alarm_Waiting];
                            }
                            else if (graphics.m_colorsByMode.ContainsKey(eChainedPuzzleGraphicsColorMode.Waiting))
                                scanCol = graphics.m_colorsByMode[eChainedPuzzleGraphicsColorMode.Waiting];
                            Fig.DrawCircle(graphics.transform.position, _rotation, graphics.m_radius, scanCol, MaterialHelper.DefaultOverlay, CircleWidth, 96);
                        }
                    }

                    yield return null;
                }
            }
        }
    }
}
