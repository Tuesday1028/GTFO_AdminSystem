using BepInEx;
using BepInEx.Unity.IL2CPP;
using Hikaria.AdminSystem.Utilities;
using System.Collections.Generic;
using TheArchive;
using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;

namespace Hikaria.AdminSystem;

[BepInDependency("Hikaria.DevConsole", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(ArchiveMod.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class EntryPoint : BasePlugin, IArchiveModule
{
    public override void Load()
    {
        Instance = this;

        ArchiveMod.RegisterArchiveModule(typeof(EntryPoint));

        Logs.LogMessage("OK");
    }

    public override bool Unload()
    {
        ArchiveMod.UnpatchModule(Instance);

        return base.Unload();
    }

    public void Init()
    {
    }

    public void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
    }

    public void OnLateUpdate()
    {
    }

    public void OnExit()
    {
    }

    public static EntryPoint Instance { get; private set; }

    public bool ApplyHarmonyPatches => false;

    public bool UsesLegacyPatches => false;

    public ArchiveLegacyPatcher Patcher { get; set; }

    public string ModuleGroup => Groups.ModuleGroup;

    public Dictionary<Language, string> ModuleGroupLanguages => new()
    {
        { Language.Chinese, "管理系统" }, { Language.English, "Admin System" }
    };

    public static class Groups
    {
        static Groups()
        {
            Item.SetLanguage(Language.Chinese, "物品");
            Item.SetLanguage(Language.English, "Item");

            Weapon.SetLanguage(Language.Chinese, "武器");
            Weapon.SetLanguage(Language.English, "Weapon");

            Player.SetLanguage(Language.Chinese, "玩家");
            Player.SetLanguage(Language.English, "Player");

            InLevel.SetLanguage(Language.Chinese, "游戏内");
            InLevel.SetLanguage(Language.English, "InLevel");

            Misc.SetLanguage(Language.Chinese, "杂项");
            Misc.SetLanguage(Language.English, "Misc");

            Enemy.SetLanguage(Language.Chinese, "敌人");
            Enemy.SetLanguage(Language.English, "Enemy");

            Security.SetLanguage(Language.Chinese, "安全");
            Security.SetLanguage(Language.English, "Security");
        }

        public static FeatureGroup ModuleGroup => FeatureGroups.GetOrCreateModuleGroup("Admin System");

        public static FeatureGroup Item => ModuleGroup.GetOrCreateSubGroup("Item");

        public static FeatureGroup Weapon => ModuleGroup.GetOrCreateSubGroup("Weapon");

        public static FeatureGroup Player => ModuleGroup.GetOrCreateSubGroup("Player");

        public static FeatureGroup Enemy => ModuleGroup.GetOrCreateSubGroup("Enemy");

        public static FeatureGroup Misc => ModuleGroup.GetOrCreateSubGroup("Misc");

        public static FeatureGroup Security => ModuleGroup.GetOrCreateSubGroup("Security");

        public static FeatureGroup InLevel => ModuleGroup.GetOrCreateSubGroup("InLevel");
    }
}
