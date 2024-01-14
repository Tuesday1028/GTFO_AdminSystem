using Gear;
using Hikaria.AdminSystem.Features.Weapon;
using Hikaria.AdminSystem.Utilities;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Threading;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Misc
{
    [DisallowInGameToggle]
    [EnableFeatureByDefault]
    [DoNotSaveToConfig]
    public class Spinbot : Feature
    {
        public override string Name => "陀螺";

        public override string Description => "类似于CSGO的陀螺";

        public override FeatureGroup Group => EntryPoint.Groups.Misc;

        [FeatureConfig]
        public static SpinbotSetting Settings { get; private set; }

        public class SpinbotSetting
        {
            [FSDisplayName("陀螺模式")]
            public SpinMode Mode { get; set; } = SpinMode.Off;

            [FSDisplayName("大陀螺旋转速度")]
            public float SpinSpeed { get; set; } = 3.25f;

            public enum SpinMode
            {
                Off,
                BigSpin,
                SmallSpin
            }
        }

        public override void Init()
        {
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<SpinbotHandler>();
        }

        [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
        private class LocalPlayerAgent_Setup_Patch
        {
            private static void Postfix(LocalPlayerAgent __instance)
            {
                if (__instance.GetComponent<SpinbotHandler>() == null)
                {
                    __instance.gameObject.AddComponent<SpinbotHandler>();
                }
            }
        }

        [ArchivePatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
        private class BulletWeapon_Fire_Patch
        {
            static void Prefix(BulletWeapon __instance)
            {
                if (!IsWeaponOwner(__instance))
                {
                    return;
                }
                string text = "weaponfiring";
                if (!timers.TryGetValue(text, out Thread thread))
                {
                    CreateThread(text, true);
                }
                else if (thread.IsAlive)
                {
                    timersInstance[text].add();
                }
                else
                {
                    CreateThread(text, false);
                }
            }
        }

        [ArchivePatch(typeof(Shotgun), nameof(Shotgun.Fire))]
        private class ShotGun__Fire__Patch
        {
            private static void Prefix(Shotgun __instance)
            {
                if (!IsWeaponOwner(__instance))
                {
                    return;
                }
                string text = "weaponfiring";
                if (!timers.TryGetValue(text, out Thread thread))
                {
                    CreateThread(text, true);
                }
                else if (thread.IsAlive)
                {
                    timersInstance[text].add();
                }
                else
                {
                    CreateThread(text, false);
                }
            }
        }

        private static bool IsWeaponOwner(BulletWeapon bw)
        {
            if (bw == null || bw.Owner == null)
            {
                return false;
            }
            return bw.Owner.Owner.IsLocal;
        }

        private static Dictionary<string, Thread> timers = new();

        private static Dictionary<string, Timer> timersInstance = new();

        private static void CreateThread(string threadName, bool add)
        {
            Timer timer = new();
            Thread thread = new(delegate ()
            {
                timer.Start();
            });
            thread.Start();
            if (add)
            {
                timers.Add(threadName, thread);
                timersInstance.Add(threadName, timer);
                return;
            }
            timers[threadName] = thread;
            timersInstance[threadName] = timer;
        }

        private sealed class Timer
        {
            public void add()
            {
                _sec = 6;
            }

            public void Start()
            {
                SpinbotHandler.IsInWeaponFiring = true;
                while (_sec > 0)
                {
                    Thread.Sleep(50);
                    _sec--;
                }
                SpinbotHandler.IsInWeaponFiring = false;
            }

            private int _sec = 6;
        }

        [ArchivePatch(typeof(PlayerSync), nameof(PlayerSync.SendLocomotion))]
        private class PlayerSync_SendLocomotion_Patch
        {
            private static bool Prefix(PlayerSync __instance)
            {
                if (!__instance.Replicator.OwningPlayer.IsLocal)
                {
                    return true;
                }
                if (Settings.Mode != SpinbotSetting.SpinMode.Off && __instance.m_agent.Locomotion.m_currentStateEnum != PlayerLocomotion.PLOC_State.Downed)
                {
                    return false;
                }
                return true;
            }
        }

        private class SpinbotHandler : MonoBehaviour
        {
            private void Start()
            {
                m_Player = AdminUtils.LocalPlayerAgent;
            }

            private void Update()
            {
                if (Settings.Mode == SpinbotSetting.SpinMode.Off || m_Player == null || m_Player.Locomotion.m_currentStateEnum == PlayerLocomotion.PLOC_State.Downed)
                {
                    return;
                }
                switch (Settings.Mode)
                {
                    case SpinbotSetting.SpinMode.SmallSpin:
                        Vector3 vector = WeaponAutoAim.WeaponAutoAimHandler.Current.AimTargetPos - m_Player.FPSCamera.Position;
                        if (!IsInWeaponFiring)
                        {
                            vector.x *= -1f;
                            vector.z *= -1f;
                            vector.y = -100f;
                        }
                        SendLocomotion(m_Player.Locomotion.m_currentStateEnum, m_Player.Position, vector.normalized, m_Player.Locomotion.VelFwdLocal, m_Player.Locomotion.VelRightLocal);
                        break;
                    case SpinbotSetting.SpinMode.BigSpin:
                        if (!IsInWeaponFiring)
                        {
                            SendLocomotion(m_Player.Locomotion.m_currentStateEnum, m_Player.Position, m_lookdir.normalized, m_Player.Locomotion.VelFwdLocal, m_Player.Locomotion.VelRightLocal);
                        }
                        else
                        {
                            SendLocomotion(m_Player.Locomotion.m_currentStateEnum, m_Player.Position, WeaponAutoAim.WeaponAutoAimHandler.Current.AimTargetPos - m_Player.FPSCamera.Position, m_Player.Locomotion.VelFwdLocal, m_Player.Locomotion.VelRightLocal);
                        }
                        break;
                    default:
                        break;
                }
            }

            private void FixedUpdate()
            {
                if (Settings.Mode != SpinbotSetting.SpinMode.BigSpin)
                {
                    return;
                }
                m_degree += Settings.SpinSpeed;
                m_lookdir.x = (float)Math.Sin(m_degree);
                m_lookdir.z = (float)Math.Cos(m_degree);
                if (m_degree >= 360.0)
                {
                    m_degree = 0.0;
                }
            }

            private void SendLocomotion(PlayerLocomotion.PLOC_State state, Vector3 pos, Vector3 lookDir, float velFwd, float velRight)
            {
                if (m_Player.Locomotion.m_currentStateEnum == PlayerLocomotion.PLOC_State.Downed)
                {
                    return;
                }
                pPlayerLocomotion data = m_Player.Sync.m_locomotionToSend;
                if (Settings.Mode == SpinbotSetting.SpinMode.BigSpin && Vector3.Distance(pos, m_Player.Sync.m_locomotionLast.Pos) < 0.15f)
                {
                    state = PlayerLocomotion.PLOC_State.Stunned;
                }
                data.LookDir.Value = lookDir;
                data.Pos = pos;
                data.State = state;
                data.VelFwd.Set(velFwd, 10f);
                data.VelRight.Set(velRight, 10f);
                if (m_Player.Sync.m_fireCountSyncRef > 255)
                {
                    data.FireCount = byte.MaxValue;
                }
                else
                {
                    data.FireCount = (byte)m_Player.Sync.m_fireCountSyncRef;
                }
                m_Player.Sync.m_fireCountSyncRef = 0;
                m_Player.Sync.m_locomotionToSend = data;
                m_Player.Sync.m_locomotionPacket.Send(data, SNet_ChannelType.GameNonCritical);
                m_Player.Sync.m_locomotionLast = data;
                m_Player.Sync.m_lastLookDir = lookDir;
                m_Player.Sync.m_velFwdLast = velFwd;
                m_Player.Sync.m_velRightLast = velRight;
            }

            private double m_degree;

            private Vector3 m_lookdir = new(0f, -100f, 0f);

            public static bool IsInWeaponFiring { get; set; }

            private PlayerAgent m_Player;
        }
    }
}
