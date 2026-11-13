/*
First Date: 12/11/2026
Date: 12/11/2026
Author: Tony
Use by: Nothing to set up. It's an Editor tool (it's in an "Editor" folder, so it is not part of the game).
Unity Editor: Not used directly. The tools that bring in Asset Store models use it (Tools > World > Build Wild Animal,
              Tools > Items > Build Tier Tools).
Purpose: Many free packs ship materials made for URP (the Universal Render Pipeline). Our project uses the Built-in
         pipeline, which can't draw them: they show pink. This makes a Standard-shader copy of such a material with the
         same picture, colour, smoothness and metalness, read from the saved file (the URP shader itself isn't in the
         project). Made once: an existing copy is kept as it is, so changes made to it stay.
Requirement: MaterialBuildHelper.cs x-> UIBuildHelper.cs
             MaterialBuildHelper.cs <-x TierToolBuilder.cs, WildAnimalBuilder.cs
*/

using UnityEditor;
using UnityEngine;
using static UIBuildHelper;   // MakeFolders

public static class MaterialBuildHelper
{
    // ===== My methods =====

    // The materials to draw a model with: each one Built-in can't draw swapped for its Standard copy in this folder.
    // glossiness = force the copies' smoothness (e.g. 0.1 for a matte look); null = copy it from the original.
    public static Material[] DrawableCopies(Material[] originals, string folder, float? glossiness = null)
    {
        Material[] result = new Material[originals.Length];
        for (int i = 0; i < originals.Length; i++)
        {
            result[i] = CanDraw(originals[i]) ? originals[i] : BuiltInCopy(originals[i], folder, glossiness);
        }
        return result;
    }

    // True if Built-in can draw it: not a URP or HDRP material, and its shader is in the project and works here.
    public static bool CanDraw(Material m)
    {
        if (m == null || m.shader == null || !m.shader.isSupported) return false;
        string shader = m.shader.name;
        return !shader.StartsWith("Universal Render Pipeline") && !shader.StartsWith("HDRP") && shader != "Hidden/InternalErrorShader";
    }

    // A Standard copy of a material: "<name> (Built-in).mat" in the folder. An existing copy is kept as it is.
    public static Material BuiltInCopy(Material original, string folder, float? glossiness = null)
    {
        if (original == null) return null;
        MakeFolders(folder);
        string path = folder + "/" + original.name + " (Built-in).mat";
        Material copy = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (copy != null) return copy;

        // URP keeps the picture in _BaseMap, the colour in _BaseColor and the shine in _Smoothness.
        Texture picture = SavedTexture(original, "_BaseMap");
        if (picture == null) picture = SavedTexture(original, "_MainTex");
        copy = new Material(Shader.Find("Standard")) { name = original.name + " (Built-in)" };
        copy.mainTexture = picture;
        copy.color = SavedColor(original, "_BaseColor", SavedColor(original, "_Color", Color.white));
        copy.SetFloat("_Glossiness", glossiness ?? SavedFloat(original, "_Smoothness", SavedFloat(original, "_Glossiness", 0.1f)));
        copy.SetFloat("_Metallic", SavedFloat(original, "_Metallic", 0f));
        AssetDatabase.CreateAsset(copy, path);
        return copy;
    }

    static Texture SavedTexture(Material m, string property)
    {
        SerializedProperty list = new SerializedObject(m).FindProperty("m_SavedProperties.m_TexEnvs");
        for (int i = 0; list != null && i < list.arraySize; i++)
        {
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("first").stringValue == property)
                return entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
        }
        return null;
    }

    static Color SavedColor(Material m, string property, Color fallback)
    {
        SerializedProperty list = new SerializedObject(m).FindProperty("m_SavedProperties.m_Colors");
        for (int i = 0; list != null && i < list.arraySize; i++)
        {
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("first").stringValue == property) return entry.FindPropertyRelative("second").colorValue;
        }
        return fallback;
    }

    static float SavedFloat(Material m, string property, float fallback)
    {
        SerializedProperty list = new SerializedObject(m).FindProperty("m_SavedProperties.m_Floats");
        for (int i = 0; list != null && i < list.arraySize; i++)
        {
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("first").stringValue == property) return entry.FindPropertyRelative("second").floatValue;
        }
        return fallback;
    }
}
