using GameData;
using System;
using System.Collections.Generic;
using TheArchive.Core.ModulesAPI;

namespace Hikaria.AdminSystem.Managers
{
    public class TranslateManager
    {
        public static string EnemyName(uint id)
        {
            if (!EnemyID2NameLookup.TryGetValue(id, out string Name))
            {
                Name = $"{EnemyDataBlock.GetBlock(id).name} [{id}]";
            }
            return Name;
        }

        private static Dictionary<uint, string> EnemyID2NameLookup = new();

        private static CustomSetting<List<EnemyIDNameData>> EnemyIDNames = new("EnemyIDNameLookup", new(), new Action<List<EnemyIDNameData>>((data) =>
        {
            EnemyID2NameLookup.Clear();
            foreach (var item in data)
            {
                foreach (var id in item.IDs)
                {
                    EnemyID2NameLookup.TryAdd(id, item.Name);
                }
            }
        }));

        public class EnemyIDNameData
        {
            public List<uint> IDs { get; set; }

            public string Name { get; set; }
        }

        private static Dictionary<string, uint[]> EnemyName2ID = new();

        public struct EnemyIDName
        {
            public List<uint> IDs { get; set; }

            public string Name { get; set; }
        }
    }
}
