using BoosterImplants;
using GameData;
using Hikaria.AdminSystem.Interfaces;
using Hikaria.AdminSystem.Managers;
using SNetwork;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Linq;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.FeaturesAPI.Components;
using static Hikaria.AdminSystem.Interfaces.IOnSessionMemberChanged;

namespace Hikaria.AdminSystem.Features.Player
{
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    [DoNotSaveToConfig]
    internal class BoosterModifier : Feature, IOnSessionMemberChanged
    {
        public override string Name => "修改强化剂";

        public override string Group => EntryPoint.Groups.Player;

        [FeatureConfig]
        public static GiveBoosterSetting Settings { get; set; }

        public class GiveBoosterSetting
        {
            [FSInline]
            [FSDisplayName("玩家设置")]
            public List<GiveBoosterEntry> GiveBoosterEntries { get => GiveBoosterEntryLookup.Values.ToList(); set { } }
        }

        public class GiveBoosterEntry
        {
            [FSSeparator]
            [FSReadOnly]
            [FSDisplayName("玩家名称")]
            public string NickName { get => Owner.NickName; set { } }

            [FSDisplayName("加载强化剂")]
            public FButton LoadBoosters { get; set; }

            [FSDisplayName("自定义强化剂")]
            public CustomBoosterImplantsWithOwner CustomBoosterImplants { get; set; } = new();

            [FSDisplayName("修改强化剂")]
            public FButton ModifyBooster { get; set; }

            public GiveBoosterEntry(SNet_Player player)
            {
                Owner = player;
                CustomBoosterImplants.Owner = player;
                ModifyBooster = new("修改强化剂", "修改强化剂", new Action(delegate ()
                {
                    CustomBoosterImplants?.ModifyBooster();
                }));
                LoadBoosters = new("加载强化剂", "加载强化剂", new Action(delegate ()
                {
                    CustomBoosterImplants?.LoadFromPlayer();
                }));
            }
            
            private SNet_Player Owner;
        }

        private static Dictionary<ulong, GiveBoosterEntry> GiveBoosterEntryLookup = new();

        public override void Init()
        {
            GameEventManager.RegisterSelfInGameEventManager(this);
        }

        public class CustomBoosterImplantsWithOwner
        {
            [FSIgnore]
            public SNet_Player Owner { get; set; }

            [FSHeader("强化剂种类")]
            [FSDisplayName("低效")]
            public CustomBoosterImplantData BasicImplant { get; set; } = new();
            [FSDisplayName("高效")]
            public CustomBoosterImplantData AdvancedImplant { get; set; } = new();
            [FSDisplayName("特效")]
            public CustomBoosterImplantData SpecializedImplant { get; set; } = new();

            public bool SetImplantOnSlot(int slotIndex, CustomBoosterImplantData implantData)
            {
                switch (slotIndex)
                {
                    case 0:
                        BasicImplant = implantData;
                        return true;
                    case 1:
                        AdvancedImplant = implantData;
                        return true;
                    case 2:
                        SpecializedImplant = implantData;
                        return true;
                    default:
                        return false;
                }
            }

            public bool TryGetBoosterImplantDataByCategory(BoosterImplantCategory category, out CustomBoosterImplantData data)
            {
                switch (category)
                {
                    case BoosterImplantCategory.Muted:
                        data = BasicImplant;
                        return true;
                    case BoosterImplantCategory.Bold:
                        data = AdvancedImplant;
                        return true;
                    case BoosterImplantCategory.Aggressive:
                        data = SpecializedImplant;
                        return true;
                    default:
                        data = null;
                        return false;
                }
            }

            public bool TryGetBoosterImplantDataByIndex(int index, out CustomBoosterImplantData data)
            {
                switch (index)
                {
                    case 0:
                        data = BasicImplant;
                        return true;
                    case 1:
                        data = AdvancedImplant;
                        return true;
                    case 2:
                        data = SpecializedImplant;
                        return true;
                    default:
                        data = null;
                        return false;
                }
            }

