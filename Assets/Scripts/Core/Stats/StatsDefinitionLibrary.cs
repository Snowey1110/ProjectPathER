using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads <see cref="StatsDefinition"/> assets from Resources/StatsDefinitions and provides lookup by id.
///
/// Put assets under: Assets/Resources/StatsDefinitions/
/// and set their <see cref="StatsDefinition.id"/> to a unique value.
/// </summary>
public static class StatsDefinitionLibrary
{
    private static bool s_loaded;
    private static readonly Dictionary<string, StatsDefinition> s_byId = new Dictionary<string, StatsDefinition>();

    private const string ResourcesPath = "StatsDefinitions";

    private static void EnsureLoaded()
    {
        if (s_loaded) return;
        s_loaded = true;

        s_byId.Clear();

        var defs = Resources.LoadAll<StatsDefinition>(ResourcesPath);
        foreach (var def in defs)
        {
            if (def == null) continue;

            string key = string.IsNullOrWhiteSpace(def.id) ? def.name : def.id;
            if (s_byId.ContainsKey(key))
            {
                Debug.LogWarning($"[StatsDefinitionLibrary] Duplicate StatsDefinition id '{key}'. Keeping first, ignoring '{def.name}'.");
                continue;
            }

            s_byId[key] = def;
        }
    }

    public static StatsDefinition GetById(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(id)) return null;

        s_byId.TryGetValue(id, out var def);
        return def;
    }

    public static StatsDefinition GetForClass(ClassType classType)
    {
        return classType switch
        {
            ClassType.Archer => GetById("Archer"),
            ClassType.Knight => GetById("Knight"),
            ClassType.Mage => GetById("Mage"),
            ClassType.Healer => GetById("Healer"),
            _ => GetById("Generic")
        };
    }

    public static StatsDefinition GetForSlime() => GetById("Slime");

    public static StatsDefinition GetGeneric() => GetById("Generic");
}
