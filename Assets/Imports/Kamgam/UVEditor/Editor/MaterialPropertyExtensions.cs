using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Kamgam.UVEditor
{
    public static class MaterialPropertyExtensions
    {
        public static List<string> MainTextureNames = new List<string> { "_BaseMap", "_MainTex", "_AlbedoMap", "_AlbedoTex", "_Main", "_Albedo", "_BaseTexture", "_BaseTex", "_Base_Texture" };
        public static List<string> NormalMapNames = new List<string> { "_BumpMap", "_NormalMap", "_Bump", "_Normal", "_MainNormalMap", "_ParallaxMap" };
        public static List<string> SpecularMapNames = new List<string> { "_SpecGlossMap", "_SpecularColorMap", "_SpecularMap", "_Specular", "_MainSpecularMap" };
        public static List<string> MetallicMapNames = new List<string> { "_MetallicGlossMap", "_MetallicColorMap", "_MetallicMap", "_Metallic", "_MainMetallicMap" };
        public static List<string> EmissionMapNames = new List<string> { "_EmissionMap", "_EmissiveColorMap", "_Emission", "_EmissiveMap", "_Emissive", "_MainEmissiveMap" };
        public static List<string> OcclusionMapNames = new List<string> { "_OcclusionMap", "_OcclusionColorMap", "_Occlusion", "_MainOcclusionMap" };

        public static bool HasTextureProperty(this Material material, string propertyName)
        {
            if (material == null || material.shader == null)
                return false;

            int count = material.shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                string name = material.shader.GetPropertyName(i);
                if (name == propertyName)
                {
                    var type = material.shader.GetPropertyType(i);
                    if (type == UnityEngine.Rendering.ShaderPropertyType.Texture)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tries each property name in order and returns the result for the first property that is found.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="propertyNames"></param>
        /// <returns></returns>
        public static Texture GetTextureOrNull(this Material material, params string[] propertyNames)
        {
            return GetTextureOrNullByName(material, propertyNames);
        }

        /// <summary>
        /// Tries each property name in order and returns the result for the first property that is found.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="propertyNames"></param>
        /// <returns></returns>
        public static Texture GetTextureOrNullByName(this Material material, IList<string> propertyNames)
        {
            if (material == null || material.shader == null)
                return null;

            for (int i = 0; i < propertyNames.Count; i++)
            {
                if (material.HasTextureProperty(propertyNames[i]))
                {
                    return material.GetTexture(propertyNames[i]);
                }
            }

            return null;
        }

        /// <summary>
        /// Tries each property name in order and sets the first property that is found.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="texture"></param>
        /// <param name="propertyNames"></param>
        public static void SetTexture(this Material material, Texture texture, params string[] propertyNames)
        {
            SetTextureByName(material, texture, propertyNames);
        }

        /// <summary>
        /// Tries each property name in order and sets the first property that is found.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="texture"></param>
        /// <param name="propertyNames"></param>
        public static void SetTextureByName(this Material material, Texture texture, IList<string> propertyNames)
        {
            if (material == null || material.shader == null || texture == null)
                return;

            for (int i = 0; i < propertyNames.Count; i++)
            {
                if (material.HasTextureProperty(propertyNames[i]))
                {
                    material.SetTexture(propertyNames[i], texture);
                    return;
                }
            }
            
            // Last resort, set first texture with names similar to ..
            var names = material.GetTexturePropertyNames();
            for (int i = 0; i < names.Length; i++)
            {
                var lcName = names[i].ToLowerInvariant();
                if (lcName.Contains("albedo") || lcName.Contains("main") || lcName.Contains("base") || lcName.Contains("color"))
                {
                    material.SetTexture(names[i], texture);
                }
            }
        }

        public static Texture GetMainTexture(this Material material)
        {
            if (material == null || material.shader == null)
                return null;

            // Unity does not throw an error but it logs an annoying error message:
            // https://discussions.unity.com/t/please-make-setting-the-maintexture-flags-mandatory-for-shadergraph-shaders/1598813
            Texture result = material.getMainTextureWithoutErrorLogs();
            
            if (result == null)
            {
                string mainTexturePropertyName = material.GetMainTexturePropertyName();
                if (mainTexturePropertyName != null)
                    return material.GetTexture(mainTexturePropertyName);
            }

            return result;
        }

        public static string GetMainTexturePropertyName(this Material material)
        {
            if (material == null || material.shader == null)
                return null;

            for (int i = 0; i < MainTextureNames.Count; i++)
            {
                if (material.HasTextureProperty(MainTextureNames[i]))
                {
                    return MainTextureNames[i];
                }
            }

            var names = material.GetTexturePropertyNames();
            for (int i = 0; i < names.Length; i++)
            {
                var lcName = names[i].ToLowerInvariant();
                if (lcName.Contains("albedo") || lcName.Contains("main") || lcName.Contains("base") || lcName.Contains("color"))
                {
                    return names[i];
                }
            }

            return null;
        }

        public static void SetMainTexture(this Material material, Texture texture)
        {
            if (material == null || material.shader == null || texture == null)
                return;

            string mainTexturePropertyName = material.GetMainTexturePropertyName();
            
            // Try to guess the texture property name.
            if (mainTexturePropertyName != null)
                material.SetTexture(mainTexturePropertyName, texture);

            // Try the unity method of setting too.
            // Unity does not throw an error but it logs an annoying error message:
            // https://discussions.unity.com/t/please-make-setting-the-maintexture-flags-mandatory-for-shadergraph-shaders/1598813
            material.mainTexture = texture;
        }
        
        public static Vector2 GetMainTextureOffset(this Material material)
        {
            if (material == null || material.shader == null)
                return Vector2.zero;

            // Unity does not throw an error but it logs an annoying error message:
            // https://discussions.unity.com/t/please-make-setting-the-maintexture-flags-mandatory-for-shadergraph-shaders/1598813
            if (material.getMainTextureWithoutErrorLogs() != null)
            {
                return material.mainTextureOffset;
            }

            string mainTexturePropertyName = material.GetMainTexturePropertyName();
            return material.GetTextureOffset(mainTexturePropertyName);
        }
        
        public static Vector2 GetMainTextureScale(this Material material)
        {
            if (material == null || material.shader == null)
                return Vector2.one;

            // Unity does not throw an error but it logs an annoying error message:
            // https://discussions.unity.com/t/please-make-setting-the-maintexture-flags-mandatory-for-shadergraph-shaders/1598813
            if (material.getMainTextureWithoutErrorLogs() != null)
            {
                return material.mainTextureScale;
            }
            
            string mainTexturePropertyName = material.GetMainTexturePropertyName();
            return material.GetTextureScale(mainTexturePropertyName);
        }

        private static CustomLogHandler lh = new CustomLogHandler();

        static Texture getMainTextureWithoutErrorLogs(this Material material)
        {
            // Unity does not throw an error but it logs an annoying error message:
            // https://discussions.unity.com/t/please-make-setting-the-maintexture-flags-mandatory-for-shadergraph-shaders/1598813
            // Sadly
            
            if (material.HasTextureProperty("_MainTex"))
                return material.mainTexture;

            return null;
        }

        public static Texture GetNormalMap(this Material material)
        {
            // Add custom shader propery names at the end of this list (don't forget to add them to Set... too).
            return material.GetTextureOrNullByName(NormalMapNames);
        }

        public static void SetNormalMap(this Material material, Texture texture)
        {
            material.SetTextureByName(texture, NormalMapNames);
        }


        public static Texture GetSpecularMap(this Material material)
        {
            // Add custom shader propery names at the end of this list (don't forget to add them to Set... too).
            return material.GetTextureOrNullByName(SpecularMapNames);
        }

        public static void SetSpecularMap(this Material material, Texture texture)
        {
            material.SetTextureByName(texture, SpecularMapNames);
        }


        public static Texture GetMetallicMap(this Material material)
        {
            // Add custom shader propery names at the end of this list (don't forget to add them to Set... too).
            return material.GetTextureOrNullByName(MetallicMapNames);
        }

        public static void SetMetallicMap(this Material material, Texture texture)
        {
            material.SetTextureByName(texture, MetallicMapNames);
        }


        public static Texture GetEmissionMap(this Material material)
        {
            // Add custom shader propery names at the end of this list (don't forget to add them to Set... too).
            return material.GetTextureOrNullByName(EmissionMapNames);
        }

        public static void SetEmissionMap(this Material material, Texture texture)
        {
            material.SetTextureByName(texture, EmissionMapNames);
        }


        public static Texture GetOcclusionMap(this Material material)
        {
            // Add custom shader propery names at the end of this list (don't forget to add them to Set... too).
            return material.GetTextureOrNullByName(OcclusionMapNames);
        }

        public static void SetOcclusionMap(this Material material, Texture texture)
        {
            material.SetTextureByName(texture, OcclusionMapNames);
        }

        public static bool IsMainName(this Material material, string propertyName)
        {
            return IsAlbedoName(material, propertyName);
        }

        public static bool IsAlbedoName(this Material material, string propertyName)
        {
            return MainTextureNames.Contains(propertyName);
        }

        public static bool IsNormalMapName(this Material material, string propertyName)
        {
            return NormalMapNames.Contains(propertyName);
        }

        public static bool IsSpecularMapName(this Material material, string propertyName)
        {
            return SpecularMapNames.Contains(propertyName);
        }

        public static bool IsMetallicMapName(this Material material, string propertyName)
        {
            return MetallicMapNames.Contains(propertyName);
        }

        public static bool IsEmissionMapName(this Material material, string propertyName)
        {
            return EmissionMapNames.Contains(propertyName);
        }

        public static bool IsOcclusionMapName(this Material material, string propertyName)
        {
            return OcclusionMapNames.Contains(propertyName);
        }

        // public static bool IsAutoDetectedTextureName(this Material material, string propertyName)
        // {
        //     return IsAutoDetectedTextureName(propertyName);
        // }

        // public static bool IsAutoDetectedTextureName(string propertyName)
        // {
        //     foreach (var name in allAutoDetectedTextureNames())
        //     {
        //         if (propertyName == name)
        //             return true;
        //     }
        // 
        //     return false;
        // }

        // public static IEnumerable<string> AllAutoDetectedTextureNames => allAutoDetectedTextureNames();
        // 
        // static IEnumerable<string> allAutoDetectedTextureNames()
        // {
        //     foreach (var name in MainTextureNames)
        //         yield return name;
        // 
        //     foreach (var name in NormalMapNames)
        //         yield return name;
        // 
        //     foreach (var name in SpecularMapNames)
        //         yield return name;
        // 
        //     foreach (var name in MetallicMapNames)
        //         yield return name;
        // 
        //     foreach (var name in EmissionMapNames)
        //         yield return name;
        // 
        //     foreach (var name in OcclusionMapNames)
        //         yield return name;
        // }
    }
    
    public class CustomLogHandler : ILogHandler
    {
        public void LogException(System.Exception exception, Object context)
        {
            // No-op for exceptions
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            // No-op for all log messages
        }

        void ILogHandler.LogException(Exception exception, Object context)
        {
            // No-op for all log messages
        }
    }
}
