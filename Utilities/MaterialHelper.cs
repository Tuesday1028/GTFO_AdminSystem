using Hikaria.AdminSystem.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace Hikaria.AdminSystem.Utilities
{
    internal static class MaterialHelper
    {
        public const string SHADER_NAME = "UI/Fig";

        public const string SHADER_PROPERTY_OPACITY = "_Opacity"; // 0 .. 1
        public const string SHADER_PROPERTY_SOFTNESS = "_Softness"; // 0 .. 1
        public const string SHADER_PROPERTY_DOTCOUNT = "_DotCount"; // 0 .. 1000
        public const string SHADER_PROPERTY_DOTSNAP = "_DotSnap";
        public const string SHADER_PROPERTY_ZTEST = "_ZTest";
        public const string SHADER_PROPERTY_WORLD_SIZE = "_WorldSize";

        public static Shader SHADER_UI_FIG { get; private set; }

        public static int ID_SHADER_PROPERTY_OPACITY { get; private set; }
        public static int ID_SHADER_PROPERTY_SOFTNESS { get; private set; }
        public static int ID_SHADER_PROPERTY_DOTCOUNT { get; private set; }
        public static int ID_SHADER_PROPERTY_DOTSNAP { get; private set; }
        public static int ID_SHADER_PROPERTY_ZTEST { get; private set; }
        public static int ID_SHADER_PROPERTY_WORLD_SIZE { get; private set; }

        private static readonly HashSet<int> s_RegisteredMaterialIDs = new();

        internal static void SetupShaderAndCacheProperties()
        {
            if (ID_SHADER_PROPERTY_OPACITY != 0)
                return;

            SHADER_UI_FIG = Shader.Find(SHADER_NAME);

            SHADER_UI_FIG.DoNotDestroyAndSetHideFlags();

            ID_SHADER_PROPERTY_OPACITY = Shader.PropertyToID(SHADER_PROPERTY_OPACITY);
            ID_SHADER_PROPERTY_SOFTNESS = Shader.PropertyToID(SHADER_PROPERTY_SOFTNESS);
            ID_SHADER_PROPERTY_DOTCOUNT = Shader.PropertyToID(SHADER_PROPERTY_DOTCOUNT);
            ID_SHADER_PROPERTY_DOTSNAP = Shader.PropertyToID(SHADER_PROPERTY_DOTSNAP);
            ID_SHADER_PROPERTY_ZTEST = Shader.PropertyToID(SHADER_PROPERTY_ZTEST);
            ID_SHADER_PROPERTY_WORLD_SIZE = Shader.PropertyToID(SHADER_PROPERTY_WORLD_SIZE);
        }

        public static Material SetOpacity(Material figMat, float opacity)
        {
            figMat.SetFloat(ID_SHADER_PROPERTY_OPACITY, opacity);
            return figMat;
        }

        public static Material SetSoftness(Material figMat, float softness)
        {
            figMat.SetFloat(ID_SHADER_PROPERTY_SOFTNESS, softness);
            return figMat;
        }

        public static Material SetDotCount(Material figMat, float dotCount)
        {
            figMat.SetFloat(ID_SHADER_PROPERTY_DOTCOUNT, dotCount);
            return figMat;
        }

        public static Material SetDotSnap(Material figMat, float dotSnap)
        {
            figMat.SetFloat(ID_SHADER_PROPERTY_DOTSNAP, dotSnap);
            return figMat;
        }

        public static Material SetRenderMode(Material figMat, Render mode)
        {
            figMat.SetFloat(ID_SHADER_PROPERTY_ZTEST, (int)mode);
            return figMat;
        }

        public static Material SetWorldSize(Material figMat, float worldSize)
        {
            figMat.SetFloat(ID_SHADER_PROPERTY_WORLD_SIZE, worldSize);
            return figMat;
        }

        public static void RegisterFigMaterial(Material mat)
        {
            var id = mat.GetInstanceID();
            if (s_RegisteredMaterialIDs.Contains(id))
            {
                return;
            }

            s_RegisteredMaterialIDs.Add(id);
            Fig.RegisterMaterial(mat);
        }

        private static Material s_DefaultInWorld;
        public static Material DefaultInWorld
        {
            get
            {
                if (s_DefaultInWorld != null)
                    return s_DefaultInWorld;

                SetupShaderAndCacheProperties();

                s_DefaultInWorld = new Material(SHADER_UI_FIG);

                s_DefaultInWorld.DoNotDestroyAndSetHideFlags();

                SetOpacity(s_DefaultInWorld, 1f);
                SetSoftness(s_DefaultInWorld, 0);
                SetDotCount(s_DefaultInWorld, 100);
                SetDotSnap(s_DefaultInWorld, 0);
                SetRenderMode(s_DefaultInWorld, Render.InWorld);
                SetWorldSize(s_DefaultInWorld, 0);

                RegisterFigMaterial(s_DefaultInWorld);

                return s_DefaultInWorld;
            }
        }

        private static Material s_DefaultInWorldFaded;
        public static Material DefaultInWorldFaded
        {
            get
            {
                if (s_DefaultInWorldFaded != null)
                    return s_DefaultInWorldFaded;

                SetupShaderAndCacheProperties();

                s_DefaultInWorldFaded = new Material(s_DefaultInWorld);

                s_DefaultInWorldFaded.DoNotDestroyAndSetHideFlags();

                SetOpacity(s_DefaultInWorldFaded, 0.2f);

                RegisterFigMaterial(s_DefaultInWorldFaded);

                return s_DefaultInWorldFaded;
            }
        }

        private static Material s_DefaultOverlay;
        public static Material DefaultOverlay
        {
            get
            {
                if (s_DefaultOverlay != null)
                    return s_DefaultOverlay;

                SetupShaderAndCacheProperties();

                s_DefaultOverlay = new Material(DefaultInWorld);

                s_DefaultOverlay.DoNotDestroyAndSetHideFlags();

                SetRenderMode(s_DefaultOverlay, Render.Always);

                RegisterFigMaterial(s_DefaultOverlay);

                return s_DefaultOverlay;
            }
        }

        private static Material s_DefaultOverlayFaded;
        public static Material DefaultOverlayFaded
        {
            get
            {
                if (s_DefaultOverlayFaded != null)
                    return s_DefaultOverlayFaded;

                SetupShaderAndCacheProperties();

                s_DefaultOverlayFaded = new Material(DefaultOverlay);

                s_DefaultOverlayFaded.DoNotDestroyAndSetHideFlags();

                SetOpacity(s_DefaultOverlayFaded, 0.2f);

                RegisterFigMaterial(s_DefaultOverlayFaded);

                return s_DefaultOverlayFaded;
            }
        }

        private static Material s_DefaultBehindWorld;
        public static Material DefaultBehindWorld
        {
            get
            {
                if (s_DefaultBehindWorld != null)
                    return s_DefaultBehindWorld;

                SetupShaderAndCacheProperties();

                s_DefaultBehindWorld = new Material(DefaultInWorld);

                s_DefaultBehindWorld.DoNotDestroyAndSetHideFlags();

                SetRenderMode(s_DefaultBehindWorld, Render.BehindWorld);

                RegisterFigMaterial(s_DefaultBehindWorld);

                return s_DefaultBehindWorld;
            }
        }

        private static Material s_DefaultBehindWorldFaded;
        public static Material DefaultBehindWorldFaded
        {
            get
            {
                if (s_DefaultBehindWorldFaded != null)
                    return s_DefaultBehindWorldFaded;

                SetupShaderAndCacheProperties();

                s_DefaultBehindWorldFaded = new Material(DefaultBehindWorld);

                s_DefaultBehindWorldFaded.DoNotDestroyAndSetHideFlags();

                SetOpacity(s_DefaultBehindWorldFaded, 0.2f);

                RegisterFigMaterial(s_DefaultBehindWorldFaded);

                return s_DefaultBehindWorldFaded;
            }
        }

        // https://docs.unity3d.com/Manual/SL-ZTest.html
        public enum Render
        {
            OnTop = 0, // Disabled
            Never = 1, // Never
                       // Less = 2, // Less
                       // Equal = 3, // Equal
            InWorld = 4, // LEqual
                         // BehindWorld = 5, // Greater
                         // NotEqual = 6, // NotEqual
            BehindWorld = 7, // GEqual
            Always = 8, // Always
        }
    }
}
