using AIGraph;
using CullingSystem;
using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Player
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    public class Noclip : Feature
    {
        public override string Name => "穿墙";

        public override string Description => "启用后可飞天遁地";

        public override FeatureGroup Group => EntryPoint.Groups.Player;

        public override bool InlineSettingsIntoParentMenu => true;

        [FeatureConfig]
        public static NoClipSettings Settings { get; set; }

        public class NoClipSettings
        {
            [FSDisplayName("启用穿墙")]
            public bool EnableNoClip
            {
                get
                {
                    return NoclipHandler.FreecamEnabled;
                }
                set
                {
                    if (CurrentGameState != (int)eGameStateName.InLevel)
                    {
                        return;
                    }
                    if (value)
                    {
                        NoclipHandler.SetEnable();
                    }
                    else
                    {
                        NoclipHandler.SetDisable();
                    }
                }
            }
        }

        public override void Init()
        {
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<NoclipHandler>();
            DevConsole.AddCommand(Command.Create<bool?>("NoClip", "穿墙", "穿墙", Parameter.Create("Enable", "True: 启用, False: 禁用"), enable =>
            {
                if (!enable.HasValue)
                {
                    enable = !Settings.EnableNoClip;
                }
                Settings.EnableNoClip = enable.Value;
                DevConsole.LogSuccess($"已{(enable.Value ? "启用" : "禁用")} 穿墙");
            }, () =>
            {
                DevConsole.LogVariable("穿墙", Settings.EnableNoClip);
            }));
        }

        public override void OnGameStateChanged(int state)
        {
            eGameStateName current = (eGameStateName)state;
            if (current == eGameStateName.InLevel)
            {
                if (Settings.EnableNoClip)
                {
                    NoclipHandler.SetEnable();
                }
                else
                {
                    NoclipHandler.SetDisable();
                }
            }
            if (current == eGameStateName.AfterLevel)
            {
                Settings.EnableNoClip = false;
                NoclipHandler.SetDisable();
            }
        }

        [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
        private class LocalPlayerAgent_Setup_Patch
        {
            private static void Postfix(LocalPlayerAgent __instance)
            {
                if (__instance.GetComponent<NoclipHandler>() == null)
                {
                    __instance.gameObject.AddComponent<NoclipHandler>();
                }
            }
        }

        private class NoclipHandler : MonoBehaviour
        {
            public static NoclipHandler Instance;

            public static Vector3 rot = Vector3.zero;
            public static float rotSpeed = 5;
            public static float moveSpeed = 8;

            public static bool FreecamEnabled { get; private set; }

            private PlayerLocomotion _Locomotion;
            private PlayerAgent _LocalPlayer;
            private FPSCamera _FPSCam;
            private FPSCameraHolder _FPSCamHolder;
            private AIG_CourseNode _LastNode;

            private static bool CanUpdate => GameStateManager.CurrentStateName == eGameStateName.InLevel && !DevConsole.IsOpen && GuiManager.Current.m_lastFocusState == eFocusState.FPS;

            private void Awake()
            {
                Instance = this;
                _LocalPlayer = AdminUtils.LocalPlayerAgent;
                _Locomotion = _LocalPlayer.Locomotion;
                _FPSCam = _LocalPlayer.FPSCamera;
                _FPSCamHolder = _FPSCam.m_holder;
            }

            private void Update()
            {
                if (!FreecamEnabled)
                    return;

                if (_LocalPlayer == null)
                    return;

                if (!CanUpdate)
                    return;

                UpdateMovement();
                var playerTransform = _LocalPlayer.transform;
                _Locomotion.m_owner.m_movingCuller.UpdatePosition(_LocalPlayer.DimensionIndex, playerTransform.position);
                _ = _Locomotion.m_owner.Sync.SendLocomotion(_Locomotion.m_currentStateEnum, playerTransform.position, _FPSCam.Forward, 0, 0);
            }

            private void LateUpdate()
            {
                if (!FreecamEnabled)
                    return;

                if (_LocalPlayer == null)
                    return;

                if (!CanUpdate)
                    return;

                var movement = _LocalPlayer.PlayerCharacterController;
                movement.m_smoothPosition = _LocalPlayer.transform.position;
            }

            private void FixedUpdate()
            {
                if (!FreecamEnabled)
                    return;

                if (_LocalPlayer == null)
                    return;

                if (!CanUpdate)
                    return;

                if (Physics.Raycast(_LocalPlayer.transform.position, Vector3.down, out var hit, float.MaxValue, LayerManager.MASK_NODE_GENERATION))
                {
                    if (AIG_CourseNode.TryGetCourseNode(_LocalPlayer.gameObject.GetDimension().DimensionIndex, hit.point, 1.0f, out var node))
                    {
                        if (_LastNode == null || _LastNode.NodeID != node.NodeID)
                        {
                            _LocalPlayer.SetCourseNode(node);
                            _LocalPlayer.m_movingCuller.SetCurrentNode(node.m_cullNode);
                            foreach (var light in node.m_lightsInNode)
                            {
                                var clight = light.GetC_Light();
                                if (clight != null)
                                {
                                    var isOn = clight.m_clusterLight.m_isOn;
                                    light.SetEnabled(isOn);
                                }
                            }
                            _LastNode = node;
                        }
                    }
                }
            }

            private void OnDisable()
            {
                if (FreecamEnabled)
                {
                    _FPSCamHolder.m_flatTrans.gameObject.SetActive(true);
                    _Locomotion.enabled = true;
                    C_CullingManager.CullingEnabled = true;
                    C_CullingManager.HideAll();
                    FreecamEnabled = false;
                }
            }

            public static void SetEnable()
            {
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                if (!FreecamEnabled)
                {
                    Instance._Locomotion.enabled = false;
                    C_CullingManager.CullingEnabled = false;
                    C_CullingManager.ShowAll();
                    FreecamEnabled = !FreecamEnabled;
                }
            }

            public static void SetDisable()
            {
                if (CurrentGameState != (int)eGameStateName.InLevel)
                {
                    return;
                }
                if (FreecamEnabled)
                {
                    Instance._FPSCamHolder.m_flatTrans.gameObject.SetActive(true);
                    Instance._Locomotion.enabled = true;
                    C_CullingManager.CullingEnabled = true;
                    C_CullingManager.HideAll();
                    FreecamEnabled = !FreecamEnabled;
                }
            }

            private void UpdateMovement()
            {
                if (Input.mouseScrollDelta.y > 0)
                {
                    moveSpeed = Mathf.Min(24.0f, moveSpeed + 1.0f);
                }
                else if (Input.mouseScrollDelta.y < 0)
                {
                    moveSpeed = Mathf.Max(1.0f, moveSpeed - 1.0f);
                }

                Vector3 movement = Vector3.zero;
                if (Input.GetKey(KeyCode.W))
                {
                    movement += _FPSCam.transform.forward;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    movement += -_FPSCam.transform.forward;
                }

                if (Input.GetKey(KeyCode.A))
                {
                    movement += -_FPSCam.transform.right;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    movement += _FPSCam.transform.right;
                }

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    movement *= 2.0f;
                }

                _LocalPlayer.transform.Translate(moveSpeed * Time.deltaTime * movement, Space.World);
            }
        }
    }
}