            public void ModifyBooster()
            {
                pBoosterImplantsWithOwner data = GetBoosterImplantsWithOwner();

                SNet_ReplicatedPlayerData<pBoosterImplantsWithOwner>.s_singleton.m_syncPacket.Send(data, SNet_ChannelType.SessionOrderCritical);
                SNet_ReplicatedPlayerData<pBoosterImplantsWithOwner>.s_singleton.m_syncPacket.ReceiveAction.Invoke(data);

                if (Owner.IsLocal)
                {
                    BoosterImplantManager.Current.OnSyncBoosterImplants(SNet.LocalPlayer, data);
                }
            }

            public pBoosterImplantsWithOwner GetBoosterImplantsWithOwner()
            {
                pBoosterImplantsWithOwner data = new();

                data.PlayerData = new();
                data.PlayerData.SetPlayer(Owner);

                data.BasicImplant.BoosterImplantID = BasicImplant.BoosterImplantID;
                data.BasicImplant.BoosterEffectDatas = new(BasicImplant.GetBoosterEffectDataArray());
                data.BasicImplant.BoosterEffectCount = BasicImplant.BoosterEffectCount;
                data.BasicImplant.Conditions = new(BasicImplant.GetConditionArray());
                data.BasicImplant.ConditionCount = BasicImplant.ConditionCount;
                data.BasicImplant.UseCount = BasicImplant.UseCount;

                data.AdvancedImplant.BoosterImplantID = AdvancedImplant.BoosterImplantID;
                data.AdvancedImplant.BoosterEffectDatas = new(AdvancedImplant.GetBoosterEffectDataArray());
                data.AdvancedImplant.BoosterEffectCount = AdvancedImplant.BoosterEffectCount;
                data.AdvancedImplant.Conditions = new(AdvancedImplant.GetConditionArray());
                data.AdvancedImplant.ConditionCount = AdvancedImplant.ConditionCount;
                data.AdvancedImplant.UseCount = AdvancedImplant.UseCount;

                data.SpecializedImplant.BoosterImplantID = SpecializedImplant.BoosterImplantID;
                data.SpecializedImplant.BoosterEffectDatas = new(SpecializedImplant.GetBoosterEffectDataArray());
                data.SpecializedImplant.BoosterEffectCount = SpecializedImplant.BoosterEffectCount;
                data.SpecializedImplant.Conditions = new(SpecializedImplant.GetConditionArray());
                data.SpecializedImplant.ConditionCount = SpecializedImplant.ConditionCount;
                data.SpecializedImplant.UseCount = SpecializedImplant.UseCount;

                return data;
            }

            public void LoadFromPlayer()
            {
                pBoosterImplantsWithOwner originBoosterImplantData = Owner.Load<pBoosterImplantsWithOwner>();
                Dictionary<BoosterImplantCategory, pBoosterImplantData> dataDic = new()
                {
                    { BoosterImplantCategory.Muted, originBoosterImplantData.BasicImplant },
                    { BoosterImplantCategory.Bold, originBoosterImplantData.AdvancedImplant },
                    { BoosterImplantCategory.Aggressive, originBoosterImplantData.SpecializedImplant }

                };
                foreach (var pair in dataDic)
                {
                    var boosterImplantData = pair.Value;
                    if (!TryGetBoosterImplantDataByCategory(pair.Key, out var customBoosterImplantData))
                    {
                        continue;
                    }

                    customBoosterImplantData.BoosterImplantID = boosterImplantData.BoosterImplantID;

                    customBoosterImplantData.BoosterEffectDatas.Clear();
                    if (boosterImplantData.BoosterEffectDatas != null)
                    {
                        int count = 1;
                        foreach (var effect in boosterImplantData.BoosterEffectDatas)
                        {
                            customBoosterImplantData.BoosterEffectDatas.Add(new(count, (AgentModifier)effect.BoosterEffectID, effect.EffectValue));
                            count++;
                        }
                        for (; count <= pBoosterImplantData.IMPLANT_EFFECT_SYNC_COUNT; count++)
                        {
                            customBoosterImplantData.BoosterEffectDatas.Add(new(count, AgentModifier.None, 1f));
                        }
                    }

                    customBoosterImplantData.BoosterEffectCount = boosterImplantData.BoosterEffectCount;

                    customBoosterImplantData.Conditions.Clear();
                    if (boosterImplantData.Conditions != null)
                    {
                        int count = 1;
                        foreach (var condition in boosterImplantData.Conditions)
                        {
                            customBoosterImplantData.Conditions.Add(new(count, (BoosterCondition)condition));
                            count++;
                        }
                        for (; count <= pBoosterImplantData.IMPLANT_CONDITION_SYNC_COUNT; count++)
                        {
                            customBoosterImplantData.Conditions.Add(new(count, BoosterCondition.None));
                        }
                    }

                    customBoosterImplantData.ConditionCount = boosterImplantData.ConditionCount;

                    customBoosterImplantData.UseCount = boosterImplantData.UseCount;
                }
            }
        }

