using BepInEx;
using BepInEx.Unity.IL2CPP;
using Hikaria.AdminSystem.Utilities;
using System.Collections.Generic;
using TheArchive;
using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Localization;

[assembly: ModDefaultFeatureGroupName("Admin System")]

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

    public Dictionary<Language, string> ModuleGroupLanguages => new();

    public static class Groups
    {
        public static FeatureGroup ModuleGroup => FeatureGroups.GetOrCreateModuleGroup("Admin System");

        public static FeatureGroup Item => ModuleGroup.GetOrCreateSubGroup("Item");

        public static FeatureGroup Weapon => ModuleGroup.GetOrCreateSubGroup("Weapon");

        public static FeatureGroup Player => ModuleGroup.GetOrCreateSubGroup("Player");

        public static FeatureGroup Door => ModuleGroup.GetOrCreateSubGroup("Door");

        public static FeatureGroup Enemy => ModuleGroup.GetOrCreateSubGroup("Enemy");

        public static FeatureGroup Resource => ModuleGroup.GetOrCreateSubGroup("Resource");

        public static FeatureGroup Misc => ModuleGroup.GetOrCreateSubGroup("Misc");

        public static FeatureGroup Security => ModuleGroup.GetOrCreateSubGroup("Security");

        public static FeatureGroup Environment => ModuleGroup.GetOrCreateSubGroup("Environment");

        public static FeatureGroup InLevel => ModuleGroup.GetOrCreateSubGroup("InLevel");
    }
}
