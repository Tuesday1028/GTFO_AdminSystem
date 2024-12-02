using Clonesoft.Json;
using Hikaria.QC;
using Player;
using TheArchive.Core.Attributes;
using TheArchive.Core.Attributes.Feature.Settings;
using TheArchive.Core.FeaturesAPI;
using TheArchive.Core.Models;
using TheArchive.Utilities;

namespace Hikaria.AdminSystem.Features.Misc;

[DisallowInGameToggle]
[EnableFeatureByDefault]
[DoNotSaveToConfig]
public class FullBright : Feature
{
    public override string Name => "地图全亮";

    public override FeatureGroup Group => EntryPoint.Groups.Misc;

    [FeatureConfig]
    public static FullBrightSettings Settings { get; set; }

    public class FullBrightSettings
    {
        [FSDisplayName("启用")]
        [Command("FullBright", "视野全亮", MonoTargetType.Registry)]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                if (SuperLight != null)
                {
                    SuperLight.enabled = _enabled;
                }
            }
        }
        public bool _enabled = false;

        [FSDisplayName("强度")]
        public float Intensity
        {
            get
            {
                return _intensity;
            }
            set
            {
                _intensity = value;
                if (SuperLight != null)
                {
                    SuperLight.Intensity = _intensity;
                }
            }
        }
        private float _intensity = 0.2f;

        /*
        [FSDisplayName("Physical")]
        public float Physical
        {
            get
            {
                return _physical;
            }
            set
            {
                _physical = value;
                if (SuperLight != null)
                {
                    SuperLight.Physical = _physical;
                }
            }
        }
        private float _physical = 1f;
        */

        [FSDisplayName("范围")]
        public float Range
        {
            get
            {
                return _range;
            }
            set
            {
                _range = value;
                if (SuperLight != null)
                {
                    SuperLight.Range = _range;
                }
            }
        }
        private float _range = 200f;

        [FSDisplayName("颜色")]
        public SColor Color
        {
            get
            {
                return _color;
            }
            set
            {
                _color = value;
                if (SuperLight != null)
                {
                    SuperLight.Color = _color.ToUnityColor();
                }
            }
        }

        [JsonIgnore]
        private SColor _color = new(1f, 1f, 1f, 1f);
    }

    private static EffectLight SuperLight;

    public override void Init()
    {
        QuantumRegistry.RegisterObject(Settings);
    }

    [ArchivePatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))] 
    private class LocalPlayerAgent__Setup__Patch
    {
        private static void Postfix(LocalPlayerAgent __instance)
        {
            if (__instance.gameObject.GetComponent<EffectLight>() == null)
            {
                __instance.gameObject.AddComponent<EffectLight>();
            }
            SuperLight = __instance.gameObject.GetComponent<EffectLight>();
            Settings.Enabled = Settings.Enabled;
            Settings.Range = Settings.Range;
            Settings.Color = Settings.Color;
            //Settings.Physical = Settings.Physical;
            Settings.Intensity = Settings.Intensity;
        }
    }
}
