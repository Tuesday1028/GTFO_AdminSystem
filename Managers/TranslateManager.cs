using GameData;
using Hikaria.AdminSystem.Utilities;
using System;
using System.Collections.Generic;
using TheArchive.Core;
using TheArchive.Interfaces;

namespace Hikaria.AdminSystem.Managers
{
    public class TranslateManager : InitSingletonBase<TranslateManager>, IInitAfterGameDataInitialized
    {
        public static string EnemyName(uint id)
        {
            if (!EnemyID2Name.TryGetValue(id, out string Name))
            {
                Name = $"{EnemyDataBlock.GetBlock(id).name} [{id}]";
            }
            return Name;
        }

        public static T GetRandom<T>(T[] arr)
        {
            int num = new Random().Next(arr.Length - 1);
            return arr[num];
        }

        public void Init()
        {
            EnemyID2Name.Clear();
            EnemyName2ID.Clear();
            JsonHelper.TryRead(EnemyIDNamePath, EnemyIDNameFile, out List<EnemyIDName> enemyIDNames);
            foreach (var item in enemyIDNames)
            {
                EnemyName2ID.Add(item.Name, item.IDs.ToArray());
                foreach (var id in item.IDs)
                {
                    EnemyID2Name.Add(id, item.Name);
                }
            }
        }

        private static Dictionary<uint, string> EnemyID2Name = new();

        private static Dictionary<string, uint[]> EnemyName2ID = new();

        public struct EnemyIDName
        {
            public List<uint> IDs { get; set; }

            public string Name { get; set; }
        }

        private static readonly string EnemyIDNamePath = string.Concat(BepInEx.Paths.ConfigPath, "\\Hikaria\\AdminSystem\\");

        private static readonly string EnemyIDNameFile = "EnemyIDNames.json";
    }
}
