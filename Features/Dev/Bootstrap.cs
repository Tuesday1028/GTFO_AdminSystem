using Hikaria.AdminSystem.Utilities;
using TheArchive.Core.Attributes;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.AdminSystem.Features.Develop
{
    [HideInModSettings]
    [EnableFeatureByDefault]
    [DisallowInGameToggle]
    internal class Bootstrap : Feature
    {
        public override string Name => "Bootstrap";

        public override FeatureGroup Group => EntryPoint.Groups.Dev;

        public override void OnGameDataInitialized()
        {
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<UnityMainThreadDispatcher>();
            GameObject obj = new("Hikaria.AdminSystem.ScriptsHolder");
            Object.DontDestroyOnLoad(obj);
            obj.AddComponent<UnityMainThreadDispatcher>();
        }
    }
}