        public class CustomBoosterImplantData
        {
            [FSReadOnly]
            [FSHeader("强化剂详情")]
            [FSDisplayName("强化剂ID")]
            public uint BoosterImplantID { get; set; } = 0;

            [FSDisplayName("效果列表")]
            public List<CustomBoosterEffectData> BoosterEffectDatas { get; set; } = new()
            {
                new(1, 0, 1f), new(2, 0, 1f), new(3, 0, 1f), new(4, 0, 1f), new(5, 0, 1f),
                new(6, 0, 1f), new(7, 0, 1f), new(8, 0, 1f), new(9, 0, 1f), new(10, 0, 1f)
            };

            [FSDisplayName("效果数量")]
            public int BoosterEffectCount { get; set; } = 0;

            [FSDisplayName("条件列表")]
            public List<CustomBoosterCondition> Conditions { get; set; } = new()
            {
                new(1, 0), new(2, 0), new(3, 0), new(4, 0), new(5, 0)
            };

            [FSDisplayName("条件数量")]
            public int ConditionCount { get; set; } = 0;

            [FSDisplayName("剩余次数")]
            public int UseCount { get; set; } = 0;

            public pBoosterEffectData[] GetBoosterEffectDataArray()
            {
                List<pBoosterEffectData> result = new();
                foreach (var effect in BoosterEffectDatas)
                {
                    result.Add(effect.GetBoosterEffectData());
                }
                return result.ToArray();
            }

            public uint[] GetConditionArray()
            {
                List<uint> result = new();
                foreach (var condition in Conditions)
                {
                    result.Add((uint)condition.Condition);
                }
                return result.ToArray();
            }
        }

        public class CustomBoosterCondition
        {
            public CustomBoosterCondition(int index, BoosterCondition condition)
            {
                Index = index;
                Condition = condition;
            }

            [FSSeparator]
            [FSReadOnly]
            [FSDisplayName("索引")]
            public int Index { get; set; } = 0;

            [FSDisplayName("条件")]
            public BoosterCondition Condition { get; set; } = BoosterCondition.None;
        }

        public class CustomBoosterEffectData
        {
            public CustomBoosterEffectData(int index, AgentModifier effectID, float effectValue)
            {
                Index = index;
                BoosterEffectID = effectID;
                EffectValue = effectValue;
            }
            
            [FSSeparator]
            [FSReadOnly]
            [FSDisplayName("索引")]
            public int Index { get; set; } = 0;

            [FSDisplayName("类型")]
            public AgentModifier BoosterEffectID { get; set; } = AgentModifier.None;

            [FSDisplayName("数值")]
            public float EffectValue { get; set; } = 0;

            public pBoosterEffectData GetBoosterEffectData()
            {
                return new() { BoosterEffectID = (uint)BoosterEffectID, EffectValue = EffectValue };
            }
        }


        public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
        {
            switch (playerEvent)
            {
                case SessionMemberEvent.JoinSessionHub:
                    GiveBoosterEntryLookup.TryAdd(player.Lookup, new(player));
                    break;
                case SessionMemberEvent.LeftSessionHub:
                    if (player.IsLocal)
                        GiveBoosterEntryLookup.Clear();
                    else
                        GiveBoosterEntryLookup.Remove(player.Lookup);
                    break;
            }
        }
    }
}
