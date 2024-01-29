using Enemies;
using GameData;
using System.Collections.Generic;
using System.Linq;
using TheArchive.Core;
using TheArchive.Interfaces;

namespace Hikaria.AdminSystem.Managers
{
    public class EnemyDataManager : InitSingletonBase<TranslateManager>, IInitAfterGameDataInitialized
    {
        public static Dictionary<uint, EnemyDataBlock> EnemyDataBlockLookup { get; set; } = new();

        public static Dictionary<uint, EnemyDamageData> EnemyDamageDataLookup { get; set; } = new();

        public static float ArmorMultiThreshold { get; set; } = 0.1f;

        public void Init()
        {
            EnemyDataBlockLookup.Clear();
            foreach (EnemyDataBlock block in EnemyDataBlock.GetAllBlocksForEditor())
            {
                EnemyDataBlockLookup.Add(block.persistentID, block);
            }
        }

        public static EnemyDamageData GetEnemyDamageData(EnemyAgent enemy)
        {
            if (!EnemyDamageDataLookup.TryGetValue(enemy.EnemyDataID, out var data))
            {
                return GenerateAndStoreEnemyDamageData(enemy);
            }
            return data;
        }

        private static EnemyDamageData GenerateAndStoreEnemyDamageData(EnemyAgent enemy)
        {
            EnemyDamageData data = new();
            data.Id = enemy.EnemyDataID;
            foreach (var limb in enemy.Damage.DamageLimbs)
            {
                switch (limb.m_type)
                {
                    case eLimbDamageType.Armor:
                        data.Armorspots.Add(limb.m_limbID, limb.m_armorDamageMulti);
                        break;
                    case eLimbDamageType.Weakspot:
                        data.Weakspots.Add(limb.m_limbID, limb.m_weakspotDamageMulti);
                        break;
                    case eLimbDamageType.Normal:
                        data.Normalspots.Add(limb.m_limbID, 1f);
                        break;
                }
            }
            data.Armorspots = data.Armorspots.OrderByDescending(p => p.Value).ToDictionary(p=>p.Key, p=> p.Value);
            data.Weakspots = data.Weakspots.OrderByDescending(p => p.Value).ToDictionary(p=>p.Key, p=> p.Value);
            data.IsImmortal = data.Armorspots.Count == enemy.Damage.DamageLimbs.Count && !data.Armorspots.Any(p => p.Value > ArmorMultiThreshold);
            EnemyDamageDataLookup.Add(enemy.EnemyDataID, data);
            return data;
        }

        public static void ClearGeneratedEnemyDamageData()
        {
            EnemyDamageDataLookup.Clear();
        }

        public struct EnemyDamageData
        {
            public EnemyDamageData()
            {
                Id = 0;
                IsImmortal = false;
                Weakspots = new();
                Normalspots = new();
                Armorspots = new();
            }

            public uint Id { get; set; }

            public bool IsImmortal { get; set; }

            public Dictionary<int, float> Weakspots { get; set; }

            public Dictionary<int, float> Normalspots { get; set; }

            public Dictionary<int, float> Armorspots { get; set; }

            public bool HasWeakSpot => Weakspots.Count > 0;

            public bool HasNormalSpot => Normalspots.Count > 0;

            public bool HasArmorSpot => Armorspots.Count > 0;
        }
    }
}
