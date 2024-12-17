using Hikaria.AdminSystem.Utility;
using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;

namespace Hikaria.AdminSystem;

[ArchiveDependency(ItemMarker.PluginInfo.GUID)]
[ArchiveDependency(QC.PluginInfo.GUID)]
[ArchiveDependency(Core.PluginInfo.GUID)]
[ArchiveModule(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class EntryPoint : IArchiveModule
{
    public void Init()
    {
        Logs.LogMessage("OK");
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

    public bool ApplyHarmonyPatches => false;

    public bool UsesLegacyPatches => false;

    public ArchiveLegacyPatcher Patcher { get; set; }

    public string ModuleGroup => Groups.ModuleGroup;

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

            Dev.SetLanguage(Language.Chinese, "开发者选项");
            Dev.SetLanguage(Language.English, "Develop");

            Visual.SetLanguage(Language.Chinese, "可视化");
            Visual.SetLanguage(Language.English, "Visual");
        }

        public static FeatureGroup ModuleGroup => FeatureGroups.GetOrCreateModuleGroup("Admin System", new()
        {
            { Language.Chinese, "管理系统" }, { Language.English, "Admin System" }
        }); 

        public static FeatureGroup Item => ModuleGroup.GetOrCreateSubGroup("Item");

        public static FeatureGroup Weapon => ModuleGroup.GetOrCreateSubGroup("Weapon");

        public static FeatureGroup Player => ModuleGroup.GetOrCreateSubGroup("Player");

        public static FeatureGroup Enemy => ModuleGroup.GetOrCreateSubGroup("Enemy");

        public static FeatureGroup Misc => ModuleGroup.GetOrCreateSubGroup("Misc");

        public static FeatureGroup Security => ModuleGroup.GetOrCreateSubGroup("Security");

        public static FeatureGroup InLevel => ModuleGroup.GetOrCreateSubGroup("InLevel");

        public static FeatureGroup Dev => ModuleGroup.GetOrCreateSubGroup("Develop", true);

        public static FeatureGroup Visual => ModuleGroup.GetOrCreateSubGroup("Visual");
    }
}
