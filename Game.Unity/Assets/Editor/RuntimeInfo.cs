using UnityEditor;
using UnityEngine;
using UnityEditor.Build;

public static class RuntimeInfo
{
    [MenuItem("Tools/Show Runtime Info")]
    private static void Show()
    {
        var runtimeVersion = System.Environment.Version;
    var apiCompatibility = PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone);
        Debug.Log($"Runtime Info => .NET Runtime Version: {runtimeVersion}, API Compatibility: {apiCompatibility}");
    }
}