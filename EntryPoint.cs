using BepInEx;
using BepInEx.Unity.IL2CPP;
using Hikaria.AdminSystem.Utilities;
using TheArchive;
using TheArchive.Core;
using TheArchive.Core.Attributes;
using static TheArchive.Core.FeaturesAPI.FeatureGroups;

[assembly: ModDefaultFeatureGroupName("Admin System")]
namespace Hikaria.AdminSystem
{
    [BepInDependency("Hikaria.DevConsole", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(ArchiveMod.GUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class EntryPoint : BasePlugin, IArchiveModule
    {
        public override void Load()
        {
            Instance = this;

            ArchiveMod.RegisterModule(typeof(EntryPoint));

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

        public static class Groups
        {
            public static Group Item => GetOrCreate("Admin System | Item");

            public static Group Weapon => GetOrCreate("Admin System | Weapon");

            public static Group Player => GetOrCreate("Admin System | Player");

            public static Group Door => GetOrCreate("Admin System | Door");

            public static Group Enemy => GetOrCreate("Admin System | Enemy");

            public static Group Resource => GetOrCreate("Admin System | Resource");

            public static Group Misc => GetOrCreate("Admin System | Misc");

            public static Group Security => GetOrCreate("Admin System | Security");

            public static Group Environment => GetOrCreate("Admin System | Environment");

            public static Group InLevel => GetOrCreate("Admin System | InLevel");
        }
    }
}
