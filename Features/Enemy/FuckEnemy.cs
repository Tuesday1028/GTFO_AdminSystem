#if false
using BepInEx.Unity.IL2CPP.Utils;
using Hikaria.AdminSystem.Utilities;
using Hikaria.DevConsoleLite;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Player;
using System;
using System.Collections;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Enemy
{
    [HideInModSettings]
    [DisallowInGameToggle]
    [EnableFeatureByDefault]
    [DoNotSaveToConfig]
    public class FuckEnemy : Feature
    {
        public override string Name => "干死敌人";

        public override string Group => EntryPoint.Groups.Enemy;

        [FeatureConfig]
        public static FuckEnemySettings Settings { get; set; }

        public class FuckEnemySettings
        {
            [FSDisplayName("干死敌人")]
            public bool EnableEnemyFucker { get; set; }
        }

        public override void Init()
        {
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<FuckEnemyHandler>();
        }


        [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
        private class LocalPlayerAgent__Setup__Patch
        {
            private static void Postfix(LocalPlayerAgent __instance)
            {
                if (__instance.gameObject.GetComponent<FuckEnemyHandler>() == null)
                {
                    __instance.gameObject.AddComponent<FuckEnemyHandler>();
                }
            }
        }


        private class FuckEnemyHandler : MonoBehaviour
        {
            private void Awake()
            {
                Instance = this;
            }

            private void Start()
            {
                this.StartCoroutine(FuckUpdater());
            }

            private IEnumerator FuckUpdater()
            {
                var yielder = new WaitForSecondsRealtime(0.05f);
                while (true)
                {
                    if (Settings.EnableEnemyFucker)
                    {
                        this.StartCoroutine(UpdateFucker());
                    }
                    yield return yielder;
                }
            }

            private static IEnumerator UpdateFucker()
            {
                for (int i = 0; i < 4; i++)
                {
                    if (enableKillEnemyClose[i])
                    {
                        if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(i + 1, out PlayerAgent playerAgent))
                        {
                            enableKillEnemyClose[i] = false;
                            enableKillEnemyInRay[i] = false;
                            continue;
                        }
                        foreach (Collider collider in Physics.OverlapSphere(playerAgent.Position, killRadius, LayerManager.MASK_ENEMY_DAMAGABLE))
                        {
                            Dam_EnemyDamageBase dmgBase = collider.GetComponentInParent<Dam_EnemyDamageBase>();
                            if (dmgBase != null)
                            {
                                dmgBase.ExplosionDamage(100000000f, dmgBase.DamageTargetPos, playerAgent.TargetLookDir * 50f, 0);
                            }
                        }
                    }
                    if (enableKillEnemyInRay[i])
                    {
                        if (!AdminUtils.TryGetPlayerAgentFromSlotIndex(i + 1, out PlayerAgent playerAgent))
                        {
                            enableKillEnemyClose[i] = false;
                            enableKillEnemyInRay[i] = false;
                            continue;
                        }
                        Il2CppStructArray<RaycastHit> il2CppStructArray = Physics.RaycastAll(playerAgent.GetHeadCamTransform().position, playerAgent.TargetLookDir, killLength, LayerManager.MASK_ENEMY_DAMAGABLE);
                        if (il2CppStructArray != null)
                        {
                            foreach (RaycastHit raycastHit in il2CppStructArray)
                            {
                                Dam_EnemyDamageBase dmgBase = raycastHit.collider.GetComponentInParent<Dam_EnemyDamageBase>();
                                if (dmgBase != null)
                                {
                                    dmgBase.ExplosionDamage(100000000f, dmgBase.DamageTargetPos, playerAgent.TargetLookDir * 50f, 0);
                                }
                            }
                        }
                    }
                }
                yield break;
            }

            public static void SetKillEnemyRange(int range)
            {
                killLength = range;
                killRadius = range;
            }

            public static void ToggleKillEnemyClose(int slot)
            {
                if (AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent playerAgent))
                {
                    enableKillEnemyClose[slot - 1] = !enableKillEnemyClose[slot - 1];
                    DevConsole.Log($"<color={(enableKillEnemyClose[slot - 1] ? "green" : "red")}>{playerAgent.PlayerName}近身杀怪已{(enableKillEnemyClose[slot - 1] ? "开启" : "关闭")}</color>");
                    return;
                }
                DevConsole.Log($"<color=red>不存在slot为{slot}的玩家</color>");
            }

            public static void ToggleKillEnemyInRay(int slot)
            {
                if (AdminUtils.TryGetPlayerAgentFromSlotIndex(slot, out PlayerAgent playerAgent))
                {
                    enableKillEnemyInRay[slot - 1] = !enableKillEnemyInRay[slot - 1];
                    DevConsole.Log($"<color={(enableKillEnemyInRay[slot - 1] ? "green" : "red")}>{playerAgent.PlayerName}视线杀怪已{(enableKillEnemyInRay[slot - 1] ? "开启" : "关闭")}</color>");
                    return;
                }
                DevConsole.Log($"<color=red>不存在slot为{slot}的玩家</color>");
            }

            public static void DoClear(eGameStateName pre, eGameStateName next)
            {
                if (next == eGameStateName.AfterLevel)
                {
                    Array.Fill(enableKillEnemyClose, false);
                    Array.Fill(enableKillEnemyInRay, false);
                }
            }

            public static void AddCommands()
            {
                DevConsole.AddCommand(Command.Create<int>("KillEnemyClose", "近身杀怪", "近身杀怪", Parameter.Create("Slot", "槽位, 1-4"), ToggleKillEnemyClose));
                DevConsole.AddCommand(Command.Create<int>("KillEnemyInRay", "视线杀怪", "视线杀怪", Parameter.Create("Slot", "槽位, 1-4"), ToggleKillEnemyInRay));
                DevConsole.AddCommand(Command.Create<int>("SetKillEnemyRange", "杀怪距离", "杀怪距离", Parameter.Create("Range", "默认30"), SetKillEnemyRange));
            }

            private static bool[] enableKillEnemyClose = new bool[4];

            private static bool[] enableKillEnemyInRay = new bool[4];

            private static float killLength = 30f;

            private static float killRadius = 30f;

            public static FuckEnemyHandler Instance { get; private set; }
        }
    }
}
#endif