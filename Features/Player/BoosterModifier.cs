using BoosterImplants;
using GameData;
using Hikaria.AdminSystem.Interfaces;
using Hikaria.AdminSystem.Managers;
using SNetwork;
using System;
using System.Collections.Generic;
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
            [FSDisplayName("玩家设置")]
            public List<ModifyBoosterEntry> ModifyBoosterEntries { get => ModifyBoosterEntryLookup.Values.ToList(); set { } }
        }

        public class ModifyBoosterEntry
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

            public ModifyBoosterEntry(SNet_Player player)
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

        private static Dictionary<ulong, ModifyBoosterEntry> ModifyBoosterEntryLookup = new();
        private static Dictionary<BoosterCondition, BoosterImplantConditionDataBlock> BoosterConditionDataBlockLookup = new();

        public override void Init()
        {
            GameEventManager.RegisterSelfInGameEventManager(this);
        }

        public override void OnDatablocksReady()
        {
            BoosterConditionDataBlockLookup.Clear();
            BoosterConditionDataBlockLookup = BoosterImplantConditionDataBlock.GetAllBlocksForEditor().ToDictionary(p => p.Condition);
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
                pBoosterImplantsWithOwner data = GetModifiedBoosterImplantsWithOwner();

                SNet_ReplicatedPlayerData<pBoosterImplantsWithOwner>.s_singleton.m_syncPacket.Send(data, SNet_ChannelType.SessionOrderCritical);
                BoosterImplantManager.Current.OnSyncBoosterImplants(Owner, data);
            }

            public pBoosterImplantsWithOwner GetModifiedBoosterImplantsWithOwner()
            {
                var Data = Owner.Load<pBoosterImplantsWithOwner>();

                if (Data.BasicImplant != null)
                {
                    if (Data.BasicImplant.BoosterEffectDatas != null)
                    {
                        var effects = BasicImplant.GetBoosterEffectDataArray();
                        for (int i = 0; i < Data.BasicImplant.BoosterEffectDatas.Count; i++)
                        {
                            Data.BasicImplant.BoosterEffectDatas[i] = effects[i];
                        }
                        Data.BasicImplant.BoosterEffectCount = BasicImplant.BoosterEffectCount;
                    }
                    if (Data.BasicImplant.Conditions != null)
                    {
                        var Conditions = BasicImplant.GetConditionArray();
                        for (int i = 0; i < Data.BasicImplant.Conditions.Count; i++)
                        {
                            Data.BasicImplant.Conditions[i] = Conditions[i];
                        }
                        Data.BasicImplant.ConditionCount = BasicImplant.ConditionCount;
                    }
                    Data.BasicImplant.UseCount = BasicImplant.UseCount;
                }

                if (Data.AdvancedImplant != null)
                {
                    if (Data.AdvancedImplant.BoosterEffectDatas != null)
                    {
                        var effects = AdvancedImplant.GetBoosterEffectDataArray();
                        for (int i = 0; i < Data.AdvancedImplant.BoosterEffectDatas.Count; i++)
                        {
                            Data.AdvancedImplant.BoosterEffectDatas[i] = effects[i];
                        }
                        Data.AdvancedImplant.BoosterEffectCount = AdvancedImplant.BoosterEffectCount;
                    }
                    if (Data.AdvancedImplant.Conditions != null)
                    {
                        var Conditions = AdvancedImplant.GetConditionArray();
                        for (int i = 0; i < Data.AdvancedImplant.Conditions.Count; i++)
                        {
                            Data.AdvancedImplant.Conditions[i] = Conditions[i];
                        }
                        Data.AdvancedImplant.ConditionCount = AdvancedImplant.ConditionCount;
                    }
                    Data.AdvancedImplant.UseCount = AdvancedImplant.UseCount;
                }

                if (Data.SpecializedImplant != null)
                {
                    if (Data.SpecializedImplant.BoosterEffectDatas != null)
                    {
                        var effects = SpecializedImplant.GetBoosterEffectDataArray();
                        for (int i = 0; i < Data.SpecializedImplant.BoosterEffectDatas.Count; i++)
                        {
                            Data.SpecializedImplant.BoosterEffectDatas[i] = effects[i];
                        }
                        Data.SpecializedImplant.BoosterEffectCount = SpecializedImplant.BoosterEffectCount;
                    }
                    if (Data.SpecializedImplant.Conditions != null)
                    {
                        var Conditions = SpecializedImplant.GetConditionArray();
                        for (int i = 0; i < Data.SpecializedImplant.Conditions.Count; i++)
                        {
                            Data.SpecializedImplant.Conditions[i] = Conditions[i];
                        }
                        Data.SpecializedImplant.ConditionCount = SpecializedImplant.ConditionCount;
                    }
                    Data.SpecializedImplant.UseCount = SpecializedImplant.UseCount;
                }

                return Data;
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
                            customBoosterImplantData.BoosterEffectDatas.Add(new(count, effect.BoosterEffectID, effect.EffectValue));
                            count++;
                        }
                        for (; count <= pBoosterImplantData.IMPLANT_EFFECT_SYNC_COUNT; count++)
                        {
                            customBoosterImplantData.BoosterEffectDatas.Add(new(count, 0U, 1f));
                        }
                    }
                    else
                    {
                        for (int i = 1; i <= pBoosterImplantData.IMPLANT_EFFECT_SYNC_COUNT; i++)
                        {
                            customBoosterImplantData.BoosterEffectDatas.Add(new(i, 0U, 1f));
                        }
                    }

                    customBoosterImplantData.BoosterEffectCount = boosterImplantData.BoosterEffectCount;

                    customBoosterImplantData.Conditions.Clear();
                    if (boosterImplantData.Conditions != null)
                    {
                        int count = 1;
                        foreach (var condition in boosterImplantData.Conditions)
                        {
                            BoosterImplantConditionDataBlock block = GameDataBlockBase<BoosterImplantConditionDataBlock>.GetBlock(condition);
                            customBoosterImplantData.Conditions.Add(new(count, block == null ? BoosterCondition.None : block.Condition));
                            count++;
                        }
                        for (; count <= pBoosterImplantData.IMPLANT_CONDITION_SYNC_COUNT; count++)
                        {
                            customBoosterImplantData.Conditions.Add(new(count, BoosterCondition.None));
                        }
                    }
                    else
                    {
                        for (int i = 1; i <= pBoosterImplantData.IMPLANT_CONDITION_SYNC_COUNT; i++)
                        {
                            customBoosterImplantData.Conditions.Add(new(i, BoosterCondition.None));
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
            public uint BoosterImplantID { get; set; }

            [FSDisplayName("效果列表")]
            public List<CustomBoosterEffectData> BoosterEffectDatas { get; set; } = new();

            [FSDisplayName("效果数量")]
            public int BoosterEffectCount { get; set; }

            [FSDisplayName("条件列表")]
            public List<CustomBoosterCondition> Conditions { get; set; } = new();

            [FSDisplayName("条件数量")]
            public int ConditionCount { get; set; }

            [FSDisplayName("剩余次数")]
            public int UseCount { get; set; }

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
                    uint id = 0;
                    if (BoosterConditionDataBlockLookup.TryGetValue(condition.Condition, out var block))
                    {
                        id = block.persistentID;
                    }
                    result.Add(id);
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
            public int Index { get; set; }

            [FSDisplayName("条件")]
            public BoosterCondition Condition { get; set; }
        }

        public class CustomBoosterEffectData
        {
            public CustomBoosterEffectData(int index, uint effectID, float effectValue)
            {
                Index = index;
                BoosterEffectID = effectID;
                EffectValue = effectValue;
            }
            
            [FSSeparator]
            [FSReadOnly]
            [FSDisplayName("索引")]
            public int Index { get; set; }

            [FSDisplayName("类型")]
            public uint BoosterEffectID { get; set; }

            [FSDisplayName("数值")]
            public float EffectValue { get; set; }

            public pBoosterEffectData GetBoosterEffectData()
            {
                return new() { BoosterEffectID = BoosterEffectID, EffectValue = EffectValue };
            }
        }


        public void OnSessionMemberChanged(SNet_Player player, SessionMemberEvent playerEvent)
        {
            if (player.IsBot)
                return;
            switch (playerEvent)
            {
                case SessionMemberEvent.JoinSessionHub:
                    ModifyBoosterEntryLookup.TryAdd(player.Lookup, new(player));
                    break;
                case SessionMemberEvent.LeftSessionHub:
                    if (player.IsLocal)
                        ModifyBoosterEntryLookup.Clear();
                    else
                        ModifyBoosterEntryLookup.Remove(player.Lookup);
                    break;
            }
        }
    }
}
