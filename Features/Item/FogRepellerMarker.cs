using BepInEx.Unity.IL2CPP.Utils.Collections;
using Hikaria.Core;
using Hikaria.Core.Interfaces;
using SNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;
using TheArchive.Utilities;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Item
{
    public class FogRepellerMarker : Feature, IOnRecallComplete, IOnSessionMemberChanged
    {
        public override string Name => "驱雾器标记";

        public override FeatureGroup Group => EntryPoint.Groups.Item;

        public override bool SkipInitialOnEnable => true;

        public override Type[] LocalizationExternalTypes => new Type[]
        {
            typeof(eFogRepellerSphereState)
        };

        public static new ILocalizationService Localization { get; set; }

        public static bool IsEnabled { get; set; }

        [FeatureConfig]
        public static FogRepellerMarkerSettings Settings { get; set; }

        public class FogRepellerMarkerSettings
        {
            [FSDisplayName("显示名称")]
            public bool ShowName { get; set; } = true;
            [FSDisplayName("显示状态")]
            public bool ShowState { get; set; } = true;
            [FSDisplayName("显示状态计时器")]
            public bool ShowStateTimer { get; set; } = true;
        }

        public override void Init()
        {
            GameEventAPI.RegisterSelf(this);
        }


        public override void OnEnable()
        {
            if (CurrentGameState != (int)eGameStateName.InLevel)
                return;
            _fogRepellerInstances = GameObject.FindObjectsOfType<FogRepellerInstance>().ToArray().Where(fri => fri.name != "Consumable_Fogrepeller_Instance").ToList();

            foreach (var fri in _fogRepellerInstances)
            {
                FogRepellerInstance__Start__Patch.Prefix(fri);
            }
        }

        public override void OnDisable()
        {
            if (CurrentGameState != (int)eGameStateName.InLevel)
                return;

            _fogRepellerInstances.Clear();
        }

        public void OnRecallComplete(eBufferType bufferType)
        {
            DoClear();
        }

        public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
        {
            if (player.IsLocal && playerEvent == SessionMemberEvent.LeftSessionHub)
            {
                DoClear();
            }
        }

        public static void DoClear()
        {
            foreach (var marker in _markerLookup.Values)
            {
                if (marker == null)
                    continue;
                GuiManager.NavMarkerLayer.RemoveMarker(marker);
            }
            _markerLookup.Clear();
        }

        [ArchivePatch(typeof(FogRepellerInstance), nameof(FogRepellerInstance.Start))]
        private class FogRepellerInstance__Start__Patch
        {
            public static void Prefix(FogRepellerInstance __instance)
            {
                var sphere = __instance.m_repellerSphere;

                if (!sphere)
                    return;

                if (!_fogRepellerInstances.SafeContains(__instance))
                {
                    _fogRepellerInstances.Add(__instance);

                    NavMarker marker = GuiManager.NavMarkerLayer.PlaceCustomMarker(NavMarkerOption.Title, __instance.gameObject, string.Empty, 0, false);
                    marker.SetVisible(true);
                    marker.SetPinEnabled(true);
                    marker.m_titleSubObj.transform.Cast<RectTransform>().anchoredPosition = Vector2.zero;
                    marker.m_title.fontStyle = TMPro.FontStyles.Normal;

                    _markerLookup[__instance.SyncID] = marker;

                    __instance.StartCoroutine(UpdateMarkerInfo(__instance, sphere, marker).WrapToIl2Cpp());
                }
            }


            private static IEnumerator UpdateMarkerInfo(FogRepellerInstance fri, FogRepeller_Sphere sphere, NavMarker marker)
            {
                var yielder = new WaitForSeconds(0.25f);
                float lifeTimer = Clock.Time + 3f;
                StringBuilder sb = new(100);

                float timer = 0f;
                while (IsEnabled && lifeTimer >= Clock.Time)
                {
                    switch (sphere.CurrentState)
                    {
                        case eFogRepellerSphereState.Disabled:
                            timer = 0f;
                            break;
                        case eFogRepellerSphereState.Initialize:
                        case eFogRepellerSphereState.Grow:
                        case eFogRepellerSphereState.Life:
                        case eFogRepellerSphereState.Shrink:
                            timer = Math.Max(0f, sphere.m_stateTimer - Clock.Time);
                            lifeTimer = Clock.Time + 3f;
                            break;
                    }

                    sb.Append($"<color=#FFFFFF><b>");

                    if (Settings.ShowName)
                    {
                        sb.AppendLine(Localization.Get(1));
                    }

                    if (Settings.ShowState)
                    {
                        sb.AppendLine($"{Localization.Get(2)}: {Localization.Get(sphere.CurrentState)}");
                    }

                    if (Settings.ShowStateTimer)
                    {
                        sb.AppendLine($"{Localization.Get(3)}: {timer:F0}s");
                    }

                    sb.Append("</b></color>");

                    marker.SetTitle(sb.ToString());

                    sb.Clear();

                    yield return yielder;
                }

                GuiManager.NavMarkerLayer.RemoveMarker(marker);

                _fogRepellerInstances.Remove(fri);
            }
        }



        private static List<FogRepellerInstance> _fogRepellerInstances = new List<FogRepellerInstance>();
        private static Dictionary<uint, NavMarker> _markerLookup = new();
    }
}
