// Assets/Editor/ReplaceTMPFontTool.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ReplaceTMPFontTool : EditorWindow
{
    [Header("Replace Font")]
    [SerializeField] private TMP_FontAsset fromFont;
    [SerializeField] private TMP_FontAsset toFont;

    [Header("Scope")]
    [SerializeField] private bool includePrefabs = true;
    [SerializeField] private bool includeScenes = true;

    [Header("Extra")]
    [SerializeField] private bool includeTMPSettings = true; // Default Font + Fallbacks in TMP Settings

    [MenuItem("Tools/Importer/Replace Font In Prefabs & Scenes")]
    public static void Open()
    {
        var w = GetWindow<ReplaceTMPFontTool>();
        w.titleContent = new GUIContent("Replace TMP Font");
        w.minSize = new Vector2(460, 220);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Replace TMP Font Asset (Project-wide)", EditorStyles.boldLabel);
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            fromFont = (TMP_FontAsset)EditorGUILayout.ObjectField("From (old)", fromFont, typeof(TMP_FontAsset), false);
            toFont   = (TMP_FontAsset)EditorGUILayout.ObjectField("To (new)",  toFont,   typeof(TMP_FontAsset), false);

            EditorGUILayout.HelpBox(
                "Tip: 'From'을 비워두면, 모든 TMP_Text의 폰트를 'To'로 강제 변경합니다.\n" +
                "일반적으로는 LiberationSans SDF를 'From'에 넣고 바꾸는 걸 추천.",
                MessageType.Info);
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            includePrefabs = EditorGUILayout.ToggleLeft("Include Prefabs", includePrefabs);
            includeScenes  = EditorGUILayout.ToggleLeft("Include Scenes", includeScenes);
            includeTMPSettings = EditorGUILayout.ToggleLeft("Also replace TMP Settings (Default + Fallbacks)", includeTMPSettings);
        }

        EditorGUILayout.Space(10);

        using (new EditorGUI.DisabledScope(toFont == null || (!includePrefabs && !includeScenes && !includeTMPSettings)))
        {
            if (GUILayout.Button("RUN: Replace Now", GUILayout.Height(34)))
            {
                Run();
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Notes:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• This will modify and save assets/scenes. Make sure you have a commit or backup.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.LabelField("• Scenes will be opened & saved, then your currently open scenes will be restored.", EditorStyles.wordWrappedMiniLabel);
    }

    private void Run()
    {
        if (toFont == null)
        {
            EditorUtility.DisplayDialog("Replace TMP Font", "'To' font is required.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Replace TMP Font",
                "This will modify assets/scenes and save them.\n\n" +
                "Recommended: commit/backup first.\n\nProceed?",
                "Proceed", "Cancel"))
        {
            return;
        }

        // Save current modified scenes before we start
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // Capture currently open scenes so we can restore them
        var openScenePaths = GetCurrentlyOpenScenePaths(out var activeScenePath);

        int changedTextComponents = 0;
        int changedPrefabs = 0;
        int changedScenes = 0;
        int changedTMPSettings = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            if (includeTMPSettings)
            {
                changedTMPSettings += ReplaceInTMPSettings();
            }

            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();

            // Prefabs
            if (includePrefabs)
            {
                var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    EditorUtility.DisplayProgressBar("Replacing TMP Fonts (Prefabs)", path, (float)i / prefabGuids.Length);

                    if (ReplaceInPrefab(path, out int changedInThisPrefab))
                    {
                        changedPrefabs++;
                        changedTextComponents += changedInThisPrefab;
                    }
                }
                EditorUtility.ClearProgressBar();
            }

            // Scenes
            if (includeScenes)
            {
                var sceneGuids = AssetDatabase.FindAssets("t:Scene");
                for (int i = 0; i < sceneGuids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                    EditorUtility.DisplayProgressBar("Replacing TMP Fonts (Scenes)", path, (float)i / sceneGuids.Length);

                    if (ReplaceInScene(path, out int changedInThisScene))
                    {
                        changedScenes++;
                        changedTextComponents += changedInThisScene;
                    }
                }
                EditorUtility.ClearProgressBar();
            }
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[ReplaceTMPFontTool] Error: " + e);
            EditorUtility.DisplayDialog("Replace TMP Font", "Error occurred. Check Console.", "OK");
        }
        finally
        {
            // Restore open scenes
            RestoreOpenScenes(openScenePaths, activeScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog(
            "Replace TMP Font - Done",
            $"Changed TMP_Text components: {changedTextComponents}\n" +
            $"Changed Prefabs: {changedPrefabs}\n" +
            $"Changed Scenes: {changedScenes}\n" +
            $"Changed TMP Settings entries: {changedTMPSettings}\n\n" +
            $"From: {(fromFont ? fromFont.name : "(ANY)")}\nTo: {toFont.name}",
            "OK");
    }

    private int ReplaceInTMPSettings()
    {
        int changes = 0;

        // TMP Settings asset (Undo/dirty 표시용)
        var settingsAsset = TMP_Settings.instance;
        if (settingsAsset == null) return 0;

        Undo.RecordObject(settingsAsset, "Replace TMP Settings Fonts");

        var currentDefault = TMP_Settings.defaultFontAsset;
        if (currentDefault != null && ShouldReplace(currentDefault))
        {
            TMP_Settings.defaultFontAsset = toFont;
            changes++;
        }

        var fallbacks = TMP_Settings.fallbackFontAssets;
        if (fallbacks != null)
        {
            for (int i = 0; i < fallbacks.Count; i++)
            {
                var f = fallbacks[i];
                if (f != null && ShouldReplace(f))
                {
                    fallbacks[i] = toFont;
                    changes++;
                }
            }
        }

        EditorUtility.SetDirty(settingsAsset);
        return changes;
    }

    private bool ReplaceInPrefab(string prefabPath, out int changedCount)
    {
        changedCount = 0;
        GameObject root = null;

        try
        {
            root = PrefabUtility.LoadPrefabContents(prefabPath);
            var texts = root.GetComponentsInChildren<TMP_Text>(true);

            bool changed = false;
            foreach (var t in texts)
            {
                if (t == null || t.font == null) continue;
                if (!ShouldReplace(t.font)) continue;

                t.font = toFont;
                changedCount++;
                changed = true;
                EditorUtility.SetDirty(t);
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }

            return false;
        }
        finally
        {
            if (root != null)
                PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private bool ReplaceInScene(string scenePath, out int changedCount)
    {
        changedCount = 0;

        // Open scene
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var roots = scene.GetRootGameObjects();
        var texts = roots.SelectMany(r => r.GetComponentsInChildren<TMP_Text>(true)).ToArray();

        bool changed = false;
        foreach (var t in texts)
        {
            if (t == null || t.font == null) continue;
            if (!ShouldReplace(t.font)) continue;

            t.font = toFont;
            changedCount++;
            changed = true;
            EditorUtility.SetDirty(t);
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }

        return false;
    }

    private bool ShouldReplace(TMP_FontAsset current)
    {
        if (current == null) return false;
        if (fromFont == null) return true; // replace any font
        return current == fromFont;
    }

    private static List<string> GetCurrentlyOpenScenePaths(out string activeScenePath)
    {
        var paths = new List<string>();
        activeScenePath = SceneManager.GetActiveScene().path;

        int count = SceneManager.sceneCount;
        for (int i = 0; i < count; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.IsValid() && s.isLoaded)
                paths.Add(s.path);
        }

        // Filter empty paths (Untitled)
        paths = paths.Where(p => !string.IsNullOrEmpty(p)).ToList();
        return paths;
    }

    private static void RestoreOpenScenes(List<string> openScenePaths, string activeScenePath)
    {
        if (openScenePaths == null || openScenePaths.Count == 0)
            return;

        // Reopen the first scene (Single), then add others (Additive)
        var first = openScenePaths[0];
        var s0 = EditorSceneManager.OpenScene(first, OpenSceneMode.Single);

        for (int i = 1; i < openScenePaths.Count; i++)
        {
            var p = openScenePaths[i];
            if (!string.IsNullOrEmpty(p))
                EditorSceneManager.OpenScene(p, OpenSceneMode.Additive);
        }

        // Restore active scene if possible
        if (!string.IsNullOrEmpty(activeScenePath))
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == activeScenePath)
                {
                    SceneManager.SetActiveScene(s);
                    break;
                }
            }
        }
    }
}
#endif