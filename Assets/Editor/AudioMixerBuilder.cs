using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

static class AudioMixerBuilder
{
    const string Path = "Assets/Audio/MainMixer.mixer";
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    static readonly Assembly EditorAssembly = typeof(Editor).Assembly;

    [MenuItem("Tools/Audio/Crear MainMixer")]
    public static void Build()
    {
        if (AssetDatabase.LoadMainAssetAtPath(Path) != null)
        {
            Debug.LogWarning($"Ya existe {Path}: bórralo antes si quieres crearlo de nuevo.");
            return;
        }

        var controllerType = EditorAssembly.GetType("UnityEditor.Audio.AudioMixerController");
        var mixer = controllerType.GetMethod("CreateMixerControllerAtPath", Any).Invoke(null, new object[] { Path });
        var master = controllerType.GetProperty("masterGroup", Any).GetValue(mixer);

        var music = CreateGroup(mixer, "Music", master);
        var sfx = CreateGroup(mixer, "SFX", master);
        var ambience = CreateGroup(mixer, "Ambience", sfx);
        var effects = CreateGroup(mixer, "Effects", sfx);

        Expose(mixer, master, "MasterVolume");
        Expose(mixer, music, "MusicVolume");
        Expose(mixer, sfx, "SFXVolume");
        Expose(mixer, ambience, "AmbienceVolume");
        Expose(mixer, effects, "EffectsVolume");

        EditorUtility.SetDirty((UnityEngine.Object)mixer);
        AssetDatabase.SaveAssets();
        Debug.Log($"Creado {Path}");
    }

    static object CreateGroup(object mixer, string name, object parent)
    {
        var type = mixer.GetType();
        var group = type.GetMethods(Any).First(m => m.Name == "CreateNewGroup" && m.GetParameters().Length == 2)
            .Invoke(mixer, new object[] { name, false });
        type.GetMethods(Any).First(m => m.Name == "AddChildToParent" && m.GetParameters().Length == 2)
            .Invoke(mixer, new[] { group, parent });
        return group;
    }

    static void Expose(object mixer, object group, string name)
    {
        var guid = group.GetType().GetMethod("GetGUIDForVolume", Any).Invoke(group, null);
        var pathType = EditorAssembly.GetType("UnityEditor.Audio.AudioGroupParameterPath");
        var path = Activator.CreateInstance(pathType, Any, null, new[] { group, guid }, null);
        mixer.GetType().GetMethods(Any).First(m => m.Name == "AddExposedParameter" && m.GetParameters().Length == 1)
            .Invoke(mixer, new[] { path });

        var property = mixer.GetType().GetProperty("exposedParameters", Any);
        var parameters = (Array)property.GetValue(mixer);
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters.GetValue(i);
            var guidField = parameter.GetType().GetField("guid", Any);
            if (!guidField.GetValue(parameter).Equals(guid)) continue;
            parameter.GetType().GetField("name", Any).SetValue(parameter, name);
            parameters.SetValue(parameter, i);
        }
        property.SetValue(mixer, parameters);
    }
}
